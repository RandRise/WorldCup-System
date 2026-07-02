import { Component, computed, inject } from '@angular/core';
import { WorldCupContextService } from '../../../core/services/worldcup-context.service';

@Component({
  selector: 'app-world-cup-selector',
  templateUrl: './world-cup-selector.component.html',
  styleUrl: './world-cup-selector.component.scss',
})
export class WorldCupSelectorComponent {
  protected readonly context = inject(WorldCupContextService);

  protected readonly selectedId = computed(() => this.context.selectedWorldCupId());

  onSelect(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.context.selectWorldCup(Number(select.value));
  }
}
