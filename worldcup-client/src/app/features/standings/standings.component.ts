import { Component, effect, inject, signal } from '@angular/core';
import { StandingsApiService } from '../../core/api/standings-api.service';
import { Group, Standing } from '../../core/models/api.models';
import { WorldCupContextService } from '../../core/services/worldcup-context.service';
import { WorldCupSelectorComponent } from '../shared/world-cup-selector/world-cup-selector.component';

@Component({
  selector: 'app-standings',
  imports: [WorldCupSelectorComponent],
  templateUrl: './standings.component.html',
  styleUrl: './standings.component.scss',
})
export class StandingsComponent {
  private readonly standingsApi = inject(StandingsApiService);
  protected readonly context = inject(WorldCupContextService);

  protected readonly selectedGroupId = signal<number | null>(null);
  protected readonly standings = signal<Standing[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  constructor() {
    effect(() => {
      const groups = this.context.groupsForSelectedWorldCup();
      const currentGroupId = this.selectedGroupId();
      const groupBelongsToSelection =
        currentGroupId != null && groups.some((group) => group.id === currentGroupId);

      if (groups.length === 0) {
        this.selectedGroupId.set(null);
      } else if (!groupBelongsToSelection) {
        this.selectedGroupId.set(groups[0].id);
      }

      const groupId = this.selectedGroupId();
      if (groupId != null) {
        void this.loadStandings(groupId);
      }
    });
  }

  selectGroup(group: Group): void {
    this.selectedGroupId.set(group.id);
  }

  private async loadStandings(groupId: number): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const rows = await this.standingsApi.getGroupStandings(groupId);
      this.standings.set(rows);
    } catch {
      this.errorMessage.set('Failed to load standings.');
      this.standings.set([]);
    } finally {
      this.isLoading.set(false);
    }
  }
}
