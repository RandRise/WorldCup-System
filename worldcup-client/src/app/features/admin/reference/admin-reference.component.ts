import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ReferenceApiService } from '../../../core/api/reference-api.service';
import { City, Country, Stadium } from '../../../core/models/api.models';

type ReferenceTab = 'countries' | 'cities' | 'stadiums';

@Component({
  selector: 'app-admin-reference',
  imports: [FormsModule],
  templateUrl: './admin-reference.component.html',
  styleUrl: './admin-reference.component.scss',
})
export class AdminReferenceComponent implements OnInit {
  private readonly referenceApi = inject(ReferenceApiService);

  protected readonly activeTab = signal<ReferenceTab>('countries');
  protected readonly isLoading = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly countries = signal<Country[]>([]);
  protected readonly cities = signal<City[]>([]);
  protected readonly stadiums = signal<Stadium[]>([]);

  protected readonly cityCountryFilter = signal<number | null>(null);
  protected readonly filteredCities = computed(() => {
    const filter = this.cityCountryFilter();
    const allCities = this.cities();
    if (filter == null) {
      return allCities;
    }
    return allCities.filter((city) => city.countryId === filter);
  });

  protected stadiumForm = { id: 0, name: '', cityId: 0 };
  protected importCountryId = 0;

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  setTab(tab: ReferenceTab): void {
    this.activeTab.set(tab);
    this.clearMessages();
  }

  async reload(): Promise<void> {
    this.isLoading.set(true);
    this.clearMessages();
    try {
      const [countries, cities, stadiums] = await Promise.all([
        this.referenceApi.getCountries(),
        this.referenceApi.getCities(),
        this.referenceApi.getStadiums(),
      ]);
      this.countries.set(countries.sort((a, b) => a.name.localeCompare(b.name)));
      this.cities.set(cities.sort((a, b) => a.name.localeCompare(b.name)));
      this.stadiums.set(stadiums.sort((a, b) => a.name.localeCompare(b.name)));
      if (this.importCountryId === 0 && countries.length > 0) {
        const unitedStates = countries.find((country) => country.name === 'United States');
        this.importCountryId = unitedStates?.id ?? countries[0].id;
      }
      if (this.stadiumForm.cityId === 0 && cities.length > 0) {
        this.stadiumForm.cityId = cities[0].id;
      }
    } catch {
      this.errorMessage.set('Failed to load reference data.');
    } finally {
      this.isLoading.set(false);
    }
  }

  countryName(countryId: number): string {
    return this.countries().find((country) => country.id === countryId)?.name ?? `#${countryId}`;
  }

  cityName(cityId: number): string {
    return this.cities().find((city) => city.id === cityId)?.name ?? `#${cityId}`;
  }

  async onImportCountries(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }
    await this.runImport(() => this.referenceApi.importCountriesCsv(file));
    input.value = '';
  }

  async onImportCities(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file || this.importCountryId <= 0) {
      return;
    }
    await this.runImport(() => this.referenceApi.importCitiesCsv(file, this.importCountryId));
    input.value = '';
  }

  async onImportStadiums(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }
    await this.runImport(() => this.referenceApi.importStadiumsCsv(file));
    input.value = '';
  }

  editStadium(stadium: Stadium): void {
    this.stadiumForm = { id: stadium.id, name: stadium.name, cityId: stadium.cityId };
  }

  resetStadiumForm(): void {
    this.stadiumForm = { id: 0, name: '', cityId: this.cities()[0]?.id ?? 0 };
  }

  async saveStadium(): Promise<void> {
    this.clearMessages();
    try {
      if (this.stadiumForm.id > 0) {
        await this.referenceApi.updateStadium({
          id: this.stadiumForm.id,
          name: this.stadiumForm.name.trim(),
          cityId: this.stadiumForm.cityId,
        });
        this.message.set('Stadium updated.');
      } else {
        await this.referenceApi.addStadium({
          id: 0,
          name: this.stadiumForm.name.trim(),
          cityId: this.stadiumForm.cityId,
        });
        this.message.set('Stadium created.');
      }
      this.resetStadiumForm();
      await this.reload();
    } catch {
      this.errorMessage.set('Failed to save stadium.');
    }
  }

  private async runImport(action: () => Promise<void>): Promise<void> {
    this.clearMessages();
    try {
      await action();
      this.message.set('Import completed.');
      await this.reload();
    } catch {
      this.errorMessage.set('Import failed.');
    }
  }

  private clearMessages(): void {
    this.message.set(null);
    this.errorMessage.set(null);
  }
}
