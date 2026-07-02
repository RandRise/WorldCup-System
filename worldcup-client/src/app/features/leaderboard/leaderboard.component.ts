import { Component, effect, inject, OnInit, signal } from '@angular/core';
import { BetApiService } from '../../core/api/bet-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { LeaderboardEntry, LeaderboardSummary } from '../../core/models/api.models';
import { WorldCupContextService } from '../../core/services/worldcup-context.service';
import { WorldCupSelectorComponent } from '../shared/world-cup-selector/world-cup-selector.component';

@Component({
  selector: 'app-leaderboard',
  imports: [WorldCupSelectorComponent],
  templateUrl: './leaderboard.component.html',
  styleUrl: './leaderboard.component.scss',
})
export class LeaderboardComponent implements OnInit {
  private readonly betApi = inject(BetApiService);
  protected readonly auth = inject(AuthService);
  protected readonly context = inject(WorldCupContextService);

  protected readonly entries = signal<LeaderboardEntry[]>([]);
  protected readonly summary = signal<LeaderboardSummary | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  constructor() {
    effect(() => {
      this.context.selectedWorldCupId();
      void this.load();
    });
  }

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  private async load(): Promise<void> {
    const worldCupId = this.context.selectedWorldCupId() ?? undefined;
    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const requests: Promise<void>[] = [
        this.betApi.getLeaderboard(worldCupId).then((rows) => this.entries.set(rows)),
      ];
      if (this.auth.isAuthenticated()) {
        requests.push(
          this.betApi.getMySummary(worldCupId).then((row) => this.summary.set(row)),
        );
      } else {
        this.summary.set(null);
      }
      await Promise.all(requests);
    } catch {
      this.errorMessage.set('Failed to load leaderboard.');
    } finally {
      this.isLoading.set(false);
    }
  }

  isCurrentUser(entry: LeaderboardEntry): boolean {
    return this.auth.currentUser()?.id === entry.userId;
  }
}
