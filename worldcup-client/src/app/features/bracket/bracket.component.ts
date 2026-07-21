import { DatePipe } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { KnockoutApiService } from '../../core/api/knockout-api.service';
import { Bracket, BracketRound, Match } from '../../core/models/api.models';
import { WorldCupContextService } from '../../core/services/worldcup-context.service';
import { WorldCupSelectorComponent } from '../shared/world-cup-selector/world-cup-selector.component';

@Component({
  selector: 'app-bracket',
  imports: [DatePipe, RouterLink, WorldCupSelectorComponent],
  templateUrl: './bracket.component.html',
  styleUrl: './bracket.component.scss',
})
export class BracketComponent {
  private readonly knockoutApi = inject(KnockoutApiService);
  protected readonly context = inject(WorldCupContextService);

  private loadToken = 0;

  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly rounds = signal<BracketRound[]>([]);

  constructor() {
    effect(() => {
      const worldCupId = this.context.selectedWorldCupId();
      void this.loadBracket(worldCupId);
    });
  }

  async loadBracket(worldCupId: number | null = this.context.selectedWorldCupId()): Promise<void> {
    const token = ++this.loadToken;

    if (worldCupId == null) {
      this.rounds.set([]);
      this.isLoading.set(false);
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const bracket: Bracket = await this.knockoutApi.getBracket(worldCupId);
      if (token !== this.loadToken || this.context.selectedWorldCupId() !== worldCupId) {
        return;
      }
      this.rounds.set(bracket.rounds ?? []);
    } catch {
      if (token !== this.loadToken || this.context.selectedWorldCupId() !== worldCupId) {
        return;
      }
      this.errorMessage.set('Failed to load knockout bracket.');
      this.rounds.set([]);
    } finally {
      if (token === this.loadToken) {
        this.isLoading.set(false);
      }
    }
  }

  teamLabel(match: Match, side: 'one' | 'two'): string {
    if (side === 'one') {
      return match.teamOneName?.trim() || 'TBD';
    }
    return match.teamTwoName?.trim() || 'TBD';
  }

  scoreLabel(match: Match, side: 'one' | 'two'): string {
    if (match.status === 'Scheduled' && (!match.teamOneId || !match.teamTwoId)) {
      return '—';
    }
    if (side === 'one') {
      return String(match.teamOneScore ?? 0);
    }
    return String(match.teamTwoScore ?? 0);
  }
}
