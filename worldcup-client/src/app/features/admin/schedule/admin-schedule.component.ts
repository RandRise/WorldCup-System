import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BetApiService } from '../../../core/api/bet-api.service';
import { KnockoutApiService } from '../../../core/api/knockout-api.service';
import { MatchApiService } from '../../../core/api/match-api.service';
import { ReferenceApiService } from '../../../core/api/reference-api.service';
import { TournamentApiService } from '../../../core/api/tournament-api.service';
import {
  MATCH_STAGE_OPTIONS,
  Match,
  MatchStage,
  MatchStages,
  ResolveBetsResult,
  Stadium,
  Team,
} from '../../../core/models/api.models';
import { WorldCupContextService } from '../../../core/services/worldcup-context.service';
import { WorldCupSelectorComponent } from '../../shared/world-cup-selector/world-cup-selector.component';

@Component({
  selector: 'app-admin-schedule',
  imports: [FormsModule, DatePipe, RouterLink, WorldCupSelectorComponent],
  templateUrl: './admin-schedule.component.html',
  styleUrl: './admin-schedule.component.scss',
})
export class AdminScheduleComponent implements OnInit {
  private readonly matchApi = inject(MatchApiService);
  private readonly betApi = inject(BetApiService);
  private readonly knockoutApi = inject(KnockoutApiService);
  private readonly tournamentApi = inject(TournamentApiService);
  private readonly referenceApi = inject(ReferenceApiService);
  protected readonly context = inject(WorldCupContextService);

  protected readonly stageOptions = MATCH_STAGE_OPTIONS;
  protected readonly MatchStages = MatchStages;

  protected readonly isLoading = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly matches = signal<Match[]>([]);
  protected readonly teams = signal<Team[]>([]);
  protected readonly stadiums = signal<Stadium[]>([]);
  protected readonly lastResolve = signal<ResolveBetsResult | null>(null);
  protected readonly stageFilter = signal<MatchStage | 'all'>('all');

  protected readonly matchesForWorldCup = computed(() => {
    const filter = this.stageFilter();
    return this.matches()
      .filter((match) => filter === 'all' || (match.stage ?? MatchStages.Group) === filter)
      .sort((left, right) => left.date.localeCompare(right.date));
  });

  protected readonly teamsForWorldCup = computed(() => {
    const groupIds = new Set(this.context.groupsForSelectedWorldCup().map((group) => group.id));
    return this.teams()
      .filter((team) => groupIds.has(team.groupId))
      .sort((a, b) => (a.countryName ?? '').localeCompare(b.countryName ?? ''));
  });

  protected matchForm = {
    id: 0,
    teamOneId: null as number | null,
    teamTwoId: null as number | null,
    stadiumId: 0,
    date: '',
    stage: MatchStages.Group as MatchStage,
  };

  constructor() {
    effect(() => {
      this.context.selectedWorldCupId();
      void this.loadMatches();
    });
  }

  async ngOnInit(): Promise<void> {
    await this.reloadLookups();
    await this.loadMatches();
  }

  async reloadLookups(): Promise<void> {
    const [teams, stadiums] = await Promise.all([
      this.tournamentApi.getTeams(),
      this.referenceApi.getStadiums(),
    ]);
    this.teams.set(teams);
    this.stadiums.set(stadiums);
    this.initFormDefaults();
  }

  async loadMatches(): Promise<void> {
    const worldCupId = this.context.selectedWorldCupId();
    if (worldCupId == null) {
      this.matches.set([]);
      return;
    }
    this.isLoading.set(true);
    try {
      const fixtures = await this.matchApi.getFixturesByWorldCup(worldCupId);
      this.matches.set(fixtures);
    } catch {
      this.errorMessage.set('Failed to load matches.');
    } finally {
      this.isLoading.set(false);
    }
  }

  setStageFilter(filter: MatchStage | 'all'): void {
    this.stageFilter.set(filter);
  }

  editMatch(match: Match): void {
    this.matchForm = {
      id: match.id,
      teamOneId: match.teamOneId ?? null,
      teamTwoId: match.teamTwoId ?? null,
      stadiumId: match.stadiumId,
      date: match.date.slice(0, 16),
      stage: match.stage ?? MatchStages.Group,
    };
  }

  resetForm(): void {
    this.matchForm = {
      id: 0,
      teamOneId: null,
      teamTwoId: null,
      stadiumId: 0,
      date: '',
      stage: MatchStages.Group,
    };
    this.initFormDefaults();
  }

