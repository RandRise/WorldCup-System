import { Component, effect, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BetApiService } from '../../../core/api/bet-api.service';
import { KnockoutApiService } from '../../../core/api/knockout-api.service';
import { MatchApiService } from '../../../core/api/match-api.service';
import { ReferenceApiService } from '../../../core/api/reference-api.service';
import { GenerateBracketResult, ResolveBetsWorldCupResult, Stadium } from '../../../core/models/api.models';
import { WorldCupContextService } from '../../../core/services/worldcup-context.service';
import { WorldCupSelectorComponent } from '../../shared/world-cup-selector/world-cup-selector.component';

@Component({
  selector: 'app-admin-hub',
  imports: [FormsModule, RouterLink, WorldCupSelectorComponent],
  templateUrl: './admin-hub.component.html',
  styleUrl: './admin-hub.component.scss',
})
export class AdminHubComponent implements OnInit {
  private readonly matchApi = inject(MatchApiService);
  private readonly betApi = inject(BetApiService);
  private readonly knockoutApi = inject(KnockoutApiService);
  private readonly referenceApi = inject(ReferenceApiService);
  protected readonly context = inject(WorldCupContextService);

  protected readonly isLoading = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly liveMatchCount = signal(0);
  protected readonly finishedMatchCount = signal(0);
  protected readonly lastBulkResolve = signal<ResolveBetsWorldCupResult | null>(null);
  protected readonly stadiums = signal<Stadium[]>([]);
  protected readonly bracketStadiumId = signal(0);
  protected readonly bracketKickoff = signal('');

  constructor() {
    effect(() => {
      this.context.selectedWorldCupId();
      void this.loadCounts();
    });
  }

  async ngOnInit(): Promise<void> {
    const stadiums = await this.referenceApi.getStadiums();
    this.stadiums.set(stadiums);
    if (stadiums.length > 0) {
      this.bracketStadiumId.set(stadiums[0].id);
    }
    const kickoff = new Date();
    kickoff.setUTCDate(kickoff.getUTCDate() + 1);
    kickoff.setUTCHours(18, 0, 0, 0);
    this.bracketKickoff.set(kickoff.toISOString().slice(0, 16));
    await this.loadCounts();
  }

  async loadCounts(): Promise<void> {
    const worldCupId = this.context.selectedWorldCupId();
    if (worldCupId == null) {
      this.liveMatchCount.set(0);
      this.finishedMatchCount.set(0);
      return;
    }

    this.isLoading.set(true);
    try {
      const fixtures = await this.matchApi.getFixturesByWorldCup(worldCupId);
      this.liveMatchCount.set(fixtures.filter((match) => match.status === 'Live').length);
      this.finishedMatchCount.set(fixtures.filter((match) => match.status === 'Finished').length);
    } catch {
      this.errorMessage.set('Failed to load match counts.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async bulkResolve(): Promise<void> {
    const worldCupId = this.context.selectedWorldCupId();
    if (worldCupId == null) {
      return;
    }

    this.message.set(null);
    this.errorMessage.set(null);
    try {
      const result = await this.betApi.resolveBetsForWorldCup(worldCupId);
      this.lastBulkResolve.set(result);
      this.message.set(result.message);
    } catch {
      this.errorMessage.set('Failed to resolve bets for finished matches.');
    }
  }

  async generateBracket(): Promise<void> {
    const worldCupId = this.context.selectedWorldCupId();
    if (worldCupId == null) {
      return;
    }

    this.message.set(null);
    this.errorMessage.set(null);
    try {
      const result: GenerateBracketResult = await this.knockoutApi.generateBracket({
        worldCupId,
        stadiumId: this.bracketStadiumId(),
        firstKickoff: new Date(this.bracketKickoff()).toISOString(),
      });
      this.message.set(result.message);
      await this.loadCounts();
    } catch (error: unknown) {
      const body = (error as { error?: { error?: string } })?.error?.error;
      this.errorMessage.set(
        typeof body === 'string' && body.trim().length > 0
          ? body
          : 'Failed to generate knockout bracket. Need exactly 8 groups with finished standings.',
      );
    }
  }
}
