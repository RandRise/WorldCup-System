import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AddCardRequest, Card } from '../models/api.models';
import { ApiHttpService } from './api-http.service';

@Injectable({ providedIn: 'root' })
export class CardApiService {
  private readonly http = inject(HttpClient);
  private readonly apiHttp = inject(ApiHttpService);
  private readonly baseUrl = environment.apiUrl;

  getCardsByMatch(matchId: number): Promise<Card[]> {
    return firstValueFrom(this.http.get<Card[]>(`${this.baseUrl}/Card/GetCardsByMatch/${matchId}`));
  }

  addCard(request: AddCardRequest): Promise<void> {
    return this.apiHttp.postCommand(`${this.baseUrl}/Card/AddCard`, request).then(() => undefined);
  }

  deleteCard(id: number): Promise<void> {
    return this.apiHttp.deleteCommand(`${this.baseUrl}/Card/DeleteCard/${id}`).then(() => undefined);
  }
}
