import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Standing } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class StandingsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  getGroupStandings(groupId: number): Promise<Standing[]> {
    return firstValueFrom(
      this.http.get<Standing[]>(`${this.baseUrl}/Standing/GetGroupStandings/${groupId}`),
    );
  }
}
