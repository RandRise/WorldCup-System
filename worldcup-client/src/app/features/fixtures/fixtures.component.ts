import { DatePipe } from '@angular/common';
import { Component, effect, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatchApiService } from '../../core/api/match-api.service';
import { BetApiService } from '../../core/api/bet-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { Bet, Match } from '../../core/models/api.models';
import { WorldCupContextService } from '../../core/services/worldcup-context.service';
import { WorldCupSelectorComponent } from '../shared/world-cup-selector/world-cup-selector.component';

@Component({
  selector: 'app-fixtures',
  imports: [DatePipe, RouterLink, WorldCupSelectorComponent],
  templateUrl: './fixtures.component.html',
  styleUrl: './fixtures.component.scss',
})
export class FixturesComponent implements OnInit, OnDestroy {
  private readonly matchApi = inject(MatchApiService);
  private readonly betApi = inject(BetApiService);
  protected readonly auth = inject(AuthService);
  protected readonly context = inject(WorldCupContextService);

  private refreshTimer: ReturnType<typeof setInterval> | null = null;
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly message = signal<string | null>(null);
  protected readonly matches = signal<Match[]>([]);
  protected readonly recentEvents = signal<{ matchId: number; eventType: string; minute: number; playerName?: string | null; teamName?: string | null }[]>([]);
  protected readonly userBets = signal<Map<number, Bet>>(new Map());
  protected readonly placingMatchId = signal<number | null>(null);

  constructor() {
    effect(() => {
      this.context.selectedWorldCupId();
      void this.loadFixtures();
    });
  }

  async ngOnInit(): Promise<void> {
    await this.loadFixtures();
    this.refreshTimer = setInterval(() => {
      if (this.shouldAutoRefresh()) {
        void this.refreshLiveData();
      }
    }, 30_000);
  }

  ngOnDestroy(): void {
    if (this.refreshTimer != null) {
      clearInterval(this.refreshTimer);
    }
  }
  async loadFixtures(): Promise<void> {
    const worldCupId = this.context.selectedWorldCupId();
    if (worldCupId == null) {
      return;
    }
    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const fixtures = await this.matchApi.getFixturesByWorldCup(worldCupId);
      this.matches.set(fixtures.sort((a, b) => a.date.localeCompare(b.date)));

      if (this.auth.isAuthenticated()) {
        const bets = await this.betApi.getMyBetsForWorldCup(worldCupId);
        const betMap = new Map<number, Bet>();
        for (const bet of bets) {
          betMap.set(bet.matchId, bet);
        }
        this.userBets.set(betMap);
      }

      // Always pull snapshot on load so finished matches can auto-resolve bets
      // even when nothing is live (polling alone would never start).
      await this.applyLiveSnapshot(worldCupId);
    } catch {
      this.errorMessage.set('Failed to load fixtures.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async refreshLiveData(): Promise<void> {
    const worldCupId = this.context.selectedWorldCupId();
    if (worldCupId == null) {
      return;
    }

    try {
      await this.applyLiveSnapshot(worldCupId);
    } catch {
      // Keep showing last known scores on transient poll failures.
    }
  }

  private async applyLiveSnapshot(worldCupId: number): Promise<void> {
    const snapshot = await this.matchApi.getLiveSnapshot(worldCupId);
    this.recentEvents.set(snapshot.recentEvents);

    const snapshotByMatch = new Map(snapshot.matches.map((item) => [item.matchId, item]));
    this.matches.update((current) =>
      current.map((match) => {
        const live = snapshotByMatch.get(match.id);
        if (!live) {
          return match;
        }
        return {
          ...match,
          teamOneScore: live.teamOneScore,
          teamTwoScore: live.teamTwoScore,
          status: live.status,
        };
      }),
    );
  }

  userBet(matchId: number): Bet | undefined {
    return this.userBets().get(matchId);
  }

  async placeBet(match: Match, isDraw: boolean, teamId?: number): Promise<void> {
    if (!this.auth.isAuthenticated()) {
      return;
    }
    this.placingMatchId.set(match.id);
    this.message.set(null);
    this.errorMessage.set(null);
    try {
      await this.betApi.placeBet({
        matchId: match.id,
        isDraw,
        teamId: isDraw ? null : teamId,
      });
      const bet = await this.betApi.getMyBetForMatch(match.id);
      const updated = new Map(this.userBets());
      if (bet) {
        updated.set(match.id, bet);
      }
      this.userBets.set(updated);
      this.message.set('Prediction saved.');
    } catch {
      this.errorMessage.set('Could not place bet. Match may have started or you already predicted.');
    } finally {
      this.placingMatchId.set(null);
    }
  }

  statusClass(status: string): string {
    return status.toLowerCase();
  }

  /** Refresh while any match is live, or a scheduled kickoff is due / overdue (Scheduled → Live). */
  private shouldAutoRefresh(): boolean {
    const now = Date.now();
    return this.matches().some((match) => {
      if (match.status === 'Live') {
        return true;
      }

      if (match.status !== 'Scheduled') {
        return false;
      }

      const kickoffMs = Date.parse(match.date);
      return !Number.isNaN(kickoffMs) && kickoffMs <= now + 60_000;
    });
  }
}
