import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Bracket, GenerateBracketRequest, GenerateBracketResult } from '../models/api.models';
import { ApiHttpService } from './api-http.service';

@Injectable({ providedIn: 'root' })
export class KnockoutApiService {
  private readonly http = inject(HttpClient);
  private readonly apiHttp = inject(ApiHttpService);
  private readonly baseUrl = environment.apiUrl;

  getBracket(worldCupId: number): Promise<Bracket> {
    return firstValueFrom(
      this.http.get<Bracket>(`${this.baseUrl}/Knockout/GetBracket/${worldCupId}`),
    );
  }

  generateBracket(request: GenerateBracketRequest): Promise<GenerateBracketResult> {
    return firstValueFrom(
      this.http.post<GenerateBracketResult>(`${this.baseUrl}/Knockout/GenerateBracket`, request),
    );
  }

  advanceFromMatch(matchId: number): Promise<string> {
    return this.apiHttp.postCommand(`${this.baseUrl}/Knockout/AdvanceFromMatch/${matchId}`, {});
  }
}
