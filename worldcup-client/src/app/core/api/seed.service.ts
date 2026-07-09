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

export interface TournamentDataResetResult {
  betResultsRemoved: number;
  betsRemoved: number;
  goalsRemoved: number;
  cardsRemoved: number;
  teamStatsRemoved: number;
  matchesRemoved: number;
  playersRemoved: number;
  coachesRemoved: number;
  teamsRemoved: number;
  groupsRemoved: number;
  worldCupsRemoved: number;
  stadiumsRemoved: number;
  citiesRemoved: number;
  countriesRemoved: number;
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

  clearTournamentData(): Promise<TournamentDataResetResult> {
    return firstValueFrom(
      this.http.post<TournamentDataResetResult>(`${environment.apiUrl}/Seed/ClearTournamentData`, {}),
    );
  }
}
