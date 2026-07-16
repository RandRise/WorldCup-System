import { Component, effect, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BetApiService } from '../../core/api/bet-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { Bet, LeaderboardSummary } from '../../core/models/api.models';
import { WorldCupContextService } from '../../core/services/worldcup-context.service';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private readonly betApi = inject(BetApiService);
  protected readonly auth = inject(AuthService);
  protected readonly context = inject(WorldCupContextService);

  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly summary = signal<LeaderboardSummary | null>(null);
  protected readonly activeBets = signal<Bet[]>([]);

  constructor() {
    effect(() => {
      this.context.selectedWorldCupId();
      void this.load();
    });
  }

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  async load(): Promise<void> {
    const worldCupId = this.context.selectedWorldCupId();
    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const [summary, bets] = await Promise.all([
        this.betApi.getMySummary(worldCupId ?? undefined),
        worldCupId != null
          ? this.betApi.getMyBetsForWorldCup(worldCupId)
          : this.betApi.getMyActiveBets(),
      ]);
      this.summary.set(summary);
      this.activeBets.set(bets.filter((bet) => bet.isActive));
    } catch {
      this.errorMessage.set('Failed to load your dashboard.');
    } finally {
      this.isLoading.set(false);
    }
  }
}
