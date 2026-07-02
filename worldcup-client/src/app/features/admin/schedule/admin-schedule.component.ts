import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatchApiService } from '../../../core/api/match-api.service';
import { ReferenceApiService } from '../../../core/api/reference-api.service';
import { TournamentApiService } from '../../../core/api/tournament-api.service';
import { Match, Stadium, Team } from '../../../core/models/api.models';
import { WorldCupContextService } from '../../../core/services/worldcup-context.service';
import { WorldCupSelectorComponent } from '../../shared/world-cup-selector/world-cup-selector.component';

@Component({
  selector: 'app-admin-schedule',
  imports: [FormsModule, DatePipe, WorldCupSelectorComponent],
  templateUrl: './admin-schedule.component.html',
  styleUrl: './admin-schedule.component.scss',
})
export class AdminScheduleComponent implements OnInit {
  private readonly matchApi = inject(MatchApiService);
  private readonly tournamentApi = inject(TournamentApiService);
  private readonly referenceApi = inject(ReferenceApiService);
  protected readonly context = inject(WorldCupContextService);

  protected readonly isLoading = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly matches = signal<Match[]>([]);
  protected readonly teams = signal<Team[]>([]);
  protected readonly stadiums = signal<Stadium[]>([]);

  protected readonly teamIdsForWorldCup = computed(() => {
    const groupIds = new Set(this.context.groupsForSelectedWorldCup().map((group) => group.id));
    return new Set(this.teams().filter((team) => groupIds.has(team.groupId)).map((team) => team.id));
  });

  protected readonly matchesForWorldCup = computed(() => {
    const teamIds = this.teamIdsForWorldCup();
    return this.matches()
      .filter((match) => teamIds.has(match.teamOneId) && teamIds.has(match.teamTwoId))
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
    teamOneId: 0,
    teamTwoId: 0,
    stadiumId: 0,
    date: '',
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

  editMatch(match: Match): void {
    this.matchForm = {
      id: match.id,
      teamOneId: match.teamOneId,
      teamTwoId: match.teamTwoId,
      stadiumId: match.stadiumId,
      date: match.date.slice(0, 16),
    };
  }

  resetForm(): void {
    this.matchForm = { id: 0, teamOneId: 0, teamTwoId: 0, stadiumId: 0, date: '' };
    this.initFormDefaults();
  }

  async saveMatch(): Promise<void> {
    this.clearMessages();
    const payload = {
      teamOneId: this.matchForm.teamOneId,
      teamTwoId: this.matchForm.teamTwoId,
      stadiumId: this.matchForm.stadiumId,
      date: new Date(this.matchForm.date).toISOString(),
    };
    try {
      if (this.matchForm.id > 0) {
        await this.matchApi.updateMatch({ id: this.matchForm.id, ...payload });
        this.message.set('Match updated.');
      } else {
        await this.matchApi.addMatch(payload);
        this.message.set('Match scheduled.');
      }
      this.resetForm();
      await this.loadMatches();
    } catch {
      this.errorMessage.set('Failed to save match.');
    }
  }

  async deleteMatch(matchId: number): Promise<void> {
    if (!confirm('Delete this match?')) {
      return;
    }
    this.clearMessages();
    try {
      await this.matchApi.deleteMatch(matchId);
      this.message.set('Match deleted.');
      await this.loadMatches();
    } catch {
      this.errorMessage.set('Failed to delete match.');
    }
  }

  async resolveBets(matchId: number): Promise<void> {
    this.clearMessages();
    try {
      const result = await this.matchApi.resolveBetsForMatch(matchId);
      this.message.set(result.message);
    } catch {
      this.errorMessage.set('Failed to resolve bets.');
    }
  }

  statusClass(status: string): string {
    return status.toLowerCase();
  }

  private initFormDefaults(): void {
    const worldCupTeams = this.teamsForWorldCup();
    if (this.matchForm.teamOneId === 0 && worldCupTeams.length > 0) {
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
}
