import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiHttpService } from '../api/api-http.service';
import {
  CurrentUser,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
} from '../models/auth.models';

const TOKEN_STORAGE_KEY = 'wc_access_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiHttp = inject(ApiHttpService);
  private readonly router = inject(Router);

  private readonly currentUserSignal = signal<CurrentUser | null>(null);

  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUserSignal() !== null);
  readonly isAdmin = computed(() =>
    (this.currentUserSignal()?.roles ?? []).includes('Admin'),
  );

  getToken(): string | null {
    return localStorage.getItem(TOKEN_STORAGE_KEY);
  }

  async initializeSession(): Promise<void> {
    if (!this.getToken()) {
      this.currentUserSignal.set(null);
      return;
    }

    try {
      const user = await firstValueFrom(
        this.http.get<CurrentUser>(`${environment.apiUrl}/User/GetMe`),
      );
      this.currentUserSignal.set(user);
    } catch {
      this.clearSession();
    }
  }

  async login(request: LoginRequest): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<LoginResponse>(`${environment.apiUrl}/User/Login`, request),
    );
    localStorage.setItem(TOKEN_STORAGE_KEY, response.token);
    await this.initializeSession();
  }

  async register(request: RegisterRequest): Promise<void> {
    await this.apiHttp.postCommand(`${environment.apiUrl}/User/CreateNewUser`, request);
  }

  logout(): void {
    this.clearSession();
    void this.router.navigate(['/login']);
  }

  private clearSession(): void {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    this.currentUserSignal.set(null);
  }
}
