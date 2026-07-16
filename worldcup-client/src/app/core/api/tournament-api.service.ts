import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AddCoachRequest,
  AddPlayerRequest,
  AddTeamRequest,
  Coach,
  Group,
  Player,
  PlayerPosition,
  Team,
  UpdateCoachRequest,
  UpdatePlayerRequest,
  UpdateTeamRequest,
  WorldCup,
} from '../models/api.models';
import { ApiHttpService } from './api-http.service';

@Injectable({ providedIn: 'root' })
export class TournamentApiService {
  private readonly http = inject(HttpClient);
  private readonly apiHttp = inject(ApiHttpService);
  private readonly baseUrl = environment.apiUrl;

  getWorldCups(): Promise<WorldCup[]> {
    return firstValueFrom(this.http.get<WorldCup[]>(`${this.baseUrl}/WorldCup/GetWorldCups`));
  }

  getGroups(): Promise<Group[]> {
    return firstValueFrom(this.http.get<Group[]>(`${this.baseUrl}/Group/GetGroups`));
  }

  getTeams(): Promise<Team[]> {
    return firstValueFrom(this.http.get<Team[]>(`${this.baseUrl}/Team/GetTeams`));
  }

  getTeamById(id: number): Promise<Team> {
    return firstValueFrom(this.http.get<Team>(`${this.baseUrl}/Team/GetTeamById/${id}`));
  }

  addTeam(request: AddTeamRequest): Promise<void> {
    return this.apiHttp.postCommand(`${this.baseUrl}/Team/AddTeam`, request).then(() => undefined);
  }

  updateTeam(request: UpdateTeamRequest): Promise<void> {
    return this.apiHttp.postCommand(`${this.baseUrl}/Team/UpdateTeam`, request).then(() => undefined);
  }

  deleteTeam(id: number): Promise<void> {
    return this.apiHttp.deleteCommand(`${this.baseUrl}/Team/DeleteTeam/${id}`).then(() => undefined);
  }

  getCoaches(): Promise<Coach[]> {
    return firstValueFrom(this.http.get<Coach[]>(`${this.baseUrl}/Coach/GetCoaches`));
  }

  getCoachesByTeam(teamId: number): Promise<Coach[]> {
    return firstValueFrom(this.http.get<Coach[]>(`${this.baseUrl}/Coach/GetCoachesByTeam/${teamId}`));
  }

  addCoach(request: AddCoachRequest): Promise<void> {
    return this.apiHttp.postCommand(`${this.baseUrl}/Coach/AddCoach`, request).then(() => undefined);
  }

  updateCoach(request: UpdateCoachRequest): Promise<void> {
    return this.apiHttp.postCommand(`${this.baseUrl}/Coach/UpdateCoach`, request).then(() => undefined);
  }

  deleteCoach(id: number): Promise<void> {
    return this.apiHttp.deleteCommand(`${this.baseUrl}/Coach/DeleteCoach/${id}`).then(() => undefined);
  }

  getPlayers(): Promise<Player[]> {
    return firstValueFrom(this.http.get<Player[]>(`${this.baseUrl}/Player/GetPlayers`));
  }

  getPlayersByTeam(teamId: number): Promise<Player[]> {
    return firstValueFrom(this.http.get<Player[]>(`${this.baseUrl}/Player/GetPlayersByTeam/${teamId}`));
  }

  getPlayerPositions(): Promise<PlayerPosition[]> {
    return firstValueFrom(
      this.http.get<PlayerPosition[]>(`${this.baseUrl}/PlayerPosition/GetPlayerPositions`),
    );
  }

  addPlayer(request: AddPlayerRequest): Promise<void> {
    return this.apiHttp.postCommand(`${this.baseUrl}/Player/AddPlayer`, request).then(() => undefined);
  }

  updatePlayer(request: UpdatePlayerRequest): Promise<void> {
    return this.apiHttp
      .postCommand(`${this.baseUrl}/Player/UpdatePlayer`, request)
      .then(() => undefined);
  }

  deletePlayer(id: number): Promise<void> {
    return this.apiHttp
      .deleteCommand(`${this.baseUrl}/Player/DeletePlayer/${id}`)
      .then(() => undefined);
  }
}
