# WorldCup System — Phase 15 Task 3: Migrate + Smoke on Target

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 15 plan](phase-15-planned.md) · [Phase 14 migrate checklist](phase-14-migrate-smoke.md) · [Deploy](phase-15-deploy.md)

## Phase 15 Task 3 — Migrate + smoke on target (`p15-migrate-smoke`)

Opened **3 Aug 2026**. Checklist: [Roadmap Phase 15](../roadmap.md#phase-15) — Task 3 **[x]** Done. Next: Task 4 WC 2026 data hygiene — [phase-15-planned](phase-15-planned.md#task-4--wc-2026-data-hygiene-p15-data-hygiene).

> **Success:** Local ship rehearsal target DB has all **28** EF migrations applied (including MatchSync / multi-cup / Company). Smoke table green: health, Admin login (out-of-band), company create/join, scoped leaderboard, CORS, fixtures/standings. No wipe/import scripts run. Checklist base: [phase-14-migrate-smoke](phase-14-migrate-smoke.md).

<a id="review-gate"></a>

## Review gate

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Migrations not applied / must-include missing | **Critical** | **Cleared** | History has all 28; must-include five present |
| Smoke health / company path skipped | **Critical** | **Cleared** | Rows 1–5 green |
| Wipe/import run against target | **Critical** | **Cleared** | None run |
| Host `ef` falsely claimed applied when DB unreachable | **High** | **Cleared** | Documented: Compose `MigrateAsync` + SQL verify; host publish flaky |
| Dual docs VitePress ↔ git mirror | **High** | **Cleared this Markdown pass** | `docs/` and `WorldCup-System/docs/` |
| Secrets committed | **Critical** | **Cleared** | Rehearsal passwords never written to docs/git |

**Gate verdict:** Task 3 **high/critical** items **Cleared**. Formal Phase 15 close remains Task 5 (`p15-gate`).

---

## Locked approach

| Choice | Decision |
| --- | --- |
| Target | Same local Production Compose stack as Task 2 (`:5055` API, Compose Postgres) |
| Apply path | API startup `Database.MigrateAsync()` (non-`Testing`) — verified via `__EFMigrationsHistory` |
| Host `dotnet ef database update` | Attempted; **Docker Desktop host→published `:5432` unreliable** (auth fail / timeout). Prefer SQL verify or tooling container on `worldcup-system_default` |
| Smoke Admin | Out-of-band: `User/CreateNewUser` + SQL `AspNetUserRoles` Admin promote (no DevAdmin in Production) |
| Wipe | Not run |

---

## Migrate evidence (3 Aug 2026)

| Check | Result |
| --- | --- |
| Containers | `worldcup-system-api-1` + `worldcup-system-db-1` healthy |
| `__EFMigrationsHistory` | **28** rows applied |
| Must-include | `AddMatchExternalMatchId`, `AddMatchExternalStageId`, `AddPlayerExternalPlayerId`, `AllowMultipleTeamsPerCountry`, `AddCompany` — all present |
| Host `ef database update` | Failed to reach published Postgres from Windows host — not used as apply path |
| Wipe scripts | Not executed |

Operator SQL verify (from nested git / with Compose up):

```powershell
# Pipe a .sql file into the DB container (avoids PowerShell quote stripping)
Get-Content .\mig-check.sql | docker exec -i worldcup-system-db-1 psql -U worldcup -d WorldCupDb
```

When host `ef` works (Linux host or working Docker port publish):

```powershell
# Nested git root (siblings Data/ + WorldCup-System/)
dotnet ef database update -p Data -s WorldCup-System --connection "<TARGET ConnectionStrings__DefaultConnection>"
```

---

<a id="smoke-results"></a>

## Smoke results (3 Aug 2026)

| # | Check | Pass | Evidence |
| --- | --- | --- | --- |
| 1 | `GET /health` | **Yes** | HTTP **200** `Healthy` |
| 2 | Login Admin | **Yes** | JWT returned after out-of-band Admin promote |
| 3 | Create company | **Yes** | Company A invite returned (`Id=1`) |
| 4 | Second user joins | **Yes** | `Company/Mine` → company A; user3 → company B |
| 5 | Leaderboard scope | **Yes** | Distinct `CompanyId` 1 vs 2; both `GetLeaderboard` → `[]` (no bets); anonymous → **401** |
| 6 | SPA CORS origin | **Yes** | `Access-Control-Allow-Origin: http://localhost:4200` on GET + OPTIONS |
| 7 | Fixtures / standings | **Yes** | `Match/GetMatches` → `[]` **200**; `Standing/GetGroupStandings/1` → `[]` **200** |

### Smoke notes

| Topic | Detail |
| --- | --- |
| Production accounts | No DevAdmin/DevUser — create via `POST User/CreateNewUser`, promote Admin in DB for rehearsal |
| Leaderboard | Empty lists are valid with no bets; isolation confirmed by separate company memberships + JWT gate |
| Fixtures empty | Fresh rehearsal DB — endpoint loads; tournament seed is separate from migrate |
| Do not wipe | Did not run `import_wc2026_finished_matches.py` or other wipe paths |

---

## Files changed

| Path | Change |
| --- | --- |
| `docs/changes/phase-15-migrate-smoke.md` | **New** — this page (dual) |
| Docs (dual) | Roadmap / index / changes-review / planned / VitePress |

No API/SPA source changes in Task 3 (ops-only).

Roadmap mapping: [Phase 15](../roadmap.md#phase-15) — `p15-migrate-smoke` **Done** `[x]`.

---

## Verify

| Check | Pass when | Evidence |
| --- | --- | --- |
| Migrations current | Must-include + full history | 28 rows; five must-include |
| Smoke table green | Rows 1–7 | [Smoke results](#smoke-results) |
| No wipe | Scripts not run | This page |
| Dual docs | VitePress + mirror | Same content |

---

## Related

- [Phase 15 planned Task 3](phase-15-planned.md#task-3--migrate--smoke-on-target-p15-migrate-smoke)
- [Phase 14 migrate + smoke checklist](phase-14-migrate-smoke.md)
- [Phase 15 Task 2 deploy](phase-15-deploy.md)
- [Phase 14 Compose](phase-14-compose.md)
- [Roadmap Phase 15](../roadmap.md#phase-15)
- [Changes Review](../changes-review.md)
