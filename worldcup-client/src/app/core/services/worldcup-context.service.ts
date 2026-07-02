import { Injectable, computed, inject, signal } from '@angular/core';
import { Group, WorldCup } from '../models/api.models';
import { TournamentApiService } from '../api/tournament-api.service';

@Injectable({ providedIn: 'root' })
export class WorldCupContextService {
  private readonly tournamentApi = inject(TournamentApiService);

  private readonly worldCupsSignal = signal<WorldCup[]>([]);
  private readonly groupsSignal = signal<Group[]>([]);
  private readonly selectedWorldCupIdSignal = signal<number | null>(null);

  readonly worldCups = this.worldCupsSignal.asReadonly();
  readonly groups = this.groupsSignal.asReadonly();
  readonly selectedWorldCupId = this.selectedWorldCupIdSignal.asReadonly();

  readonly selectedWorldCup = computed(() => {
    const id = this.selectedWorldCupIdSignal();
    return this.worldCupsSignal().find((worldCup) => worldCup.id === id) ?? null;
  });

  readonly groupsForSelectedWorldCup = computed(() => {
    const worldCupId = this.selectedWorldCupIdSignal();
    if (worldCupId == null) {
      return [];
    }
    return this.groupsSignal()
      .filter((group) => group.worldCupId === worldCupId)
      .sort((left, right) => left.name.localeCompare(right.name));
  });

  async initialize(): Promise<void> {
    const [worldCups, groups] = await Promise.all([
      this.tournamentApi.getWorldCups(),
      this.tournamentApi.getGroups(),
    ]);

    this.worldCupsSignal.set(worldCups);
    this.groupsSignal.set(groups);

    if (this.selectedWorldCupIdSignal() == null && worldCups.length > 0) {
      const preferred = worldCups.find(
        (worldCup) => new Date(worldCup.year).getUTCFullYear() === 2026,
      );
      this.selectedWorldCupIdSignal.set(preferred?.id ?? worldCups[0].id);
    }
  }

  selectWorldCup(worldCupId: number): void {
    this.selectedWorldCupIdSignal.set(worldCupId);
  }

  worldCupLabel(worldCup: WorldCup): string {
    return `FIFA World Cup ${new Date(worldCup.year).getUTCFullYear()}`;
  }
}
