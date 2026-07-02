import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Bet,
  LeaderboardEntry,
  LeaderboardSummary,
  PlaceBetRequest,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class BetApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  getLeaderboard(worldCupId?: number): Promise<LeaderboardEntry[]> {
    let params = new HttpParams();
    if (worldCupId != null) {
      params = params.set('worldCupId', String(worldCupId));
    }
    return firstValueFrom(
      this.http.get<LeaderboardEntry[]>(`${this.baseUrl}/Bet/GetLeaderboard`, { params }),
    );
  }

  getMySummary(worldCupId?: number): Promise<LeaderboardSummary> {
    let params = new HttpParams();
    if (worldCupId != null) {
      params = params.set('worldCupId', String(worldCupId));
    }
    return firstValueFrom(
      this.http.get<LeaderboardSummary>(`${this.baseUrl}/Bet/GetMySummary`, { params }),
    );
  }

  getMyBets(): Promise<Bet[]> {
    return firstValueFrom(this.http.get<Bet[]>(`${this.baseUrl}/Bet/GetMyBets`));
  }

  getMyActiveBets(): Promise<Bet[]> {
    return firstValueFrom(this.http.get<Bet[]>(`${this.baseUrl}/Bet/GetMyActiveBets`));
  }

  getMyBetForMatch(matchId: number): Promise<Bet | null> {
    return firstValueFrom(this.http.get<Bet | null>(`${this.baseUrl}/Bet/GetMyBetForMatch/${matchId}`));
  }

  placeBet(request: PlaceBetRequest): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.baseUrl}/Bet/PlaceBet`, request));
  }
}
