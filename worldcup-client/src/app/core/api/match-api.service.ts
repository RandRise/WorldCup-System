import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AddMatchRequest, LiveSnapshot, Match, MatchDetail, SetExternalMatchIdRequest, SyncFinishedResults, SyncMatchResult, UpdateMatchRequest } from '../models/api.models';
import { ApiHttpService } from './api-http.service';

@Injectable({ providedIn: 'root' })
export class MatchApiService {
  private readonly http = inject(HttpClient);
  private readonly apiHttp = inject(ApiHttpService);
  private readonly baseUrl = environment.apiUrl;

  getMatches(): Promise<Match[]> {
    return firstValueFrom(this.http.get<Match[]>(`${this.baseUrl}/Match/GetMatches`));
  }

  getMatchById(id: number): Promise<MatchDetail> {
    return firstValueFrom(this.http.get<MatchDetail>(`${this.baseUrl}/Match/GetMatchById/${id}`));
  }

  getLiveSnapshot(worldCupId: number): Promise<LiveSnapshot> {
    const params = new HttpParams().set('worldCupId', String(worldCupId));
    return firstValueFrom(
      this.http.get<LiveSnapshot>(`${this.baseUrl}/Match/GetLiveSnapshot`, { params }),
    );
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
    return this.apiHttp.postCommand(`${this.baseUrl}/Match/AddMatch`, request).then(() => undefined);
  }

  updateMatch(request: UpdateMatchRequest): Promise<void> {
    return this.apiHttp
      .postCommand(`${this.baseUrl}/Match/UpdateMatch`, request)
      .then(() => undefined);
  }

  deleteMatch(id: number): Promise<void> {
    return this.apiHttp.deleteCommand(`${this.baseUrl}/Match/DeleteMatch/${id}`).then(() => undefined);
  }

  setExternalMatchId(request: SetExternalMatchIdRequest): Promise<void> {
    return this.apiHttp
      .postCommand(`${this.baseUrl}/Match/SetExternalMatchId`, request)
      .then(() => undefined);
  }

  syncResult(matchId: number): Promise<SyncMatchResult> {
    return firstValueFrom(
      this.http.post<SyncMatchResult>(`${this.baseUrl}/Match/SyncResult/${matchId}`, {}),
    );
  }

  syncFinishedResults(worldCupId: number): Promise<SyncFinishedResults> {
    const params = new HttpParams().set('worldCupId', String(worldCupId));
    return firstValueFrom(
      this.http.post<SyncFinishedResults>(`${this.baseUrl}/Match/SyncFinishedResults`, {}, { params }),
    );
  }

  /** Scorers-only backfill — FT score and bets unchanged. */
  syncScorers(matchId: number): Promise<SyncMatchResult> {
    return firstValueFrom(
      this.http.post<SyncMatchResult>(`${this.baseUrl}/Match/SyncScorers/${matchId}`, {}),
    );
  }

  /** Batch scorers-only backfill for mapped fixtures in a World Cup. */
  syncScorersForWorldCup(worldCupId: number): Promise<SyncFinishedResults> {
    const params = new HttpParams().set('worldCupId', String(worldCupId));
    return firstValueFrom(
      this.http.post<SyncFinishedResults>(`${this.baseUrl}/Match/SyncScorersForWorldCup`, {}, { params }),
    );
  }
}
