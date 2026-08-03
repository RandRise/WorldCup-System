# WorldCup System — Phase 14 Planned Work

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Upload gate (closed)](whole-project-upload-gate.md)

## Phase 14 — Deploy Readiness

**Status: Done** — Tasks 1–7 **Done** (gate closed **3 Aug 2026**; suite **462**). Product Phases **1–13** are Done. This phase is **ops packaging** so a real host can run the API + SPA safely. Checklist: [Roadmap Phase 14](../roadmap.md#phase-14) · gate detail: [phase-14-gate](phase-14-gate.md).

> **Success criteria for Phase 14 Done:** remote has Phase 13; prod-oriented env / CORS / Compose / SPA build path documented or wired; migrations + smoke checklist runnable on a target DB; dual docs updated; no high/critical review blockers; consented `dotnet test` green when API code changed.

### Why

Local product work is complete. Remaining risk is **shipping localhost defaults** (CORS, JWT audience, `ASPNETCORE_ENVIRONMENT=Development`, SPA `apiUrl`, Docker secret placeholders) and **forgetting migrations** on the deploy DB.

### Agent rules (read before every task)

| Rule | Detail |
| --- | --- |
| One task at a time | Finish Task N checkboxes + dual-docs note before starting N+1 |
| Ask before push | Never `git push` unless the user explicitly asks |
| Ask before commit | Never `git commit` unless the user explicitly asks |
| Ask before `dotnet build` | User rule — do not build without consent |
| Ask before `dotnet test` | Prefer asking; required only at Task 7 gate (or when user says “phase gate”) |
| No secrets in git | Never commit real passwords, JWT secrets, connection strings, or `.env` with values |
| Dual docs | Edit VitePress `docs/` **and** mirror `WorldCup-System/docs/` the same change |
| Git repo root | Commands run from `WorldCup-System/` (nested git), not workspace root |
| Do not reopen Phases 1–13 | No feature work; only deploy readiness |
| Prefer config over hardcode | Prod origins / API URL via env or documented replace — not one-off localhost hacks left as defaults without a prod path |

### Locked decisions (v1)

| Choice | Decision |
| --- | --- |
| Scope | **Deploy readiness**, not cloud vendor lock-in (no Azure/AWS-specific IaC required) |
| Hosting target | Docker Compose–friendly API + Postgres; SPA as static files (or separate static host) |
| Secrets | Env vars / host secret store only |
| DevAdmin seed | **Off** in Production (`ASPNETCORE_ENVIRONMENT=Production`) |
| CORS | Configurable allow-list (env or `appsettings` non-secret origins) — not `*` |
| SPA API URL | Production `environment.ts` (or build-time replace) must not silently keep wrong host |
| Upload gate | Phases 9–12 packaging **closed** (`ac0608e`); Phase 13 on origin (`5fdd1c4`) — Task 1 **Done** |

### Goals (summary)

| # | Task id | Theme | Outcome | Status |
| --- | --- | --- | --- | --- |
| 1 | `p14-push` | Git | Push Phase 13 commit so `origin/master` matches local | **Done** (27 Jul 2026) |
| 2 | `p14-secrets` | Config | Document required prod env vars + safe Compose/env example (placeholders only) | **Done** (27 Jul 2026) |
| 3 | `p14-cors` | API | Prod CORS + JWT issuer/audience configurable for real SPA origin | **Done** (27 Jul 2026) |
| 4 | `p14-compose` | Docker | Production-oriented Compose override (no Development defaults) | **Done** (27 Jul 2026) |
| 5 | `p14-spa-prod` | Client | Production SPA build path + `apiUrl` wiring documented/implemented | **Done** (28 Jul 2026) |
| 6 | `p14-migrate-smoke` | Ops | Target DB migrate steps + smoke checklist (health + company isolation path) | **Done** (28 Jul 2026) |
| 7 | `p14-gate` | Gate | Dual-docs close + review; `dotnet test` with user consent | **Done** (3 Aug 2026; suite **462**) — [phase-14-gate](phase-14-gate.md) |

### Execution order (strict)

```text
Task 1 push  →  Task 2 secrets docs  →  Task 3 CORS/JWT  →  Task 4 Compose
    →  Task 5 SPA prod  →  Task 6 migrate/smoke checklist  →  Task 7 gate
```

Task 1 **cleared** — do not re-push unless a new commit is ahead again.

---

### Task 1 — Push Phase 13 (`p14-push`) — **Done**

**Goal:** Close the remaining upload gap. **Completed 27 Jul 2026:** `git push origin master` → `ce8502c..5fdd1c4`.

> **Success:** `origin/master` at `5fdd1c4` — *Ship Phase 13 company-scoped competitions and close the phase gate.* Phase 14 docs were uncommitted locally during push (left unstaged on purpose).

#### Verify (recorded)

| Check | Pass |
| --- | --- |
| `git status -sb` | Not ahead of origin (Phase 14 docs may still be dirty locally) |
| Remote has Phase 13 | `git log origin/master -1` → `5fdd1c4` |

---

### Task 2 — Prod secrets inventory (`p14-secrets`) — **Done**

**Goal:** One canonical list of required Production env vars with **placeholders only**, plus README/Compose pointer. No real secrets.

> **Success:** Detail page [phase-14-secrets](phase-14-secrets.md); README Configuration section links it; `.env.example` clarified (local Dev + commented Production placeholders). Docs/example only — no runtime changes. Completed **27 Jul 2026**.

#### Delivered

| Path | Action |
| --- | --- |
| `docs/changes/phase-14-secrets.md` (+ git mirror) | Canonical inventory |
| Workspace `README.md` | Link prod env table |
| `WorldCup-System/.env.example` | Local Dev defaults + commented Production placeholders |

#### Required keys (minimum)

| Env var | Purpose | Prod note |
| --- | --- | --- |
| `ConnectionStrings__DefaultConnection` | Postgres | Real host/db/user/password |
| `JWT__Secret` | Token signing | ≥32 chars; unique per env |
| `JWT__ValidIssuer` | JWT issuer | Match API public URL |
| `JWT__ValidAudience` | JWT audience | Match SPA origin / audience string |
| `ASPNETCORE_ENVIRONMENT` | Hosting | `Production` |
| CORS origins (Task 3) | Browser allow-list | Real SPA origin(s) — **Done** ([phase-14-cors](phase-14-cors.md)) |

Optional (dev-only — **omit in Production**): `DevAdmin__*`, `DevUser__*`.

#### Verify (recorded)

| Check | Pass |
| --- | --- |
| Table lists all keys API needs to boot | Yes |
| No secret-looking values in diff | Yes (placeholders / local-dev defaults only) |

---

### Task 3 — CORS + JWT for production (`p14-cors`) — **Done**

**Goal:** Production SPA origin works without editing code to hardcode one host forever. Prefer env/`appsettings` configuration. **Completed 27 Jul 2026.** Detail: [phase-14-cors](phase-14-cors.md).

#### Current baseline (as of Phase 14 plan)

- ~~`Program.cs` policy `AngularDev` → `WithOrigins("http://localhost:4200")` only~~ → policy **`Spa`** via `CorsOriginsResolver`
- JWT issuer/audience defaults in appsettings / Compose point at localhost (overridable via `JWT__ValidIssuer` / `JWT__ValidAudience`)

#### Delivered

| Path | Action |
| --- | --- |
| `WorldCup-System/WorldCup-System/Configuration/CorsOriginsResolver.cs` | Resolve array or semicolon-separated origins; reject `*` |
| `WorldCup-System/WorldCup-System/Program.cs` | Config-driven `Spa` policy; `UseCors` before auth |
| `WorldCup-System/WorldCup-System/appsettings.json` | `Cors:AllowedOrigins` default `[http://localhost:4200]` |
| `WorldCup-System/docker-compose.yml` | `Cors__AllowedOrigins` from `CORS_ALLOWED_ORIGINS` |
| `WorldCup-System/.env.example` | Local + Production CORS placeholders |
| Detail page `phase-14-cors.md` | Operator + verify notes |

#### Verify (recorded)

| Check | Pass |
| --- | --- |
| Dev still works with localhost:4200 | Default preserved |
| Prod origin settable via env | `Cors__AllowedOrigins` / indexed keys |

#### Common mistakes

| Mistake | Avoid by |
| --- | --- |
| Changing policy name and forgetting `UseCors("…")` | Both use `CorsOriginsResolver.PolicyName` |
| Trailing-slash origin mismatch | Resolver strips `/`; document exact origin form (`https://app.example.com` no slash) |

---

### Task 4 — Production Compose (`p14-compose`) — **Done**

**Goal:** A Compose path that does **not** force `ASPNETCORE_ENVIRONMENT: Development` or weak default JWT for real deploys. **Completed 27 Jul 2026.** Detail: [phase-14-compose](phase-14-compose.md).

#### Locked approach

**Option A** — `docker-compose.prod.yml` override. Local Dev keeps `docker compose up`. Prod:

```text
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

#### Delivered

| Path | Action |
| --- | --- |
| `WorldCup-System/docker-compose.prod.yml` | Production override; `${PROD_*:?…}` required secrets; clear DevAdmin/DevUser |
| `.env.example` + README(s) | Run command + required-var notes |
| Detail page `phase-14-compose.md` | Operator + verify notes |

#### Verify (recorded)

| Check | Pass |
| --- | --- |
| Local `docker-compose.yml` still usable for Dev | Base unchanged |
| Prod override sets Production | Yes |
| Healthcheck still valid | Inherited `/health` |

---

### Task 5 — SPA production build (`p14-spa-prod`) — **Done**

**Goal:** Production Angular build uses the correct API base URL. **Completed 28 Jul 2026.** Detail: [phase-14-spa-prod](phase-14-spa-prod.md).

#### Delivered

| Path | Action |
| --- | --- |
| `worldcup-client/src/environments/environment.ts` | `apiUrl` → `https://api.REPLACE_ME.example` + deploy comment |
| `worldcup-client/README.md` | Production build / serve section |
| Detail page `phase-14-spa-prod.md` | Operator steps, dist path, CORS alignment, Review gate, pre-deploy checklist |
| README(s) | SPA prod pointer |

#### Locked approach (recorded)

Documented replace of `apiUrl` before `npm run build` (or CI string replace). Dev file untouched; `ng serve` still uses localhost via `fileReplacements`.

#### Verify (recorded)

| Check | Pass |
| --- | --- |
| Prod `apiUrl` is not an accidental silent localhost trap | Placeholder `REPLACE_ME` |
| Canonical client path | `WorldCup-System/worldcup-client` only |
| Dist serve path | `dist/worldcup-client/browser/` |

---

### Task 6 — Migrate + smoke checklist (`p14-migrate-smoke`) — **Done**

**Goal:** Operator checklist for target DB + post-deploy smoke. Docs-first; optional script only if user asks. **Completed 28 Jul 2026.** Detail: [phase-14-migrate-smoke](phase-14-migrate-smoke.md).

#### Delivered

| Path | Action |
| --- | --- |
| `docs/changes/phase-14-migrate-smoke.md` (+ git mirror) | Migrate steps, must-include migrations, smoke table, wipe warnings, failure tips |
| README(s) | Migrate + smoke pointer |
| Roadmap / index / changes-review / VitePress | Task 6 Done; next = Task 7 gate |

#### Migrate steps (operator)

From the **workspace** root (`cd WorldCup-System` into the nested git repo). If already at nested git root (siblings `Data/` + `WorldCup-System/`), skip the `cd` — a second `cd WorldCup-System` enters the API host and breaks `-p Data`.

```powershell
cd WorldCup-System
# Connection string must point at TARGET database
dotnet ef database update -p Data -s WorldCup-System
```

Must include (already in repo): MatchSync migrations, multi-cup team, **AddCompany**, etc.

#### Smoke checklist (manual)

| # | Check | Pass criteria |
| --- | --- | --- |
| 1 | `GET /health` | 200 |
| 2 | Login Admin | JWT returned |
| 3 | Create company | Invite code returned |
| 4 | Second user joins | `Company/Mine` populated |
| 5 | Leaderboard | Same-company only (no cross leak) |
| 6 | SPA origin | Browser calls API without CORS errors |
| 7 | Fixtures / standings | Still load (shared tournament data) |

#### Agent do not (recorded)

- Run wipe/import scripts against prod (`import_wc2026_finished_matches` wipe path — prefer upsert ops notes from Phase 9)
- Apply migrations to production without user confirmation of connection string

#### Verify (recorded)

| Check | Pass |
| --- | --- |
| Checklist covers health + company path | Yes |
| Wipe warning present | Yes |

---

### Task 7 — Gate (`p14-gate`) — **Done**

**Goal:** Formal close of Phase 14. **Closed 3 Aug 2026.** Detail: [phase-14-gate](phase-14-gate.md).

> **Success:** Bugbot high fixed; dual docs mirrored; secrets placeholders only; consented `dotnet test` → suite **462** passed. Phase 14 **[Done]**.

#### Gate table

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Task 1 push complete | Critical | **Cleared** | `origin/master` at `5fdd1c4` |
| Tasks 2–6 deliverables present | High | **Cleared** | Detail pages / code as scoped |
| No secrets in git diff | Critical | **Cleared** | Placeholders / Dev defaults only |
| Dual docs mirrored | High | **Cleared** | VitePress `docs/` + `WorldCup-System/docs/` |
| High/critical Bugbot (code in 3–5 + Compose/SPA) | High | **Cleared** | Healthcheck uses container `$$POSTGRES_*` |
| `dotnet test` (API code changed — CORS) | High | **Cleared** | Consent **3 Aug 2026**; suite **462** |
| Roadmap / index / changes-review mark Done | High | **Cleared** | Phase 14 **[Done]** |

#### Agent do

1. ~~Run phase-completion-review~~ — completed.
2. ~~Keep [phase-14-gate](phase-14-gate.md) findings table updated~~ — closed.
3. ~~Mark Phase 14 **[Done]**~~ — done.
4. ~~Update dashboard “Recommended next work”~~ — ship/ops + maintenance.

---

### Non-goals (do not do in Phase 14)

- Kubernetes / Terraform / cloud marketplace packaging
- CI/CD deploy pipelines to a specific cloud (CI already builds/tests)
- Live FIFA sync schedule hardening (already Phase 10–11)
- New product features (extra company branding, billing, etc.)
- Re-opening whole-project upload packaging for Phases 9–12

### Related

- [Roadmap Phase 14](../roadmap.md#phase-14)
- [phase-14-gate](phase-14-gate.md) (Task 7 — Done)
- [Whole-project upload gate](whole-project-upload-gate.md)
- [Phase 13 tests gate](phase-13-tests-gate.md)
- [Phase 9 ops close-out](phase-9-ops-closeout.md) (wipe warnings)
- [Changes Review](../changes-review.md)
