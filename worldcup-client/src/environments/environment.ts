/**
 * Production environment (default `ng build` / `npm run build`).
 *
 * Before a real deploy, set `apiUrl` to the public API base (no trailing slash),
 * e.g. `https://api.example.com`. Do not leave REPLACE_ME in a shipped bundle.
 *
 * Local Dev uses `environment.development.ts` via angular.json fileReplacements
 * (`ng serve` / `build:development`) — leave that file on localhost.
 *
 * Detail: docs/changes/phase-14-spa-prod.md
 */
export const environment = {
  production: true,
  apiUrl: 'https://api.REPLACE_ME.example',
};
