import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DemoSeedResult {
  countriesAdded: number;
  citiesAdded: number;
  stadiumsAdded: number;
  worldCupId: number;
  groupsAdded: number;
  teamsAdded: number;
  alreadySeeded: boolean;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class SeedService {
  private readonly http = inject(HttpClient);

  loadWorldCup2026Demo(): Promise<DemoSeedResult> {
    return firstValueFrom(
      this.http.post<DemoSeedResult>(`${environment.apiUrl}/Seed/LoadWorldCup2026Demo`, {}),
    );
  }
}
