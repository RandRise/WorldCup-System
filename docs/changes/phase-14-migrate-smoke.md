# WorldCup System — Phase 14 Task 6: Migrate + Smoke Checklist

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 14 plan](phase-14-planned.md) · [SPA prod](phase-14-spa-prod.md) · [Compose](phase-14-compose.md)

## Phase 14 Task 6 — Migrate + smoke checklist (`p14-migrate-smoke`)

Opened **28 Jul 2026**. Checklist: [Roadmap Phase 14](../roadmap.md#phase-14) — Task 6 **[x]** Done. Next: Task 7 gate (`p14-gate`).

> **Success:** Operator checklist for target-DB migrate + post-deploy smoke (health + company isolation path). Wipe warnings present. Docs-only — no migrations applied to production by the agent; no wipe/import scripts run against prod.

<a id="review-gate"></a>

## Review gate

High/critical items that must clear **before** treating Task 6 as closed:

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Checklist omits health or company isolation path | **Critical** | **Cleared** | Smoke table covers `/health` + create/join/leaderboard |
| Wipe/import path presented as safe for prod | **Critical** | **Cleared** | Explicit do-not; prefer upsert ops notes |
| Agent applies migrations to prod without connection confirmation | **High** | **Cleared by design** | Docs only; operator confirms target connection string |
| Missing recent migrations called out (MatchSync / multi-cup / Company) | **High** | **Cleared** | Must-include migration list below |
| Dual docs VitePress ↔ git mirror | **High** | **Cleared** | Markdown review mirrored Task 6 to `WorldCup-System/docs/` |
| Wrong `cd` into API host before `ef` | **High** | **Cleared** | Nested git root only; do not double-`cd` into host project |
| Nested README secrets leave shell in API host | **High** | **Cleared** | `cd ..` after user-secrets before `ef` |
| Second User missing for join/isolation smoke | Medium | **Documented** | Smoke notes — create User out-of-band in Production |
| Wrong env / CORS mistaken for schema failure | Medium | **Documented** | [Failure tips](#failure-tips) |

**Gate verdict:** All Task 6 **high/critical** items **Cleared**. Formal Phase 14 close remains Task 7 (`p14-gate`).

---

## Locked approach

| Choice | Decision |
| --- | --- |
| Scope | **Docs-first** operator checklist — optional script only if user asks |
| Migrate tool | `dotnet ef database update -p Data -s WorldCup-System` against **target** connection |
| Runtime fallback | API also runs `MigrateAsync` (+ IF NOT EXISTS repairs) on startup except `Testing` |
| Smoke | Manual HTTP / SPA checks — health, auth, company create/join, scoped leaderboard, CORS, fixtures |
| Data scripts | Never run wipe importers against production |

---

## Pre-migrate checklist

- [ ] Confirm `ConnectionStrings__DefaultConnection` (or User Secrets / Compose) points at the **intended** target database — not local Dev by accident
- [ ] Backup / snapshot the target DB if it already has data
- [ ] Production secrets set ([phase-14-secrets](phase-14-secrets.md)); Compose uses `PROD_*` if applicable ([phase-14-compose](phase-14-compose.md))
- [ ] SPA `apiUrl` and CORS origins aligned ([phase-14-spa-prod](phase-14-spa-prod.md) · [phase-14-cors](phase-14-cors.md))
- [ ] Do **not** run `import_wc2026_finished_matches.py` (wipe path) or other wipe scripts against this DB

---

## Migrate steps (operator)

Run from the **nested git repo root** — the folder that contains sibling projects `Data/` and `WorldCup-System/` (not the API host subfolder).

From the **workspace** root:

```powershell
cd WorldCup-System
# Connection string must point at TARGET database
dotnet ef database update -p Data -s WorldCup-System
```

If you are **already** in the nested git repo (siblings `Data/` + `WorldCup-System/` visible), do **not** `cd WorldCup-System` again — that enters the API host project and breaks `-p Data`:

```powershell
# Already at nested git root:
dotnet ef database update -p Data -s WorldCup-System
```

| Note | Detail |
| --- | --- |
| Project / startup | `-p Data` · `-s WorldCup-System` (paths relative to nested git root) |
| Env override | Prefer env/`ConnectionStrings__DefaultConnection` so the CLI hits the same DB as the host |
| Docker path | With Compose up, migrate from a host with the EF tools installed, or exec into a tooling container that has the same connection string |
| Startup migrate | Non-`Testing` API start also calls `Database.MigrateAsync()` plus IF NOT EXISTS column/table repairs in `Program.cs` — still prefer an explicit `ef database update` on first deploy so failures are visible before traffic |

### Must-include migrations (already in repo)

Operators should expect these (among earlier Identity/domain migrations) to apply on a fresh or lagging target:

| Migration | Why it matters |
| --- | --- |
| `AddMatchExternalMatchId` | FIFA calendar sync / `ExternalMatchId` |
| `AddMatchExternalStageId` | Timeline URLs / stage id |
| `AddPlayerExternalPlayerId` | Real scorer resolve |
| `AllowMultipleTeamsPerCountry` | Multi-cup (e.g. WC 2030) |
| `AddCompany` | Company-scoped competitions (Phase 13) |

Also ensure historical stages/feeders (`AddMatchStage`, Feeder* columns) are present — startup IF NOT EXISTS covers some local drift; a clean `ef database update` is still the source of truth.

Verify applied:

```powershell
dotnet ef migrations list -p Data -s WorldCup-System
```

---

<a id="smoke-checklist"></a>

## Smoke checklist (manual)

Run after API (and SPA, if in scope) are up against the migrated target.

| # | Check | Pass criteria |
| --- | --- | --- |
| 1 | `GET /health` | 200 |
| 2 | Login Admin | JWT returned |
| 3 | Create company | Invite code returned |
| 4 | Second user joins | `Company/Mine` populated |
| 5 | Leaderboard | Same-company only (no cross-company leak) |
| 6 | SPA origin | Browser calls API without CORS errors |
| 7 | Fixtures / standings | Still load (shared tournament data) |

### Smoke notes

| Check | Tip |
| --- | --- |
| Admin login | Production has **no** DevAdmin seed — create/promote an Admin account out-of-band before smoke |
| Second user | Smoke rows 4–5 need a **second User** account (register or create out-of-band) — DevUser seed is also off in Production |
| Company create | Requires Admin (or documented create role path) |
| Leaderboard | Prefer **two companies** with bets: each caller must see only their company (isolation) |
| CORS | SPA origin must match `Cors__AllowedOrigins` / `PROD_CORS_ALLOWED_ORIGINS` exactly (no trailing slash) |
| Fixtures | Shared World Cup data is not company-scoped — both users should see the same tournament fixtures |

---

<a id="do-not"></a>

## Do not (production / target deploy)

| Action | Why |
| --- | --- |
| Run `scripts/import_wc2026_finished_matches.py` wipe path | Wipes WC 2026 Match/Goal/Bet then reloads — destroys live bets |
| Prefer wipe over upsert for Final-only fixes | Use `scripts/add_sf2_and_final.py` (and Phase 9 ops notes) instead |
| Point `ef database update` at the wrong DB | Schema changes are hard to reverse; confirm connection string first |
| Treat Task 6 docs as permission to migrate prod for the agent | Operator (or user) must confirm the target connection |

Wipe / upsert context: [phase-9-ops-closeout](phase-9-ops-closeout.md).

---

<a id="failure-tips"></a>

## Failure tips

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `ef` fails / relation does not exist | Wrong connection string or migrations not applied | Confirm target DB; re-run `dotnet ef database update` |
| `/health` not 200 | API not up, wrong host/port, container unhealthy | Check Compose logs / healthcheck; [phase-14-compose](phase-14-compose.md) |
| Login 401 / no JWT | Wrong JWT secret/issuer/audience vs token mint | Align `JWT__*` with SPA audience and API issuer — [phase-14-secrets](phase-14-secrets.md) · [phase-14-cors](phase-14-cors.md) |
| Browser CORS errors | SPA origin not on allow-list, or trailing slash / scheme mismatch | Set `Cors__AllowedOrigins` / `PROD_CORS_ALLOWED_ORIGINS` to exact SPA origin |
| Company create fails | Not Admin; or `AddCompany` migration missing | Login as Admin; confirm Company table exists |
| Leaderboard empty | User has no `CompanyId` | Join company first (`Company/Mine`) |
| Leaderboard shows other company | Regression / wrong scope | Fail smoke #5; do not go live — Phase 13 isolation is required |
| Fixtures empty | Tournament data not seeded on this DB | Ops seed/import (non-wipe) — separate from migrate |

---

## Files changed

| Path | Change |
| --- | --- |
| `docs/changes/phase-14-migrate-smoke.md` | This page (dual) |
| Nested + workspace `README.md` | Migrate + smoke pointer; nested README `cd ..` after user-secrets |
| Docs (dual) | Roadmap / index / changes-review / planned / upload-gate / VitePress |

Roadmap mapping: [Phase 14](../roadmap.md#phase-14) — `p14-migrate-smoke` **Done** `[x]`.

---

## Verify

| Check | Pass | Evidence |
| --- | --- | --- |
| Checklist covers health + company path | Yes | Smoke rows 1–5 |
| Wipe warning present | Yes | [Do not](#do-not) + pre-migrate |
| Must-include migrations listed | Yes | MatchSync / multi-cup / Company |
| Dual docs | Yes | VitePress + git mirror |
| No prod migrate by agent | Yes | Docs-only task |

---

## Related

- [Phase 14 planned Task 6](phase-14-planned.md#task-6--migrate--smoke-checklist-p14-migrate-smoke)
- [Phase 14 SPA prod](phase-14-spa-prod.md)
- [Phase 14 Compose](phase-14-compose.md)
- [Phase 14 CORS](phase-14-cors.md)
- [Phase 14 secrets](phase-14-secrets.md)
- [Phase 9 ops close-out](phase-9-ops-closeout.md)
- [Roadmap Phase 14](../roadmap.md#phase-14)
- [Changes Review](../changes-review.md)
