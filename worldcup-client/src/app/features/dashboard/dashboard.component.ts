import { Component, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BetApiService } from '../../core/api/bet-api.service';
import { CompanyApiService } from '../../core/api/company-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { Bet, Company, LeaderboardSummary } from '../../core/models/api.models';
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
  private readonly companyApi = inject(CompanyApiService);
  protected readonly auth = inject(AuthService);
  protected readonly context = inject(WorldCupContextService);

  private loadToken = 0;

  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly company = signal<Company | null>(null);
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
      const [mine, summary, bets] = await Promise.all([
        this.companyApi.getMine(),
        this.betApi.getMySummary(worldCupId ?? undefined),
        worldCupId != null
          ? this.betApi.getMyBetsForWorldCup(worldCupId)
          : this.betApi.getMyActiveBets(),
      ]);
      if (token !== this.loadToken || this.context.selectedWorldCupId() !== worldCupId) {
        return;
      }
      this.company.set(mine.company);
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
