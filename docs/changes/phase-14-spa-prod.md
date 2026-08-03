# WorldCup System — Phase 14 Task 5: SPA Production Build

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 14 plan](phase-14-planned.md) · [Compose](phase-14-compose.md) · [CORS/JWT](phase-14-cors.md)

## Phase 14 Task 5 — SPA production build (`p14-spa-prod`)

Opened **28 Jul 2026**. Checklist: [Roadmap Phase 14](../roadmap.md#phase-14) — Task 5 **[x]** Done. Next was Task 6 (now Done — [phase-14-migrate-smoke](phase-14-migrate-smoke.md)); formal close is Task 7.

> **Success:** Production Angular build no longer silently embeds `http://localhost:5055`. `environment.ts` uses an obvious `REPLACE_ME` placeholder; operators set the real API base before/during deploy. Build + `dist/` serve path documented. Dev (`environment.development.ts` + `ng serve`) unchanged.

<a id="review-gate"></a>

## Review gate

High/critical items that must clear **before** treating Task 5 as closed (verified **28 Jul 2026** vs git working tree: `environment.ts`, `worldcup-client/README.md`, this page):

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Prod `apiUrl` silently stays localhost | **Critical** | **Cleared** | `environment.ts` → `https://api.REPLACE_ME.example` |
| Personal / private API host baked as default | **High** | **Cleared by design** | Placeholder only; operator replaces per deploy |
| Dev `ng serve` broken | **High** | **Cleared** | `angular.json` `fileReplacements` still swap in `environment.development.ts` (`apiUrl: http://localhost:5055`) |
| `environment.development.ts` deleted / changed | **High** | **Cleared by design** | Untouched — localhost Dev |
| Wrong client path documented | **High** | **Cleared** | Canonical: `WorldCup-System/worldcup-client` only |
| Dist path wrong for application builder | **High** | **Cleared** | `outputPath` `dist/worldcup-client` → serve **`browser/`** subfolder |
| Dual docs VitePress ↔ git mirror | **High** | **Cleared this Markdown pass** | `docs/` and `WorldCup-System/docs/` identical |
| Operator ships bundle with `REPLACE_ME` still in it | Medium | **Documented** | [Pre-deploy checklist](#pre-deploy-checklist) (ops discipline, not a Task 5 code blocker) |

**Gate verdict:** All Task 5 **high/critical** items **Cleared**. Medium `REPLACE_ME`-in-bundle risk remains ops-documented (non-blocking). Task 6 migrate/smoke Done. Formal Phase 14 close remains Task 7 (`p14-gate`).

---

## Locked approach

| Choice | Decision |
| --- | --- |
| Mechanism | **Documented replace** of `apiUrl` in `src/environments/environment.ts` before production build (or CI sed/replace of the same string) |
| Why not runtime env | Angular v1 ships a static bundle; no Docker SPA service in Compose yet |
| Dev | Keep `environment.development.ts` at `http://localhost:5055`; `ng serve` uses development configuration |
| Default prod value in git | Obvious placeholder `https://api.REPLACE_ME.example` — fails loudly if forgotten |

Optional later (out of Task 5 scope): `fileReplacements` to a host-specific file, or CI token replace — same outcome as editing `apiUrl`.

---

## Operator steps

### 1. Set production API URL

Edit `WorldCup-System/worldcup-client/src/environments/environment.ts`:

```ts
export const environment = {
  production: true,
  apiUrl: 'https://api.example.com', // no trailing slash
};
```

Or replace the placeholder string in CI before `npm run build`. Align with:

| API / SPA concern | Must match |
| --- | --- |
| SPA `apiUrl` | Public API origin (scheme + host [+ port]) |
| `JWT__ValidIssuer` | Same public API URL (typical) |
| `Cors__AllowedOrigins` / `PROD_CORS_ALLOWED_ORIGINS` | SPA origin (where static files are served), **not** the API URL |
| `JWT__ValidAudience` | SPA audience / origin string |

CORS/JWT: [phase-14-cors](phase-14-cors.md). Compose: [phase-14-compose](phase-14-compose.md).

### 2. Build

```powershell
cd WorldCup-System\worldcup-client
npm ci
# or: npm install
npm run build
# equivalent: npx ng build --configuration=production
```

`angular.json` default build configuration is **production**. Artifacts:

```text
WorldCup-System/worldcup-client/dist/worldcup-client/browser/
```

(Angular application builder — serve the **browser** folder contents as the site root.)

### 3. Serve `dist/`

| Option | Notes |
| --- | --- |
| Static host (Netlify, S3+CDN, Azure Static Web Apps, …) | Point root at `dist/worldcup-client/browser` |
| Reverse proxy (nginx / Caddy) | `root` / `file_server` → same folder; SPA fallback to `index.html` for client routes |
| Same host as API | Optional; still set CORS if origins differ, or same-origin proxy `/api` (not wired in v1 — SPA calls absolute `apiUrl`) |

Do **not** use `ng serve` for production.

<a id="pre-deploy-checklist"></a>

### Pre-deploy checklist

- [ ] `apiUrl` in `environment.ts` is the real public API origin (no `REPLACE_ME`, no trailing slash)
- [ ] Built bundle searched for `REPLACE_ME` / `localhost:5055` — neither present in prod assets
- [ ] SPA origin (static host URL) is listed in `Cors__AllowedOrigins` / `PROD_CORS_ALLOWED_ORIGINS`
- [ ] `JWT__ValidIssuer` matches the public API URL; `JWT__ValidAudience` matches the SPA audience/origin
- [ ] Artifacts served from `dist/worldcup-client/browser/` with SPA fallback to `index.html`

### 4. Local Dev (unchanged)

```powershell
cd WorldCup-System\worldcup-client
npm start
# → ng serve → development config → environment.development.ts → localhost:5055
```

---

## Behavior

| Concern | Production | Development |
| --- | --- | --- |
| Env file | `environment.ts` | `environment.development.ts` (via `fileReplacements`) |
| `apiUrl` in git | `https://api.REPLACE_ME.example` | `http://localhost:5055` |
| Build command | `npm run build` | `npm start` / `ng serve` |
| Output | `dist/worldcup-client/browser/` | In-memory / HMR |

---

## Files changed

| Path | Change |
| --- | --- |
| `worldcup-client/src/environments/environment.ts` | Placeholder `apiUrl` + deploy comment |
| `worldcup-client/README.md` | Production build / serve section |
| `docs/changes/phase-14-spa-prod.md` | This page (dual) |
| Nested + workspace `README.md` | SPA prod pointer |
| Docs (dual) | Roadmap / index / changes-review / planned / VitePress |

Roadmap mapping: [Phase 14](../roadmap.md#phase-14) — `p14-spa-prod` **Done** `[x]`.

---

## Verify

| Check | Pass | Evidence |
| --- | --- | --- |
| Prod `apiUrl` not silent localhost | Yes | Placeholder `REPLACE_ME` |
| Dev still localhost | Yes | `environment.development.ts` + serve `development` default |
| Canonical client path | Yes | `WorldCup-System/worldcup-client` |
| Build output path documented | Yes | `dist/worldcup-client/browser/` |
| CORS alignment noted | Yes | SPA origin ≠ API `apiUrl` |
| Pre-deploy checklist present | Yes | [checklist](#pre-deploy-checklist) |

---

## Related

- [Phase 14 planned Task 5](phase-14-planned.md#task-5--spa-production-build-p14-spa-prod)
- [Phase 14 Compose](phase-14-compose.md)
- [Phase 14 CORS](phase-14-cors.md)
- [Phase 14 secrets](phase-14-secrets.md)
- [Roadmap Phase 14](../roadmap.md#phase-14)
- [Changes Review](../changes-review.md)
