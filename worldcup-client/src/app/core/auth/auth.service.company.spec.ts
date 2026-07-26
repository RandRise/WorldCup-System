import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

describe('AuthService company roles', () => {
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

  it('isCompanyAdmin is true when GetMe includes CompanyAdmin', async () => {
    const applyPromise = authService.applyAccessToken('company-admin-jwt');

    const meRequest = httpMock.expectOne(`${environment.apiUrl}/User/GetMe`);
    meRequest.flush({
      id: 2,
      name: 'Pool Admin',
      email: 'admin@acme.test',
      roles: ['User', 'CompanyAdmin'],
    });

    await expectAsync(applyPromise).toBeResolvedTo(true);

    expect(localStorage.getItem('wc_access_token')).toBe('company-admin-jwt');
    expect(authService.isCompanyAdmin()).toBeTrue();
    expect(authService.isAdmin()).toBeFalse();
  });

  it('isCompanyAdmin is false for regular User role', async () => {
    const applyPromise = authService.applyAccessToken('user-jwt');

    const meRequest = httpMock.expectOne(`${environment.apiUrl}/User/GetMe`);
    meRequest.flush({
      id: 3,
      name: 'Member',
      email: 'member@acme.test',
      roles: ['User'],
    });

    await expectAsync(applyPromise).toBeResolvedTo(true);

    expect(authService.isCompanyAdmin()).toBeFalse();
  });

  it('keeps token when GetMe fails after applyAccessToken', async () => {
    localStorage.setItem('wc_access_token', 'old-jwt');

    const applyPromise = authService.applyAccessToken('new-jwt');
    const meRequest = httpMock.expectOne(`${environment.apiUrl}/User/GetMe`);
    meRequest.flush('error', { status: 500, statusText: 'Server Error' });

    await expectAsync(applyPromise).toBeResolvedTo(false);

    expect(localStorage.getItem('wc_access_token')).toBe('new-jwt');
  });
});
