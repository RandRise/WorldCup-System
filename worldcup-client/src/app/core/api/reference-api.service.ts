import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AddStadiumRequest,
  City,
  Country,
  Stadium,
  UpdateStadiumRequest,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ReferenceApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  getCountries(): Promise<Country[]> {
    return firstValueFrom(this.http.get<Country[]>(`${this.baseUrl}/Country/GetAllCountries`));
  }

  getCities(): Promise<City[]> {
    return firstValueFrom(this.http.get<City[]>(`${this.baseUrl}/City/GetAllCities`));
  }

  getStadiums(): Promise<Stadium[]> {
    return firstValueFrom(this.http.get<Stadium[]>(`${this.baseUrl}/Stadium/GetStadiums`));
  }

  addStadium(request: AddStadiumRequest): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.baseUrl}/Stadium/AddStadium`, request));
  }

  updateStadium(request: UpdateStadiumRequest): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.baseUrl}/Stadium/UpdateStadium`, request));
  }

  importCountriesCsv(file: File): Promise<void> {
    const formData = new FormData();
    formData.append('file', file);
    return firstValueFrom(this.http.post<void>(`${this.baseUrl}/Country/AddCountries`, formData));
  }

  importCitiesCsv(file: File, countryId: number): Promise<void> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('countryId', String(countryId));
    return firstValueFrom(this.http.post<void>(`${this.baseUrl}/City/AddCities`, formData));
  }

  importStadiumsCsv(file: File): Promise<void> {
    const formData = new FormData();
    formData.append('file', file);
    return firstValueFrom(
      this.http.post<void>(`${this.baseUrl}/Stadium/ImportStadiumsFromCsv`, formData),
    );
  }
}
