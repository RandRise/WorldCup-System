import { DatePipe } from '@angular/common';
import { Component, effect, inject, OnInit, signal } from '@angular/core';
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
export class BracketComponent implements OnInit {
  private readonly knockoutApi = inject(KnockoutApiService);
  protected readonly context = inject(WorldCupContextService);

  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly rounds = signal<BracketRound[]>([]);

  constructor() {
    effect(() => {
      this.context.selectedWorldCupId();
      void this.loadBracket();
    });
  }

  async ngOnInit(): Promise<void> {
    await this.loadBracket();
  }

  async loadBracket(): Promise<void> {
    const worldCupId = this.context.selectedWorldCupId();
    if (worldCupId == null) {
      this.rounds.set([]);
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const bracket: Bracket = await this.knockoutApi.getBracket(worldCupId);
      this.rounds.set(bracket.rounds ?? []);
    } catch {
      this.errorMessage.set('Failed to load knockout bracket.');
      this.rounds.set([]);
    } finally {
      this.isLoading.set(false);
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
