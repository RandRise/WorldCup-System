import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TeamStats, UpdateTeamStatsRequest } from '../models/api.models';
import { ApiHttpService } from './api-http.service';

@Injectable({ providedIn: 'root' })
export class TeamStatsApiService {
  private readonly http = inject(HttpClient);
  private readonly apiHttp = inject(ApiHttpService);
  private readonly baseUrl = environment.apiUrl;

  getTeamStatsByMatch(matchId: number): Promise<TeamStats[]> {
    return firstValueFrom(
      this.http.get<TeamStats[]>(`${this.baseUrl}/TeamStats/GetTeamStatsByMatch/${matchId}`),
    );
  }

  updateTeamStats(request: UpdateTeamStatsRequest): Promise<void> {
    return this.apiHttp
      .postCommand(`${this.baseUrl}/TeamStats/UpdateTeamStats`, request)
      .then(() => undefined);
  }
}
