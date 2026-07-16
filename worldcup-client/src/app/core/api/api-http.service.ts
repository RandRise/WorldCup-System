import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

/**
 * Command-style API calls (POST/DELETE) that return plain-text success messages.
 * Uses responseType 'text' so Angular does not fail JSON parsing on 200 OK bodies
 * like "Match added successfully." from the ASP.NET API.
 */
@Injectable({ providedIn: 'root' })
export class ApiHttpService {
  private readonly http = inject(HttpClient);

  postCommand(url: string, body: unknown = {}): Promise<string> {
    return firstValueFrom(this.http.post(url, body, { responseType: 'text' }));
  }

  deleteCommand(url: string): Promise<string> {
    return firstValueFrom(this.http.delete(url, { responseType: 'text' }));
  }
}
