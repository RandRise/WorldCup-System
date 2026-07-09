import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { ReferenceApiService } from '../../../core/api/reference-api.service';
import { TournamentApiService } from '../../../core/api/tournament-api.service';
import { Coach, Country, Player, PlayerPosition, Team } from '../../../core/models/api.models';
import { WorldCupContextService } from '../../../core/services/worldcup-context.service';
import { WorldCupSelectorComponent } from '../../shared/world-cup-selector/world-cup-selector.component';

@Component({
  selector: 'app-admin-teams',
  imports: [FormsModule, WorldCupSelectorComponent],
  templateUrl: './admin-teams.component.html',
  styleUrl: './admin-teams.component.scss',
})
export class AdminTeamsComponent implements OnInit {
  private readonly tournamentApi = inject(TournamentApiService);
  private readonly referenceApi = inject(ReferenceApiService);
  protected readonly context = inject(WorldCupContextService);

  protected readonly isLoading = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly countries = signal<Country[]>([]);
  protected readonly teams = signal<Team[]>([]);
  protected readonly positions = signal<PlayerPosition[]>([]);
  protected readonly selectedTeamId = signal<number | null>(null);
  protected readonly coaches = signal<Coach[]>([]);
  protected readonly players = signal<Player[]>([]);

  protected readonly teamsForWorldCup = computed(() => {
    const groupIds = new Set(this.context.groupsForSelectedWorldCup().map((group) => group.id));
    return this.teams().filter((team) => groupIds.has(team.groupId));
  });

  protected teamForm = { countryId: 0, groupId: 0 };
  protected coachForm = { id: 0, name: '', teamId: 0 };
  protected playerForm = { id: 0, name: '', number: 1, teamId: 0, positionId: 0 };

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    this.isLoading.set(true);
    this.clearMessages();
    try {
      const [countries, teams, positions] = await Promise.all([
        this.referenceApi.getCountries(),
        this.tournamentApi.getTeams(),
        this.tournamentApi.getPlayerPositions(),
      ]);
      this.countries.set(countries.sort((a, b) => a.name.localeCompare(b.name)));
      this.teams.set(teams);
      this.positions.set(positions);
      this.initForms();
      const selectedId = this.selectedTeamId();
      if (selectedId != null) {
        await this.loadSquad(selectedId);
      }
    } catch {
      this.errorMessage.set('Failed to load teams.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async selectTeam(teamId: number): Promise<void> {
    this.selectedTeamId.set(teamId);
    this.coachForm = { id: 0, name: '', teamId };
    this.playerForm = {
      id: 0,
      name: '',
      number: 1,
      teamId,
      positionId: this.positions()[0]?.id ?? 0,
    };
    await this.loadSquad(teamId);
  }

  async addTeam(): Promise<void> {
    this.clearMessages();
    try {
      await this.tournamentApi.addTeam({
        countryId: this.teamForm.countryId,
        groupId: this.teamForm.groupId,
      });
      this.message.set('Team added.');
      await this.reload();
    } catch (error: unknown) {
      this.errorMessage.set(this.readApiError(error, 'Failed to add team.'));
    }
  }

  async deleteTeam(teamId: number): Promise<void> {
    if (!confirm('Delete this team?')) {
      return;
    }
    this.clearMessages();
    try {
      await this.tournamentApi.deleteTeam(teamId);
      if (this.selectedTeamId() === teamId) {
        this.selectedTeamId.set(null);
        this.coaches.set([]);
        this.players.set([]);
      }
      this.message.set('Team deleted.');
      await this.reload();
    } catch (error: unknown) {
      this.errorMessage.set(this.readApiError(error, 'Failed to delete team.'));
    }
  }

  async saveCoach(): Promise<void> {
    this.clearMessages();
    try {
      if (this.coachForm.id > 0) {
        await this.tournamentApi.updateCoach(this.coachForm);
        this.message.set('Coach updated.');
      } else {
        await this.tournamentApi.addCoach({
          name: this.coachForm.name.trim(),
          teamId: this.coachForm.teamId,
        });
        this.message.set('Coach added.');
      }
      await this.loadSquad(this.coachForm.teamId);
      this.coachForm = { id: 0, name: '', teamId: this.coachForm.teamId };
    } catch {
      this.errorMessage.set('Failed to save coach.');
    }
  }

  editCoach(coach: Coach): void {
    this.coachForm = { id: coach.id, name: coach.name, teamId: coach.teamId };
  }

  async deleteCoach(coachId: number): Promise<void> {
    const teamId = this.selectedTeamId();
    if (teamId == null) {
      return;
    }
    this.clearMessages();
    try {
      await this.tournamentApi.deleteCoach(coachId);
      await this.loadSquad(teamId);
    } catch {
      this.errorMessage.set('Failed to delete coach.');
    }
  }

  async savePlayer(): Promise<void> {
    this.clearMessages();
    try {
      if (this.playerForm.id > 0) {
        await this.tournamentApi.updatePlayer(this.playerForm);
        this.message.set('Player updated.');
      } else {
        await this.tournamentApi.addPlayer({
          name: this.playerForm.name.trim(),
          number: this.playerForm.number,
          teamId: this.playerForm.teamId,
          positionId: this.playerForm.positionId,
        });
        this.message.set('Player added.');
      }
      await this.loadSquad(this.playerForm.teamId);
      this.playerForm = {
        id: 0,
        name: '',
        number: 1,
        teamId: this.playerForm.teamId,
        positionId: this.positions()[0]?.id ?? 0,
      };
    } catch {
      this.errorMessage.set('Failed to save player.');
    }
  }

  editPlayer(player: Player): void {
    this.playerForm = {
      id: player.id,
      name: player.name,
      number: player.number,
      teamId: player.teamId,
      positionId: player.positionId,
    };
  }

  async deletePlayer(playerId: number): Promise<void> {
    const teamId = this.selectedTeamId();
    if (teamId == null) {
      return;
    }
    this.clearMessages();
    try {
      await this.tournamentApi.deletePlayer(playerId);
      await this.loadSquad(teamId);
    } catch {
      this.errorMessage.set('Failed to delete player.');
    }
  }

  private async loadSquad(teamId: number): Promise<void> {
    const [coaches, players] = await Promise.all([
      this.tournamentApi.getCoachesByTeam(teamId),
      this.tournamentApi.getPlayersByTeam(teamId),
    ]);
    this.coaches.set(coaches);
    this.players.set(players.sort((a, b) => a.number - b.number));
  }

  private initForms(): void {
    const groups = this.context.groupsForSelectedWorldCup();
    if (this.teamForm.groupId === 0 && groups.length > 0) {
      this.teamForm.groupId = groups[0].id;
    }
    if (this.teamForm.countryId === 0 && this.countries().length > 0) {
      this.teamForm.countryId = this.countries()[0].id;
    }
    if (this.playerForm.positionId === 0 && this.positions().length > 0) {
      this.playerForm.positionId = this.positions()[0].id;
    }
  }

  private clearMessages(): void {
    this.message.set(null);
    this.errorMessage.set(null);
  }

  private readApiError(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error;
      if (typeof body === 'object' && body !== null && 'error' in body) {
        const message = (body as { error?: string }).error;
        if (typeof message === 'string' && message.trim().length > 0) {
          return message;
        }
      }
    }

    return fallback;
  }
}
