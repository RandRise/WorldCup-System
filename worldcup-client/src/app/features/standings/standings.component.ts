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

  private loadToken = 0;

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
        this.standings.set([]);
        return;
      }

      if (!groupBelongsToSelection) {
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
    const token = ++this.loadToken;
    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const rows = await this.standingsApi.getGroupStandings(groupId);
      if (token !== this.loadToken || this.selectedGroupId() !== groupId) {
        return;
      }
      this.standings.set(rows);
    } catch {
      if (token !== this.loadToken || this.selectedGroupId() !== groupId) {
        return;
      }
      this.errorMessage.set('Failed to load standings.');
      this.standings.set([]);
    } finally {
      if (token === this.loadToken) {
        this.isLoading.set(false);
      }
    }
  }
}
