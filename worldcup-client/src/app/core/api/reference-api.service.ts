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
import { ApiHttpService } from './api-http.service';

@Injectable({ providedIn: 'root' })
export class ReferenceApiService {
  private readonly http = inject(HttpClient);
  private readonly apiHttp = inject(ApiHttpService);
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
    return this.apiHttp.postCommand(`${this.baseUrl}/Stadium/AddStadium`, request);
  }

  updateStadium(request: UpdateStadiumRequest): Promise<void> {
    return this.apiHttp.postCommand(`${this.baseUrl}/Stadium/UpdateStadium`, request);
  }
}
