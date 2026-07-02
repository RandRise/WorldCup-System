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
import { authGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('authGuard', () => {
  const route = {} as ActivatedRouteSnapshot;
  const state = {} as RouterStateSnapshot;

  function runGuard(isAuthenticated: boolean): GuardResult {
    TestBed.configureTestingModule({
      providers: [
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: signal(isAuthenticated),
          },
        },
        provideRouter([]),
      ],
    });

    return TestBed.runInInjectionContext(() => authGuard(route, state)) as GuardResult;
  }

  it('allows authenticated users', () => {
    expect(runGuard(true)).toBeTrue();
  });

  it('redirects unauthenticated users to login', () => {
    const result = runGuard(false);
    const router = TestBed.inject(Router);
    expect(router.serializeUrl(result as UrlTree)).toBe('/login');
  });
});
