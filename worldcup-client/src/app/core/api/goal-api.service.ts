import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AddGoalRequest, Goal } from '../models/api.models';
import { ApiHttpService } from './api-http.service';

@Injectable({ providedIn: 'root' })
export class GoalApiService {
  private readonly http = inject(HttpClient);
  private readonly apiHttp = inject(ApiHttpService);
  private readonly baseUrl = environment.apiUrl;

  getGoalsByMatch(matchId: number): Promise<Goal[]> {
    return firstValueFrom(this.http.get<Goal[]>(`${this.baseUrl}/Goal/GetGoalsByMatch/${matchId}`));
  }

  addGoal(request: AddGoalRequest): Promise<string> {
    return this.apiHttp.postCommand(`${this.baseUrl}/Goal/AddGoal`, request);
  }

  deleteGoal(id: number): Promise<string> {
    return this.apiHttp.deleteCommand(`${this.baseUrl}/Goal/DeleteGoal/${id}`);
  }
}
