import { Component, inject, signal } from '@angular/core';
import { DemoSeedResult, SeedService } from '../../../core/api/seed.service';
import { WorldCupContextService } from '../../../core/services/worldcup-context.service';

@Component({
  selector: 'app-admin-seed',
  templateUrl: './admin-seed.component.html',
  styleUrl: './admin-seed.component.scss',
})
export class AdminSeedComponent {
  private readonly seedService = inject(SeedService);
  private readonly worldCupContext = inject(WorldCupContextService);

  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly result = signal<DemoSeedResult | null>(null);

  async loadDemoData(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    try {
      const seedResult = await this.seedService.loadWorldCup2026Demo();
      this.result.set(seedResult);
      await this.worldCupContext.initialize();
      if (seedResult.worldCupId > 0) {
        this.worldCupContext.selectWorldCup(seedResult.worldCupId);
      }
    } catch {
      this.errorMessage.set('Failed to load demo data. Ensure you are logged in as Admin and the API is running.');
    } finally {
      this.isLoading.set(false);
    }
  }
}