  async saveMatch(): Promise<void> {
    this.clearMessages();
    const isKnockoutTbd =
      this.matchForm.stage !== MatchStages.Group &&
      this.matchForm.teamOneId == null &&
      this.matchForm.teamTwoId == null;
    if (
      !isKnockoutTbd &&
      (this.matchForm.teamOneId == null ||
        this.matchForm.teamTwoId == null ||
        this.matchForm.teamOneId <= 0 ||
        this.matchForm.teamTwoId <= 0)
    ) {
      this.errorMessage.set(
        'Select both teams, or leave both empty for a knockout TBD slot.',
      );
      return;
    }

    const payload = {
      teamOneId: this.matchForm.teamOneId,
      teamTwoId: this.matchForm.teamTwoId,
      stadiumId: this.matchForm.stadiumId,
      date: new Date(this.matchForm.date).toISOString(),
      stage: this.matchForm.stage,
    };
    try {
      if (this.matchForm.id > 0) {
        await this.matchApi.updateMatch({ id: this.matchForm.id, ...payload });
        this.message.set('Match updated.');
      } else {
        if (payload.teamOneId == null || payload.teamTwoId == null) {
          this.errorMessage.set('New matches require both teams.');
          return;
        }
        await this.matchApi.addMatch({
          teamOneId: payload.teamOneId,
          teamTwoId: payload.teamTwoId,
          stadiumId: payload.stadiumId,
          date: payload.date,
          stage: payload.stage,
        });
        this.message.set('Match scheduled.');
      }
      this.resetForm();
      await this.loadMatches();
    } catch (error: unknown) {
      this.errorMessage.set(this.readApiError(error, 'Failed to save match.'));
    }
  }

  async deleteMatch(matchId: number): Promise<void> {
    if (!confirm('Delete this match? Finished matches with goals, cards, or bets cannot be deleted — keep group history and schedule knockout as a new stage instead.')) {
      return;
    }
    this.clearMessages();
    try {
      await this.matchApi.deleteMatch(matchId);
      this.message.set('Match deleted.');
      await this.loadMatches();
    } catch (error: unknown) {
      this.errorMessage.set(this.readApiError(error, 'Failed to delete match.'));
    }
  }

  async resolveBets(matchId: number): Promise<void> {
    this.clearMessages();
    try {
      const result = await this.betApi.resolveBetsForMatch(matchId);
      this.lastResolve.set(result);
      this.message.set(result.message);
    } catch (error: unknown) {
      this.errorMessage.set(this.readApiError(error, 'Failed to resolve bets.'));
    }
  }

  canAdvanceWinner(match: Match): boolean {
    const stage = match.stage ?? MatchStages.Group;
    return match.status === 'Finished' && stage !== MatchStages.Group;
  }

  async advanceWinner(matchId: number): Promise<void> {
    this.clearMessages();
    try {
      const response = await this.knockoutApi.advanceFromMatch(matchId);
      this.message.set(
        response?.trim().length > 0
          ? response
          : 'Winner advanced where destination slots were open.',
      );
      await this.loadMatches();
    } catch (error: unknown) {
      this.errorMessage.set(
        this.readApiError(
          error,
          'Cannot advance — destination may already have goals or cards recorded.',
        ),
      );
    }
  }

  statusClass(status: string): string {
    return status.toLowerCase();
  }

  private initFormDefaults(): void {
    const worldCupTeams = this.teamsForWorldCup();
    if (this.matchForm.teamOneId == null && worldCupTeams.length > 0) {
      this.matchForm.teamOneId = worldCupTeams[0].id;
      this.matchForm.teamTwoId = worldCupTeams[1]?.id ?? worldCupTeams[0].id;
    }
    if (this.matchForm.stadiumId === 0 && this.stadiums().length > 0) {
      this.matchForm.stadiumId = this.stadiums()[0].id;
    }
    if (!this.matchForm.date) {
      const kickoff = new Date();
      kickoff.setUTCDate(kickoff.getUTCDate() + 7);
      kickoff.setUTCHours(18, 0, 0, 0);
      this.matchForm.date = kickoff.toISOString().slice(0, 16);
    }
  }

  private clearMessages(): void {
    this.message.set(null);
    this.errorMessage.set(null);
  }

  private readApiError(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error;
      if (typeof body === 'object' && body !== null && 'error' in body) {
        const message = (body as { error?: string }).error;
        if (typeof message === 'string' && message.trim().length > 0) {
          return message;
        }
      }
    }

    return fallback;
  }
}
