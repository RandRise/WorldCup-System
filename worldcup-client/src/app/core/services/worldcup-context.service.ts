import { Injectable, computed, inject, signal } from '@angular/core';
import { Group, WorldCup } from '../models/api.models';
import { TournamentApiService } from '../api/tournament-api.service';

@Injectable({ providedIn: 'root' })
export class WorldCupContextService {
  private static readonly storageKey = 'worldcup.selectedId';

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

    const sortedCups = [...worldCups].sort(
      (left, right) =>
        new Date(left.year).getTime() - new Date(right.year).getTime(),
    );

    this.worldCupsSignal.set(sortedCups);
    this.groupsSignal.set(groups);

    const currentId = this.selectedWorldCupIdSignal();
    const storedId = this.readStoredId();
    const resolvedId = this.resolveInitialSelection(sortedCups, currentId, storedId);
    this.selectedWorldCupIdSignal.set(resolvedId);
    this.persistSelectedId(resolvedId);
  }

  selectWorldCup(worldCupId: number): void {
    if (!Number.isFinite(worldCupId)) {
      return;
    }
    if (this.selectedWorldCupIdSignal() === worldCupId) {
      return;
    }
    this.selectedWorldCupIdSignal.set(worldCupId);
    this.persistSelectedId(worldCupId);
  }

  worldCupLabel(worldCup: WorldCup): string {
    return `FIFA World Cup ${new Date(worldCup.year).getUTCFullYear()}`;
  }

  selectedWorldCupLabel(): string {
    const selected = this.selectedWorldCup();
    return selected ? this.worldCupLabel(selected) : 'No tournament selected';
  }

  private resolveInitialSelection(
    worldCups: WorldCup[],
    currentId: number | null,
    storedId: number | null,
  ): number | null {
    if (worldCups.length === 0) {
      return null;
    }

    if (currentId != null && worldCups.some((worldCup) => worldCup.id === currentId)) {
      return currentId;
    }

    if (storedId != null && worldCups.some((worldCup) => worldCup.id === storedId)) {
      return storedId;
    }

    const preferred =
      worldCups.find((worldCup) => {
        const date = new Date(worldCup.year);
        return date.getUTCFullYear() === 2026 && date.getUTCMonth() === 5;
      }) ??
      worldCups.find(
        (worldCup) => new Date(worldCup.year).getUTCFullYear() === 2026,
      );

    return preferred?.id ?? worldCups[0].id;
  }

  private readStoredId(): number | null {
    try {
      const raw = localStorage.getItem(WorldCupContextService.storageKey);
      if (raw == null || raw.trim() === '') {
        return null;
      }
      const parsed = Number(raw);
      return Number.isFinite(parsed) ? parsed : null;
    } catch {
      return null;
    }
  }

  private persistSelectedId(worldCupId: number | null): void {
    try {
      if (worldCupId == null) {
        localStorage.removeItem(WorldCupContextService.storageKey);
        return;
      }
      localStorage.setItem(WorldCupContextService.storageKey, String(worldCupId));
    } catch {
      // Ignore quota / private-mode failures.
    }
  }
}
