/**
 * Production environment (default `ng build` / `npm run build`).
 *
 * Phase 15 Task 1 (`p15-spa-url`): local ship rehearsal uses the same API origin
 * as Dev (`http://localhost:5055`) so a production build can be exercised without
 * a public domain. When you have a real host, replace `apiUrl` with that origin
 * (scheme + host [+ port], no trailing slash) and rebuild.
 *
 * Local Dev (`ng serve`) still uses `environment.development.ts` via
 * angular.json fileReplacements — leave that file on localhost.
 *
 * Detail: docs/changes/phase-15-spa-url.md · phase-14-spa-prod.md
 */
export const environment = {
  production: true,
  apiUrl: 'http://localhost:5055',
};
