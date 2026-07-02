import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  GuardResult,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { adminGuard } from './admin.guard';
import { AuthService } from './auth.service';

describe('adminGuard', () => {
  const route = {} as ActivatedRouteSnapshot;
  const state = {} as RouterStateSnapshot;

  function runGuard(isAuthenticated: boolean, isAdmin: boolean): GuardResult {
    TestBed.configureTestingModule({
      providers: [
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: signal(isAuthenticated),
            isAdmin: signal(isAdmin),
          },
        },
        provideRouter([]),
      ],
    });

    return TestBed.runInInjectionContext(() => adminGuard(route, state)) as GuardResult;
  }

  it('allows authenticated admin users', () => {
    expect(runGuard(true, true)).toBeTrue();
  });

  it('redirects unauthenticated users to login', () => {
    const result = runGuard(false, false);
    const router = TestBed.inject(Router);
    expect(router.serializeUrl(result as UrlTree)).toBe('/login');
  });

  it('redirects non-admin users to home', () => {
    const result = runGuard(true, false);
    const router = TestBed.inject(Router);
    expect(router.serializeUrl(result as UrlTree)).toBe('/');
  });
});
