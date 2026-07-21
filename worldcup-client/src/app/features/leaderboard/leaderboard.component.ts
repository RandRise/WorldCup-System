import { Component, effect, inject, signal } from '@angular/core';
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
export class LeaderboardComponent {
  private readonly betApi = inject(BetApiService);
  protected readonly auth = inject(AuthService);
  protected readonly context = inject(WorldCupContextService);

  private loadToken = 0;

  protected readonly entries = signal<LeaderboardEntry[]>([]);
  protected readonly summary = signal<LeaderboardSummary | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  constructor() {
    effect(() => {
      const worldCupId = this.context.selectedWorldCupId();
      void this.load(worldCupId);
    });
  }

  private async load(worldCupId: number | null = this.context.selectedWorldCupId()): Promise<void> {
    const token = ++this.loadToken;
    const scopedId = worldCupId ?? undefined;

    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const requests: Promise<void>[] = [
        this.betApi.getLeaderboard(scopedId).then((rows) => {
          if (token !== this.loadToken || this.context.selectedWorldCupId() !== worldCupId) {
            return;
          }
          this.entries.set(rows);
        }),
      ];
      if (this.auth.isAuthenticated()) {
        requests.push(
          this.betApi.getMySummary(scopedId).then((row) => {
            if (token !== this.loadToken || this.context.selectedWorldCupId() !== worldCupId) {
              return;
            }
            this.summary.set(row);
          }),
        );
      } else {
        this.summary.set(null);
      }
      await Promise.all(requests);
    } catch {
      if (token !== this.loadToken || this.context.selectedWorldCupId() !== worldCupId) {
        return;
      }
      this.errorMessage.set('Failed to load leaderboard.');
    } finally {
      if (token === this.loadToken) {
        this.isLoading.set(false);
      }
    }
  }

  isCurrentUser(entry: LeaderboardEntry): boolean {
    return this.auth.currentUser()?.id === entry.userId;
  }
}
