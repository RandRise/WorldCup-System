import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AddMatchRequest, Match, UpdateMatchRequest } from '../models/api.models';
import { ApiHttpService } from './api-http.service';

@Injectable({ providedIn: 'root' })
export class MatchApiService {
  private readonly http = inject(HttpClient);
  private readonly apiHttp = inject(ApiHttpService);
  private readonly baseUrl = environment.apiUrl;

  getMatches(): Promise<Match[]> {
    return firstValueFrom(this.http.get<Match[]>(`${this.baseUrl}/Match/GetMatches`));
  }

  getFixturesByWorldCup(worldCupId: number): Promise<Match[]> {
    return firstValueFrom(
      this.http.get<Match[]>(`${this.baseUrl}/Match/GetFixturesByWorldCup/${worldCupId}`),
    );
  }

  getFixturesByGroup(groupId: number): Promise<Match[]> {
    return firstValueFrom(
      this.http.get<Match[]>(`${this.baseUrl}/Match/GetFixturesByGroup/${groupId}`),
    );
  }

  addMatch(request: AddMatchRequest): Promise<void> {
    return this.apiHttp.postCommand(`${this.baseUrl}/Match/AddMatch`, request);
  }

  updateMatch(request: UpdateMatchRequest): Promise<void> {
    return this.apiHttp.postCommand(`${this.baseUrl}/Match/UpdateMatch`, request);
  }

  deleteMatch(id: number): Promise<void> {
    return this.apiHttp.deleteCommand(`${this.baseUrl}/Match/DeleteMatch/${id}`);
  }

  resolveBetsForMatch(matchId: number): Promise<{ resolvedCount: number; message: string }> {
    return firstValueFrom(
      this.http.post<{ resolvedCount: number; message: string }>(
        `${this.baseUrl}/Bet/ResolveBetsForMatch/${matchId}`,
        {},
      ),
    );
  }
}
