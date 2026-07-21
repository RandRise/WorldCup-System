import { Component, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BetApiService } from '../../core/api/bet-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { Bet, LeaderboardSummary } from '../../core/models/api.models';
import { WorldCupContextService } from '../../core/services/worldcup-context.service';
import { WorldCupSelectorComponent } from '../shared/world-cup-selector/world-cup-selector.component';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, WorldCupSelectorComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {
  private readonly betApi = inject(BetApiService);
  protected readonly auth = inject(AuthService);
  protected readonly context = inject(WorldCupContextService);

  private loadToken = 0;

  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly summary = signal<LeaderboardSummary | null>(null);
  protected readonly activeBets = signal<Bet[]>([]);

  constructor() {
    effect(() => {
      const worldCupId = this.context.selectedWorldCupId();
      void this.load(worldCupId);
    });
  }

  async load(worldCupId: number | null = this.context.selectedWorldCupId()): Promise<void> {
    const token = ++this.loadToken;
    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const [summary, bets] = await Promise.all([
        this.betApi.getMySummary(worldCupId ?? undefined),
        worldCupId != null
          ? this.betApi.getMyBetsForWorldCup(worldCupId)
          : this.betApi.getMyActiveBets(),
      ]);
      if (token !== this.loadToken || this.context.selectedWorldCupId() !== worldCupId) {
        return;
      }
      this.summary.set(summary);
      this.activeBets.set(bets.filter((bet) => bet.isActive));
    } catch {
      if (token !== this.loadToken || this.context.selectedWorldCupId() !== worldCupId) {
        return;
      }
      this.errorMessage.set('Failed to load your dashboard.');
    } finally {
      if (token === this.loadToken) {
        this.isLoading.set(false);
      }
    }
  }
}
