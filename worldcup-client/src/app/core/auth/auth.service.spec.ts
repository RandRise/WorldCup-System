import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

describe('AuthService', () => {
  let authService: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService, provideRouter([])],
    });

    authService = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('stores token and loads current user on login', async () => {
    const loginPromise = authService.login({
      email: 'user@example.com',
      password: 'Password1!',
    });

    const loginRequest = httpMock.expectOne(`${environment.apiUrl}/User/Login`);
    loginRequest.flush({ token: 'jwt-token', expiration: '2099-01-01T00:00:00Z' });

    await Promise.resolve();

    const meRequest = httpMock.expectOne(`${environment.apiUrl}/User/GetMe`);
    meRequest.flush({
      id: 1,
      name: 'Test User',
      email: 'user@example.com',
      roles: ['User'],
    });

    await loginPromise;

    expect(localStorage.getItem('wc_access_token')).toBe('jwt-token');
    expect(authService.isAuthenticated()).toBeTrue();
    expect(authService.currentUser()?.name).toBe('Test User');
  });

  it('clears session when GetMe fails during initializeSession', async () => {
    localStorage.setItem('wc_access_token', 'expired-token');

    const initPromise = authService.initializeSession();
    const meRequest = httpMock.expectOne(`${environment.apiUrl}/User/GetMe`);
    meRequest.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    await initPromise;

    expect(authService.isAuthenticated()).toBeFalse();
    expect(localStorage.getItem('wc_access_token')).toBeNull();
  });

  it('logout clears token and user state', () => {
    localStorage.setItem('wc_access_token', 'jwt-token');

    authService.logout();

    expect(localStorage.getItem('wc_access_token')).toBeNull();
    expect(authService.isAuthenticated()).toBeFalse();
  });
});
