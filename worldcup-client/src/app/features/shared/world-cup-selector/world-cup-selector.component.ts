import { Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { WorldCupContextService } from '../../../core/services/worldcup-context.service';

@Component({
  selector: 'app-world-cup-selector',
  imports: [FormsModule],
  templateUrl: './world-cup-selector.component.html',
  styleUrl: './world-cup-selector.component.scss',
})
export class WorldCupSelectorComponent {
  protected readonly context = inject(WorldCupContextService);

  protected readonly selectedId = computed(() => this.context.selectedWorldCupId());

  onSelect(worldCupId: number | string | null): void {
    const id = typeof worldCupId === 'number' ? worldCupId : Number(worldCupId);
    if (!Number.isFinite(id)) {
      return;
    }
    this.context.selectWorldCup(id);
  }
}
