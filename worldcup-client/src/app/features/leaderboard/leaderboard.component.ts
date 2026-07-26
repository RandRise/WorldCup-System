import { Component, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BetApiService } from '../../core/api/bet-api.service';
import { CompanyApiService } from '../../core/api/company-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { Company, LeaderboardEntry, LeaderboardSummary } from '../../core/models/api.models';
import { WorldCupContextService } from '../../core/services/worldcup-context.service';
import { WorldCupSelectorComponent } from '../shared/world-cup-selector/world-cup-selector.component';

@Component({
  selector: 'app-leaderboard',
  imports: [RouterLink, WorldCupSelectorComponent],
  templateUrl: './leaderboard.component.html',
  styleUrl: './leaderboard.component.scss',
})
export class LeaderboardComponent {
  private readonly betApi = inject(BetApiService);
  private readonly companyApi = inject(CompanyApiService);
  protected readonly auth = inject(AuthService);
  protected readonly context = inject(WorldCupContextService);

  private loadToken = 0;

  protected readonly company = signal<Company | null>(null);
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
      const mine = await this.companyApi.getMine();
      if (token !== this.loadToken || this.context.selectedWorldCupId() !== worldCupId) {
        return;
      }
      this.company.set(mine.company);

      if (mine.company == null) {
        this.entries.set([]);
        this.summary.set(null);
        return;
      }

      const [rows, summary] = await Promise.all([
        this.betApi.getLeaderboard(scopedId),
        this.betApi.getMySummary(scopedId),
      ]);
      if (token !== this.loadToken || this.context.selectedWorldCupId() !== worldCupId) {
        return;
      }
      this.entries.set(rows);
      this.summary.set(summary);
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
