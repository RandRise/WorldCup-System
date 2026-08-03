# WorldCup System — Changes Review

## Docs navigation

[Dashboard](index.md) | [System Overview](overview.md) | [Completion Roadmap](roadmap.md) | [Dev Workflow](workflow.md) | [API Status](api-status.md) | [Data Model](data-model.md) | [Changes Review](changes-review.md)

---
## Changes Review

Latest: **Phase 14 Done (3 Aug 2026)** — gate closed; Bugbot high fixed; suite **462**. Detail: [phase-14-gate](changes/phase-14-gate.md). Prior tasks: [phase-14-migrate-smoke](changes/phase-14-migrate-smoke.md) · [phase-14-planned](changes/phase-14-planned.md). Phase 13 Done (suite **438**) — [phase-13-tests-gate](changes/phase-13-tests-gate.md).

> **Success:** **Phase 14 Done — Deploy Readiness.** Tasks 1–7 complete; consented suite **462**; prod secrets / CORS / Compose / SPA / migrate-smoke packaged. Checklist: [Phase 14](roadmap.md#phase-14) · detail: [phase-14-gate](changes/phase-14-gate.md).

> **Success:** **Phase 14 Task 6 Done.** Operator migrate (`ef database update`) + smoke table (health, company create/join, scoped leaderboard, CORS, fixtures); wipe warnings; docs-only. Checklist: [Phase 14](roadmap.md#phase-14) · detail: [phase-14-migrate-smoke](changes/phase-14-migrate-smoke.md).

> **Success:** **Phase 14 Task 5 Done.** Production `environment.ts` uses `https://api.REPLACE_ME.example` (no silent localhost); build → `dist/worldcup-client/browser/`; Dev unchanged. Checklist: [Phase 14](roadmap.md#phase-14) · detail: [phase-14-spa-prod](changes/phase-14-spa-prod.md).

> **Success:** **Phase 14 Task 4 Done.** `docker-compose.prod.yml` sets `Production`, requires `PROD_*` JWT/DB/CORS env (`${VAR:?…}`), clears DevAdmin/DevUser; local Dev Compose unchanged. Checklist: [Phase 14](roadmap.md#phase-14) · detail: [phase-14-compose](changes/phase-14-compose.md).

> **Success:** **Phase 14 Task 3 Done.** `CorsOriginsResolver` + `Cors__AllowedOrigins`; policy `Spa`; no `*`; Dev localhost default kept; JWT issuer/audience already env-driven (documented). Checklist: [Phase 14](roadmap.md#phase-14) · detail: [phase-14-cors](changes/phase-14-cors.md).

> **Success:** **Phase 14 Task 2 Done.** Canonical Production env inventory + README / `.env.example` pointers; no real secrets in git. Checklist: [Phase 14](roadmap.md#phase-14) · detail: [phase-14-secrets](changes/phase-14-secrets.md).

> **Success:** **Phase 14 Task 1 Done.** Pushed Phase 13 to origin (`ce8502c..5fdd1c4`). Checklist: [Phase 14](roadmap.md#phase-14) · detail: [phase-14-planned](changes/phase-14-planned.md).

> **Success:** **Phase 13 Done — Task 6 gate closed.** Isolation + CompanyController tests; suite **438** passed. Bugbot clean after Join JWT session fix (`canManageCompany`). Checklist: [Phase 13](roadmap.md#phase-13) · detail: [phase-13-tests-gate](changes/phase-13-tests-gate.md).

> **Success:** **Phases 9–12 upload packaging closed locally.** Commit `ac0608e` shipped MatchSync + migrations + WC 2030 scripts; Phase 13 is `5fdd1c4` (push = Phase 14 Task 1). Historical checklist: [whole-project-upload-gate](changes/whole-project-upload-gate.md).

> **Success:** **Phase 13 Task 5 interim gate closed.** `/company` join + JWT-gated CompanyAdmin UI; leaderboard auth + join CTA; Admin create. Checklist: [Phase 13](roadmap.md#phase-13) · detail: [phase-13-spa](changes/phase-13-spa.md).

> **Success:** **Phase 13 Task 3 interim gate closed.** Bugbot highs fixed (role-before-join; Join token fail-soft + flat shape; orphan Admin strip); Markdown dual-docs updated; **15** CompanyService tests; suite **414** passed. Checklist: [Phase 13](roadmap.md#phase-13) · detail: [phase-13-company-api](changes/phase-13-company-api.md).

> **Success:** **Phase 13 Task 4 interim gate closed.** Bugbot found no bugs; JWT + `User.CompanyId` scope on `GetLeaderboard`; null company → empty; **14** Task 4 cases; suite **399** passed (pre–Task 3 tests). Checklist: [Phase 13](roadmap.md#phase-13) · detail: [phase-13-leaderboard-scope](changes/phase-13-leaderboard-scope.md).

> **Success:** **Phase 13 Task 2 interim gate closed.** Bugbot found no bugs; Markdown dual-docs updated; **4** Company repo tests; suite **391** passed. Checklist: [Phase 13](roadmap.md#phase-13) · detail: [phase-13-company-model](changes/phase-13-company-model.md).

> **Success:** **Phase 13 Done.** Soft multi-tenancy — company model/API, scoped leaderboard, SPA join UX; suite **438**. Checklist: [Phase 13](roadmap.md#phase-13) · [tests gate](changes/phase-13-tests-gate.md) · [SPA](changes/phase-13-spa.md).

> **Info:** **Whole-project upload gate (historical).** Product packaging for Phases 9–12 **closed** in git (`ac0608e`). Phase 13 push (**Task 1**) and secrets inventory (**Task 2**) **Done**. Checklist: [whole-project-upload-gate](changes/whole-project-upload-gate.md) · [phase-14-secrets](changes/phase-14-secrets.md).

> **Success:** **Phase 9 Done — Final FT recorded.** Argentina **0–1** Spain a.e.t. (Ferran Torres 106', MetLife); MatchId **116**; bets resolved **2/2**; GoalId **298** corrected Morata → Torres. Scripts: idempotent `apply_final_ft` in `add_sf2_and_final.py`; Final row + `FINAL=5` in `import_wc2026_finished_matches.py` (wipe path — prefer upsert script). No C# / migration. Commits Phase 7–8: `dcb322b` + `7886b40`. ThirdPlace skipped. Checklist: [Phase 9](roadmap.md#phase-9) · detail: [phase-9-ops-closeout](changes/phase-9-ops-closeout.md).

> **Success:** **Phase 11 Done — Task 8 gate closed.** Bugbot highs fixed (`SyncFinishedResults` cancel rethrow; `GetSquadScorers` name-only); **+2** regressions; suite **382** passed. Checklist: [Phase 11](roadmap.md#phase-11) · detail: [phase-11-tests-gate](changes/phase-11-tests-gate.md).

> **Success:** **Phase 12 Done — Task 4 gate closed.** Unittest **23** OK; smoke **103**; Bugbot highs fixed (`EnsureSameWorldCup`; `demo_kickoff_anchors`); medium H2H accepted. Checklist: [Phase 12](roadmap.md#phase-12) · detail: [phase-12-tests-gate](changes/phase-12-tests-gate.md).

> **Success:** **Phase 11 Task 7 gate closed.** `MatchService.ToHonestPlayerName` nulls `"Tournament Scorer"` on `GetLiveSnapshot` + match-detail goal/card DTOs; client `placeholder-scorer.ts` + fixtures Recent events + admin live timeline sanitize (name-only; not jersey 99 alone). Bugbot found no bugs; honesty **3** + suite **380** passed. Checklist: [Phase 11](roadmap.md#phase-11) · detail: [phase-11-recent-events-honesty](changes/phase-11-recent-events-honesty.md).

> **Success:** **Phase 12 Task 3 implemented.** Best thirds + FIFA Art. 12 R32→Final (31 matches, no ThirdPlace); feeder FKs; wipe walks TBD rows; unittest **22**; smoke seed 2030 → **103** matches. Formal close: [Task 4](changes/phase-12-tests-gate.md). Checklist: [Phase 12](roadmap.md#phase-12) · detail: [phase-12-bracket](changes/phase-12-bracket.md).

> **Success:** **Phase 11 Task 6 gate closed.** Admin `POST Match/SyncScorers/{matchId}` + `POST Match/SyncScorersForWorldCup?worldCupId=`; client `syncScorers` / `syncScorersForWorldCup`; optional `scripts/sync_scorers_backfill.ps1`. Calendar finished+orient only — no FT rewrite, bets, or knockout; empty timeline → `Skipped`; batch hard fail → `Warning`; local Goal counts in DTO. Bugbot highs fixed (cancel rethrow; local scores); backfill **14** + suite **375** passed. Checklist: [Phase 11](roadmap.md#phase-11) · detail: [phase-11-backfill](changes/phase-11-backfill.md).

> **Success:** **Phase 11 Task 5 gate closed.** `SyncResult` / `SyncFinishedResults` call timeline + scorer apply after calendar FT (fail-soft); `ScorerStatus` / `ScorerMessage` / `ScorerGoalsUpdated` on DTO; batch scorer counts + `BatchDelayMilliseconds`; client models updated. Bugbot found no bugs; wiring **8** + suite **361** passed. Checklist: [Phase 11](roadmap.md#phase-11) · detail: [phase-11-sync-wire](changes/phase-11-sync-wire.md).

> **Success:** **Phase 12 Task 2 gate closed.** After draw: 72 group matches, Poisson λ≈1.35 (cap 7), Goal + TeamStats, Tournament Scorer placeholders; `--wipe-matches` 2030-only; unittest **14** OK; smoke seed 2030 → 72 matches / 193 goals; WC 2026 untouched. Bugbot highs fixed (2026 importer WC-scoped teams/wipe; Forward position by name). No C# in Task 2. Checklist: [Phase 12](roadmap.md#phase-12) · detail: [phase-12-group-sim](changes/phase-12-group-sim.md).

> **Success:** **Phase 12 Task 1 Done (closed via Task 4).** `simulate_wc2030.py` ensures WC 2030, 6 hosts + 42 random, groups A–L; multi-cup via non-unique `Team.CountryId`; draw helpers in unittest suite (**23** total with group/bracket/anchors). Checklist: [Phase 12](roadmap.md#phase-12) · detail: [phase-12-draw](changes/phase-12-draw.md).

> **Success:** **Phase 11 Task 4 gate closed.** `ITimelineScorerApplyService` / `TimelineScorerApplyService`; in-place Goal `PlayerId` + `TimeScored` + `IsOwnGoal` when per-side counts align; count mismatch throws (goals unchanged); own goal → conceding team; timeline Event order preserved (same-minute); no bet/knockout; DI in `Program.cs`. Bugbot high fixed; apply **10** + suite **353** passed. Wired by Task 5. Checklist: [Phase 11](roadmap.md#phase-11) · detail: [phase-11-scorer-apply](changes/phase-11-scorer-apply.md).

> **Success:** **Phase 11 Task 3 gate closed.** `ITimelinePlayerResolver` / `TimelinePlayerResolver`; `Player.ExternalPlayerId`; exact → normalized → create; Bugbot highs fixed (jersey-99 filter); resolver **32** + suite **342** passed. Wired by Task 4 apply / Task 5 SyncResult. Checklist: [Phase 11](roadmap.md#phase-11) · detail: [phase-11-player-resolve](changes/phase-11-player-resolve.md).

> **Info:** **Phase 12 Done.** Tasks 1–4 complete; gate closed. Plan: [phase-12-planned](changes/phase-12-planned.md) · [tests gate](changes/phase-12-tests-gate.md).

> **Success:** **Phase 11 Task 2 gate closed.** `IExternalMatchEventsProvider` + `FifaTimelineEventsProvider`; `TimelineBaseUrl`; Goal!/OG/Penalty parse; skip unchanged-score Goal! lines; Mexico–SA fixture. Wired by Task 5. Bugbot highs fixed; timeline **32** + suite **310** passed. Checklist: [Phase 11](roadmap.md#phase-11) · detail: [phase-11-timeline-provider](changes/phase-11-timeline-provider.md).

> **Success:** **Phase 11 Task 1 gate closed.** `Match.ExternalStageId` column, migration `AddMatchExternalStageId`, SetExternal optional stage, calendar `IdStage` parse + SyncResult auto-persist, DTOs + client. No high/critical Bugbot findings; medium batch DTO refresh fixed; `dotnet test` **278** passed. Checklist: [Phase 11](roadmap.md#phase-11) · detail: [phase-11-external-stage](changes/phase-11-external-stage.md).

> **Info:** **Phase 11 Done.** Tasks 1–8 complete; gate closed (suite **382**). Plan: [phase-11-planned](changes/phase-11-planned.md) · [tests gate](changes/phase-11-tests-gate.md).

> **Success:** **Phase 10 Task 4 complete.** Removed stale root `worldcup-client` and orphan DB dump; empty cancelled admin dirs; README canonical SPA path; dual docs retained (VitePress + git mirror). Checklist: [Phase 10](roadmap.md#phase-10) · detail: [phase-10-cleanup](changes/phase-10-cleanup.md).

> **Success:** **Phase 10 Task 3 gate closed.** WC styling shipped; no high/critical Bugbot findings; medium fixes applied; Jasmine **25** + `dotnet test` **268** passed. Checklist: [Phase 10](roadmap.md#phase-10) · detail: [phase-10-styling](changes/phase-10-styling.md).

> **Success:** **Phase 10 Task 2 gate closed.** Fixtures UX shipped; no high/critical Bugbot findings; Jasmine **12** + `dotnet test` **268** passed. Checklist: [Phase 10](roadmap.md#phase-10) · detail: [phase-10-fixtures-ux](changes/phase-10-fixtures-ux.md).

> **Success:** **Phase 10 Task 1 gate closed.** Sync API + RabbitMQ decision done; Bugbot highs fixed; **268** tests green. Detail: [phase-10-match-sync](changes/phase-10-match-sync.md).

> **Success:** **Phase 9 Done.** Ops close-out complete — Final FT + script tooling + Phase 7–8 commits. Checklist: [Phase 9](roadmap.md#phase-9) · detail: [phase-9-ops-closeout](changes/phase-9-ops-closeout.md).

> **Success:** **Phase 8 implemented (13–16 Jul 2026).** User dashboard, login return URLs, bet resolve hardening (3/0 + bulk), live snapshot API, admin hub / live console, UpdateTeam wiring. Checklist: [Phase 8](roadmap.md#phase-8).

> **Success:** **Phase 7 knockout + live data.** `MatchStage` / feeders, `KnockoutService`, bracket SPA, H2H standings, offline WC 2026 scripts. External football API cancelled for live — Phase 10 adds **post-match** FIFA calendar sync only.

### Summary stats

- **Phase 14 Done (gate closed 3 Aug 2026):** Bugbot high fixed; suite **462** — [phase-14-gate](changes/phase-14-gate.md)
- **Phase 14 Task 7 (Done):** dual-docs + Bugbot + consented tests — [phase-14-gate](changes/phase-14-gate.md)
- **Phase 14 working tree (Tasks 2–7 still uncommitted):** `CorsOriginsResolver.cs` + CORS tests; `Program.cs` / `appsettings.json`; `docker-compose.prod.yml` + healthcheck fix; SPA `environment.ts` + README; `.env.example`; `docs/changes/phase-14-*.md`
- **Phase 14 Task 6 (Done):** migrate + smoke checklist — [phase-14-migrate-smoke](changes/phase-14-migrate-smoke.md)
- **Phase 14 Task 6 git:** **New** `docs/changes/phase-14-migrate-smoke.md`, nested `README.md` (was untracked); dual docs (roadmap/index/changes-review/planned/upload-gate/VitePress) — docs-only, no C#; **Outside git** workspace `README.md`
- **Phase 14 Task 5 (Done):** SPA production build / `apiUrl` — [phase-14-spa-prod](changes/phase-14-spa-prod.md)
- **Phase 14 Task 5 git:** **New** `docs/changes/phase-14-spa-prod.md`; **Modified** `worldcup-client/src/environments/environment.ts`, `worldcup-client/README.md`, README(s); dual docs (roadmap/index/changes-review/planned/VitePress)
- **Phase 14 Task 4 (Done):** Production Compose override — [phase-14-compose](changes/phase-14-compose.md)
- **Phase 14 Task 4 git:** **New** `docker-compose.prod.yml`, `docs/changes/phase-14-compose.md`; **Modified** `.env.example`, README(s); dual docs (roadmap/index/changes-review/planned/secrets/VitePress)
- **Phase 14 Task 3 (Done):** config-driven CORS allow-list + JWT issuer/audience documented — [phase-14-cors](changes/phase-14-cors.md)
- **Phase 14 Task 3 git:** **New** `WorldCup-System/Configuration/CorsOriginsResolver.cs`, `WorldCup-System.Tests/Configuration/CorsOriginsResolverTests.cs`, `WorldCup-System.Tests/Integration/CorsIntegrationTests.cs`, `docs/changes/phase-14-cors.md`; **Modified** `Program.cs`, `appsettings.json`, `docker-compose.yml`, `.env.example`; dual docs (roadmap/index/changes-review/planned/secrets/VitePress)
- **Phase 14 Task 2 (Done):** prod secrets inventory — placeholders only; README + `.env.example` — [phase-14-secrets](changes/phase-14-secrets.md)
- **Phase 14 Task 2 git:** **New** `docs/changes/phase-14-secrets.md` (+ `phase-14-planned.md` if untracked); **Modified** `.env.example`, roadmap/index/changes-review/upload-gate/workflow/VitePress; **Outside git** workspace `README.md`
- **Phase 14 (Done):** Deploy readiness — Tasks 1–7 Done — [phase-14-gate](changes/phase-14-gate.md) · [phase-14-planned](changes/phase-14-planned.md)
- **Upload inventory (historical):** Phases 9–12 packaging committed (`ac0608e`); Phase 13 on origin (`5fdd1c4`); Phase 14 Tasks 2–7 still local/uncommitted — [whole-project-upload-gate](changes/whole-project-upload-gate.md)
- **Phase 13 Task 6 (tests cleared):** isolation integration **3** + unit **2**; suite **419**; Bugbot still open — [phase-13-tests-gate](changes/phase-13-tests-gate.md)
- **Phase 13 Task 6 git:** **New** `LeaderboardIsolationIntegrationTests.cs`; **Modified** `LeaderboardServiceTests.cs`, `CompanyServiceTests.cs`; docs `phase-13-tests-gate.md` + roadmap/planned/index/changes-review
- **Phase 13 Task 5 (implemented; interim gate pending):** SPA `/company` join + CompanyAdmin; leaderboard auth + join CTA; Admin create — [phase-13-spa](changes/phase-13-spa.md)
- **Phase 13 Task 5 git (SPA slice):** **New** `company-api.service.ts` (+spec), `company.component.{ts,html,scss}`, `auth.service.company.spec.ts`, `phase-13-spa.md`; **Modified** `api.models.ts`, `auth.service.ts`, `app.routes.ts`, shell, dashboard, leaderboard, admin hub — client **+201/−60** (11 modified) + untracked company feature
- **Phase 13 Task 3 (interim gate closed):** Bugbot highs fixed; **+15** CompanyService tests; suite **414**; full phase gate = Task 6 — [phase-13-company-api](changes/phase-13-company-api.md)
- **Phase 13 Task 3 git (repo working tree):** **New** `CompanyDTO.cs`, `ICompanyService.cs`, `CompanyService.cs`, `CompanyController.cs`, `CompanyServiceTests.cs`, `phase-13-company-api.md`; **Modified** `Program.cs`, `ApiErrorHelper.cs`, roadmap/index/api-status/planned/changes-review/VitePress
- **Phase 13 Task 4 (interim gate closed):** JWT + company-scoped `GetLeaderboard`; null → empty; suite **399** (pre–Task 3) — [phase-13-leaderboard-scope](changes/phase-13-leaderboard-scope.md)
- **Phase 13 Task 4 git (Task 4 slice):** Modified `ILeaderboardService`, `LeaderboardService`, `BetController.GetLeaderboard`, `LeaderboardServiceTests`, `BetControllerTests`
- **Phase 13 Task 2 (interim gate closed):** Bugbot clean; **+4** Company smoke; suite **391**; full phase gate = Task 6 — [phase-13-company-model](changes/phase-13-company-model.md)
- **Phase 13 Task 2 git (repo working tree):** **New** `Company.cs`, `20260723120000_AddCompany.cs`, `phase-13-company-model.md`; **Modified** `User.cs`, `ApplicationDbContext.cs`, snapshot, `IRepositoryManager` / `RepositoryManager`, `Program.cs`, roadmap/index/data-model/planned/VitePress — broader upload tree still open
- **Phase 9 (Done):** Final FT Argentina **0–1** Spain a.e.t. (Ferran Torres 106'); MatchId **116**; bets **2/2**; `add_sf2_and_final.py` `apply_final_ft` (+466/−96 across 2 scripts); no C#; commits `dcb322b` + `7886b40`; ThirdPlace skipped — [phase-9-ops-closeout](changes/phase-9-ops-closeout.md)
- **Phase 11 Task 8 (gate closed):** formal phase close; Bugbot highs fixed (cancel rethrow + jersey-99 name-only); **+2** regressions; suite **382** — [phase-11-tests-gate](changes/phase-11-tests-gate.md)
- **Phase 11 Task 7 (gate closed):** omit `"Tournament Scorer"` on snapshot + match-detail DTOs (`ToHonestPlayerName`); client `placeholder-scorer.ts`; fixtures + admin live sanitize; name-only; Bugbot clean; honesty **3**; suite **380** — [phase-11-recent-events-honesty](changes/phase-11-recent-events-honesty.md)
- **Phase 12 Task 4 (gate closed):** unittest **23**; smoke **103**; Bugbot highs fixed (cross-cup + demo kickoffs); medium H2H accepted — [phase-12-tests-gate](changes/phase-12-tests-gate.md)
- **Phase 12 Task 3 (impl):** best thirds + R32→Final (31 KO); FIFA Art. 12 skeleton; unittest **22**; smoke **103** — [phase-12-bracket](changes/phase-12-bracket.md)
- **Phase 11 Task 6 (gate closed):** `SyncScorers` / `SyncScorersForWorldCup`; no FT/bets/knockout; empty timeline → `Skipped`; batch hard fail → `Warning`; client + `scripts/sync_scorers_backfill.ps1`; Bugbot highs fixed; backfill **14**; suite **375** — [phase-11-backfill](changes/phase-11-backfill.md)
- **Phase 11 Task 5 (gate closed):** SyncResult / SyncFinishedResults timeline + scorer apply (fail-soft); `ScorerStatus` DTO; batch delay; Bugbot clean; wiring **8**; suite **361** — [phase-11-sync-wire](changes/phase-11-sync-wire.md)
- **Phase 12 Task 2 (gate closed):** 72 group matches + Poisson scores + Goal/TeamStats; `--wipe-matches` 2030-only; Bugbot highs fixed (2026 importer scope); unittest (now **22** total); smoke 72/193 — [phase-12-group-sim](changes/phase-12-group-sim.md)
- **Phase 12 Task 1 (Done; gate closed via Task 4):** cup + hosts/random draw + multi-cup `Team.CountryId` — [phase-12-draw](changes/phase-12-draw.md)
- **Phase 11 Task 4 (gate closed):** `TimelineScorerApplyService` + `ITimelineScorerApplyService` + `TimelineScorerApplyResult`; in-place Goal rewrite; count mismatch throws; same-minute order fix; DI; **10** apply tests; suite **353** — [phase-11-scorer-apply](changes/phase-11-scorer-apply.md)
- **Phase 11 Task 3 (gate closed):** `TimelinePlayerResolver` + `ExternalPlayerId`; 32 resolver tests; suite **342** — [phase-11-player-resolve](changes/phase-11-player-resolve.md)
- **Phase 11 Task 2 (gate closed):** `FifaTimelineEventsProvider` + `IExternalMatchEventsProvider`; DTOs; `TimelineBaseUrl`; 32 provider tests; suite **310** — [phase-11-timeline-provider](changes/phase-11-timeline-provider.md)
- **Phase 11 Task 1 (gate closed):** `ExternalStageId` varchar(64) nullable; migration `20260720120000_AddMatchExternalStageId`; Program.cs IF NOT EXISTS; SetExternal optional stage; calendar `IdStage`; SyncResult auto-persist; MatchDTO + `api.models.ts` — [phase-11-external-stage](changes/phase-11-external-stage.md)
- **Phase 11 checklist:** Tasks 1–8 complete (suite **382**)
- **Phase 10 Task 4 (done):** deleted workspace-root `worldcup-client` + `TestDatabase_backup_*.dump` (**outside git**); empty `admin/seed` + `admin/reference`; README + dual docs policy; docs/VitePress updates **in git**
- **Phase 10 Task 3 (gate closed):** black/gold theme, night-pitch atmosphere, Bebas Neue + Manrope, brand-first home; medium Bugbot fixes (pending pill, amber warnings, reduced-motion, admin finished)
- **Phase 10 Task 2 (gate closed):** `fixture-sections.ts` + 12 Jasmine specs, fixtures component — Action default, chips, sections, jump CTA
- **Phase 10 Task 1 committed** (`dcb322b`): MatchSync services, `ExternalMatchId` migration, Admin sync endpoints, client API, tests
- **Phase 10 checklist:** Tasks 1–4 complete
- **Tests:** Jasmine **25** / 25; API `dotnet test` **382** / 382 (Phase 11 Done)

### Phase 9 Roadmap Mapping

| Task | Status | Evidence |
| --- | --- | --- |
| Schedule Final (Argentina vs Spain, 19 Jul) | **Done** | MatchId **116**; MetLife; `add_sf2_and_final.py` |
| Record Final result after FT | **Done** | 0–1 a.e.t. Ferran Torres 106'; bets **2/2**; [detail](changes/phase-9-ops-closeout.md) |
| Optional ThirdPlace match | **Skipped** | Not used for this tournament path |
| Apply `AddMatchStage` + Feeder* repair | **Done** | `MigrateAsync` + IF NOT EXISTS in `Program.cs` |
| Commit Phase 7–8 working tree | **Done** | `dcb322b` + `7886b40` |
| Knockout re-advance UX | **Done** | Goal warning + Schedule Advance winner |

### Phase 14 Roadmap Mapping

| Roadmap item | Status | Detail |
| --- | --- | --- |
| Docs / planned backlog | **Done** | [phase-14-planned](changes/phase-14-planned.md) |
| `p14-push` — push Phase 13 | **Done** | `5fdd1c4` on `origin/master` (27 Jul 2026) |
| `p14-secrets` — prod env inventory | **Done** | [phase-14-secrets](changes/phase-14-secrets.md) |
| `p14-cors` — CORS + JWT | **Done** | [phase-14-cors](changes/phase-14-cors.md) — policy `Spa`; `CorsOriginsResolver` |
| `p14-compose` — prod Compose | **Done** | [phase-14-compose](changes/phase-14-compose.md) — `docker-compose.prod.yml` |
| `p14-spa-prod` — SPA apiUrl / build | **Done** | [phase-14-spa-prod](changes/phase-14-spa-prod.md) — `REPLACE_ME` placeholder |
| `p14-migrate-smoke` — migrate + smoke | **Done** | [phase-14-migrate-smoke](changes/phase-14-migrate-smoke.md) — health + company path |
| `p14-gate` — formal close | **Done** | Suite **462**; Bugbot high fixed — [phase-14-gate](changes/phase-14-gate.md) |

#### Phase 14 Task 7 — Gate (Done)

| Field | Value |
| --- | --- |
| New (git) | `docs/changes/phase-14-gate.md` |
| Docs (dual) | roadmap Task 7 `[x]`; Phase 14 **[Done]**; index Recommended = ship/ops |
| Outside nested git | workspace `docs/` (VitePress source) mirror |
| Runtime / C# | Healthcheck fix in `docker-compose.yml` (container `$$POSTGRES_*`) |
| Secrets check | Cleared — placeholders / Dev defaults only |
| Tests | Consented **3 Aug 2026** — suite **462** passed |
| Detail | [phase-14-gate](changes/phase-14-gate.md) |

##### Git status (Task 7 snapshot — Phase 14 tree)

From nested git repo (`WorldCup-System/`):

```
 M .env.example
 M WorldCup-System/Program.cs
 M WorldCup-System/appsettings.json
 M docker-compose.yml
 M worldcup-client/README.md
 M worldcup-client/src/environments/environment.ts
?? WorldCup-System/Configuration/
?? WorldCup-System.Tests/Configuration/
?? WorldCup-System.Tests/Integration/CorsIntegrationTests.cs
?? docker-compose.prod.yml
?? docs/changes/phase-14-compose.md
?? docs/changes/phase-14-cors.md
?? docs/changes/phase-14-migrate-smoke.md
?? docs/changes/phase-14-planned.md
?? docs/changes/phase-14-secrets.md
?? docs/changes/phase-14-spa-prod.md
?? docs/changes/phase-14-gate.md
```

Plus dual-docs under workspace-root `docs/` and nested `WorldCup-System/docs/`.

#### Phase 14 Task 6 — Migrate + smoke checklist

| Field | Value |
| --- | --- |
| New (git) | `docs/changes/phase-14-migrate-smoke.md`, nested `README.md` (untracked; includes migrate/smoke pointer) |
| Docs (dual) | roadmap Task 6 `[x]`; index next = Task 7; changes-review; planned; upload-gate; spa-prod next note; VitePress Task 6 link |
| Outside nested git | workspace `README.md` § Target DB migrate + smoke; workspace `docs/` (VitePress source) |
| Runtime / C# | None — docs-only |
| Detail | [phase-14-migrate-smoke](changes/phase-14-migrate-smoke.md) |

##### Git status (Task 6 slice + related docs)

From nested git repo (`WorldCup-System/`), Task 6–relevant paths:

```
?? docs/changes/phase-14-migrate-smoke.md
?? README.md
 M docs/roadmap.md
 M docs/index.md
 M docs/changes-review.md
 M docs/changes/whole-project-upload-gate.md
 M docs/.vitepress/config.mts
```

Also dual-docs (untracked planned/spa-prod updates under `docs/changes/`) and workspace-root `docs/` + `README.md` (outside nested git).

#### Phase 14 Task 5 — SPA production build

| Field | Value |
| --- | --- |
| New (git) | `docs/changes/phase-14-spa-prod.md` |
| Modified (git) | `worldcup-client/src/environments/environment.ts`, `worldcup-client/README.md`, nested `README.md` |
| Docs (dual) | roadmap Task 5 `[x]`; index next = Task 6; changes-review; planned; VitePress sidebar |
| Outside nested git | workspace `README.md` § Production SPA build |
| Detail | [phase-14-spa-prod](changes/phase-14-spa-prod.md) |

##### Git status (Task 5 working tree)

```
 M worldcup-client/src/environments/environment.ts
 M worldcup-client/README.md
?? docs/changes/phase-14-spa-prod.md
```

Plus dual docs (roadmap / index / changes-review / planned / VitePress) and workspace-root `README.md`.

#### Phase 14 Task 4 — Production Compose

| Item | Notes |
| --- | --- |
| Scope | Prod Compose override; no Development defaults; required secrets |
| New (git) | `docker-compose.prod.yml`, `docs/changes/phase-14-compose.md`, `README.md` (nested, was untracked) |
| Modified (git) | `.env.example`; dual docs (roadmap/index/changes-review/planned/secrets/VitePress) |
| Outside git | Workspace root `README.md` § Production Docker Compose |
| Behavior | `ASPNETCORE_ENVIRONMENT=Production`; `${PROD_*:?…}` for JWT/DB/CORS; clear DevAdmin/DevUser; Dev `.env` keys do not satisfy |
| Detail | [phase-14-compose](changes/phase-14-compose.md) |

##### Git status (Task 4 working tree)

```
?? docker-compose.prod.yml
 M .env.example
?? README.md
?? docs/changes/phase-14-compose.md
```

#### Phase 14 Task 3 — CORS + JWT for production

| Item | Notes |
| --- | --- |
| Scope | Runtime CORS allow-list from config; JWT issuer/audience already env-driven (documented only) |
| New (git) | `WorldCup-System/Configuration/CorsOriginsResolver.cs`, `CorsOriginsResolverTests.cs`, `CorsIntegrationTests.cs`, `docs/changes/phase-14-cors.md` |
| Modified (git) | `Program.cs`, `appsettings.json`, `docker-compose.yml`, `.env.example` |
| Docs (dual) | roadmap Task 3 `[x]`; index next = Task 4; changes-review; planned; secrets pointer; VitePress sidebar |
| Behavior | Policy `Spa`; array or `;`/`,` origins; reject `*`; strip trailing `/`; default `http://localhost:4200` |
| JWT | No new code — `JWT__ValidIssuer` / `JWT__ValidAudience` / `JWT__Secret` already used |
| Detail | [phase-14-cors](changes/phase-14-cors.md) |

##### Git status (Task 3 working tree)

```
 M .env.example
 M WorldCup-System/Program.cs
 M WorldCup-System/appsettings.json
 M docker-compose.yml
?? WorldCup-System/Configuration/
?? WorldCup-System.Tests/Configuration/
?? WorldCup-System.Tests/Integration/CorsIntegrationTests.cs
?? docs/changes/phase-14-cors.md
```

#### Phase 14 Task 2 — Prod secrets inventory

| Item | Notes |
| --- | --- |
| Scope | Docs/example only — inventory from `Program.cs`, appsettings, Compose |
| New (git) | `docs/changes/phase-14-secrets.md` (+ planned page if still untracked) |
| Modified (git) | `.env.example`, `docs/roadmap.md`, `docs/index.md`, `docs/changes-review.md`, `docs/changes/whole-project-upload-gate.md`, `docs/workflow.md`, `docs/.vitepress/config.mts` |
| Outside git | Workspace root `README.md` § Configuration → Production secrets inventory |
| Secrets in diff | None — `REPLACE_ME_*` placeholders + existing local-dev Compose defaults |
| Runtime | Unchanged under Task 2 — CORS runtime landed in Task 3 |
| Detail | [phase-14-secrets](changes/phase-14-secrets.md) |

##### Git status (Task 2 working tree)

```
 M .env.example
 M docs/.vitepress/config.mts
 M docs/changes-review.md
 M docs/changes/whole-project-upload-gate.md
 M docs/index.md
 M docs/roadmap.md
 M docs/workflow.md
?? docs/changes/phase-14-planned.md
?? docs/changes/phase-14-secrets.md
```

Workspace (not in nested git repo): `README.md` — Production secrets inventory section + link to `docs/changes/phase-14-secrets.md`.

### Phase 13 Roadmap Mapping

| Roadmap item | Status | Detail |
| --- | --- | --- |
| Docs / planned backlog | **Done** | [phase-13-planned](changes/phase-13-planned.md) |
| `p13-company-model` — Company + User.CompanyId | **Done** (interim gate closed; suite **391**) | [phase-13-company-model](changes/phase-13-company-model.md) |
| `p13-company-api` — create / join / admin | **Done** (interim gate closed; suite **414**) | [phase-13-company-api](changes/phase-13-company-api.md) |
| `p13-leaderboard-scope` — company filter | **Done** (interim gate closed; suite **399**) | [phase-13-leaderboard-scope](changes/phase-13-leaderboard-scope.md) |
| `p13-spa` — join + company board | **Done** (interim Bugbot/Jasmine pending) | [phase-13-spa](changes/phase-13-spa.md) |
| `p13-tests-gate` — tests + review | **Tests cleared** (suite **419**); Bugbot formal **open** | [phase-13-tests-gate](changes/phase-13-tests-gate.md) |

### Phase 12 Roadmap Mapping

| Task | Status | Evidence |
| --- | --- | --- |
| Docs / planned backlog | **Done** | [phase-12-planned](changes/phase-12-planned.md) |
| `p12-draw` — cup, hosts+random, groups A–L | **Done** | Script + migration + TeamService per-WC unique; [detail](changes/phase-12-draw.md) |
| `p12-group-sim` — 72 group matches + scores | **Done** (gate closed) | Schedule + Poisson + wipe; Bugbot highs fixed; unittest **14**; smoke 72/193; [detail](changes/phase-12-group-sim.md) |
| `p12-bracket` — best thirds + R32→Final | **Done** | FIFA Art. 12 skeleton; 31 KO; unittest **22**; smoke 103; [detail](changes/phase-12-bracket.md) |
| `p12-tests-gate` — smoke + review | **Done** (gate closed) | Unittest **23**; smoke **103**; Bugbot highs fixed; [detail](changes/phase-12-tests-gate.md) |

### Phase 11 Roadmap Mapping

| Task | Status | Evidence |
| --- | --- | --- |
| `p11-external-stage` — IdStage for timeline URLs | **Done** | Column + migration + SetExternal + calendar parse + SyncResult persist; **278** tests; [detail](changes/phase-11-external-stage.md) |
| `p11-timeline-provider` — FIFA timeline Goal! parse | **Done** | Provider + DTOs + `TimelineBaseUrl` + DI + unit tests; [detail](changes/phase-11-timeline-provider.md) |
| `p11-player-resolve` — FIFA player → local Player | **Done** | `TimelinePlayerResolver` + `ExternalPlayerId`; **32** resolver + suite **342**; [detail](changes/phase-11-player-resolve.md) |
| `p11-scorer-apply` — Idempotent Goal rewrite | **Done** | `TimelineScorerApplyService`; count mismatch throws; same-minute order; **10** + suite **353**; [detail](changes/phase-11-scorer-apply.md) |
| `p11-sync-wire` — SyncResult / SyncFinishedResults | **Done** | Fail-soft timeline + apply; Bugbot clean; wiring **8**; suite **361**; [detail](changes/phase-11-sync-wire.md) |
| `p11-backfill` — Scorers-only Admin batch | **Done** | `SyncScorers` / `SyncScorersForWorldCup` + script; suite **375**; [detail](changes/phase-11-backfill.md) |
| `p11-recent-events-honesty` — Hide placeholders | **Done** | `ToHonestPlayerName` + client sanitizer; suite **380**; [detail](changes/phase-11-recent-events-honesty.md) |
| `p11-tests-gate` — Review + `dotnet test` | **Done** | Bugbot highs fixed; suite **382**; [detail](changes/phase-11-tests-gate.md) |

### Phase 10 Roadmap Mapping

| Task | Status | Evidence |
| --- | --- | --- |
| `p10-match-sync` — Post-match FIFA sync + resolve | **Done** | `FifaCalendarMatchResultProvider`, `MatchResultSyncService`, `ExternalMatchId`, Admin sync endpoints, 19 new tests |
| Explicit MatchStatus + home/away orientation | **Done** | Provider requires status; sync orients/swaps sides |
| RabbitMQ decision — v1 without broker | **Done** | Documented; in-process Admin/API only |
| Client sync surface | **Done** | `match-api.service.ts` + sync DTOs |
| `p10-fixtures-ux` — Bettable matches first | **Done** | Action default; chips; Open/Live/Finished; jump fixed; Jasmine **12**; [detail](changes/phase-10-fixtures-ux.md) |
| `p10-wc-styling` — WC visual polish | **Done** | Theme tokens, atmosphere, typography; Jasmine **25** + API **268**; [detail](changes/phase-10-styling.md) |
| `p10-cleanup` — Duplicates & unused | **Done** | Root client + dump removed; dual docs kept; [detail](changes/phase-10-cleanup.md) |

### Phase 8 Roadmap Mapping

| Task | Status | Evidence |
| --- | --- | --- |
| `p8-dev-roles` — Dev Admin + User | Done | Committed `Program.cs` DevAdmin / DevUser (`Admin123!` / `User123!`) |
| `p8-login-return` — Deep-link restore | Done | Guards pass `returnUrl`; login `sanitizeReturnUrl` + `navigateByUrl`; defaults `/admin` / `/dashboard` |
| `p8-user-dashboard` — User hub | Done | `/dashboard` — `getMySummary` + active bets; shell nav for non-admins |
| `p8-bet-auto-resolve` — Resolve hardening | Done | `ResolveBetsResultDTO` +3/0 breakdown; DeleteGoal re-score; TryAuto + world-cup bulk |
| `p8-admin-leaderboard` — Resolve UI | Done | Schedule breakdown + hub `ResolveBetsForWorldCup` |
| `p8-live-snapshot` — Live feed API | Done | `GET Match/GetLiveSnapshot`; fixtures 30s poll |
| `p8-live-action-api` — Client write APIs | Done | goal / card / team-stats API services |
| `p8-admin-live-console` — Live console | Done | `/admin/live/{matchId}` |
| `p8-admin-team-import` — Manual teams | Done | Admin Teams create + `updateTeam` |
| `p8-admin-hub` — Ops hub | Done | `/admin` → ops cards (+ generate bracket) |

### Phase 7 Roadmap Mapping (same uncommitted tree)

| Task | Status | Evidence in this diff |
| --- | --- | --- |
| `p7-knockout` — Bracket + feeders | Done | `KnockoutService`, `KnockoutController`, `MatchStage`, feeder columns, `/bracket`, hub GenerateBracket |
| `p7-fixtures-seed` — WC 2026 offline data | Done | `scripts/seed-wc2026-groups.sql`, `import_wc2026_finished_matches.py` (through both SFs), `RoundOf32` |
| Standings H2H tiebreakers | Done | `StandingsService.ApplyHeadToHeadTiebreakers` |
| `p7-live-refresh` / resolve after goals | Done | Fixtures snapshot poll; `GoalService.TryResolveAndAdvance` |
| `p7-external-api` | Cancelled | Manual scripts + Admin live console; Phase 10 post-match FIFA sync is a narrower revisit |

### Review Gate — Status

> **Success:** **Phase 14 Task 7 gate closed (3 Aug 2026).** Bugbot high fixed; dual docs mirrored; secrets placeholders only; consented suite **462**. Phase 14 **[Done]**. Detail: [phase-14-gate Review gate](changes/phase-14-gate.md#review-gate).

#### Phase 14 Task 7 — Formal gate (`p14-gate`)

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Task 1 push complete | Critical | **Cleared** | `5fdd1c4` on origin |
| Tasks 2–6 deliverables present | High | **Cleared** | Detail pages + code as scoped |
| No secrets in git diff | Critical | **Cleared** | Placeholders / Dev defaults only |
| Dual docs VitePress ↔ git mirror | High | **Cleared** | Both trees through Done |
| High/critical Bugbot (Tasks 3–5 + Compose/SPA) | High | **Cleared** | Healthcheck → `$$POSTGRES_*` |
| `dotnet test` (CORS API changed) | High | **Cleared** | Consent **3 Aug 2026**; suite **462** |
| Mark Phase 14 Done on roadmap/index | High | **Cleared** | Task 7 `[x]`; Phase 14 **[Done]** |

> **Success:** **Phase 14 Task 6 review gate cleared (migrate/smoke docs).** Health + company smoke table; wipe warnings; must-include migrations listed; no prod migrate by agent. Detail: [phase-14-migrate-smoke Review gate](changes/phase-14-migrate-smoke.md#review-gate). Next: Task 7 gate (now in progress).

#### Phase 14 Task 6 — Migrate + smoke (docs gate)

High/critical items that must clear **before** treating Task 6 as closed:

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Checklist omits health or company isolation | Critical | **Cleared** | Smoke rows 1–5 |
| Wipe path presented as safe for prod | Critical | **Cleared** | Explicit do-not + Phase 9 link |
| Agent migrates prod without confirmation | High | **Cleared by design** | Docs-only |
| Recent migrations not called out | High | **Cleared** | MatchSync / multi-cup / Company |
| Wrong `cd` into API host before `ef` | High | **Cleared** | Nested git root only — [migrate-smoke](changes/phase-14-migrate-smoke.md) |
| Dual docs VitePress ↔ git mirror (Task 6) | High | **Cleared** | Markdown review synced nested `WorldCup-System/docs/` |
| Nested README secrets leave shell in API host | High | **Cleared** | `cd ..` after user-secrets — nested `README.md` |
| Second User missing for join/isolation smoke | Medium | Documented | Smoke notes — create User out-of-band in Production |

> **Success:** **Phase 14 Task 5 review gate cleared (SPA prod).** Localhost trap replaced with `REPLACE_ME` placeholder; Dev unchanged; Markdown dual-docs verified vs git. Detail: [phase-14-spa-prod Review gate](changes/phase-14-spa-prod.md#review-gate).

#### Phase 14 Task 5 — SPA production build (docs / client gate)

High/critical items that must clear **before** treating Task 5 as closed:

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Prod `apiUrl` silently stays localhost | Critical | **Cleared** | `environment.ts` → `https://api.REPLACE_ME.example` |
| Personal / private API host as default | High | **Cleared by design** | Placeholder only |
| Dev `ng serve` broken | High | **Cleared** | `fileReplacements` → `environment.development.ts` |
| Wrong client / dist path in docs | High | **Cleared** | Canonical `WorldCup-System/worldcup-client`; serve `dist/.../browser/` |
| Dual docs VitePress ↔ git mirror | High | **Cleared this Markdown pass** | `docs/` ≡ `WorldCup-System/docs/` |
| Ship bundle still containing `REPLACE_ME` | Medium | **Documented** | Pre-deploy checklist on detail page |

> **Success:** **Phase 14 Task 4 review gate cleared (Compose).** Bugbot high fixed (`PROD_*` names); re-review clean. Detail: [phase-14-compose Review gate](changes/phase-14-compose.md#review-gate). Next: Task 5 SPA prod.

> **Success:** **Phase 14 Task 3 review gate cleared.** Bugbot high fixed (env scalar prefer); suite **462** passed. Detail: [phase-14-cors Review gate](changes/phase-14-cors.md#review-gate). Next: Task 4 Compose.

> **Success:** **Phase 14 Task 2 review gate cleared (docs).** No high/critical blockers for this docs-only slice. Detail: [phase-14-secrets Review gate](changes/phase-14-secrets.md#review-gate).

> **Success:** **Product gates cleared (Phases 9–13).** Phase 13 suite **438**; Phase 11 Task 8 suite **382**; Phase 12 Task 4 unittest **23** / smoke **103**. Upload packaging for 9–12 **closed** (`ac0608e`); Phase 13 on origin (`5fdd1c4`). Historical: [whole-project-upload-gate](changes/whole-project-upload-gate.md).

> **Success:** **Phase 13 Task 3 interim gate closed.** Bugbot highs fixed; suite **414**. Detail: [phase-13-company-api Review gate](changes/phase-13-company-api.md#review-gate).

> **Success:** **Phase 13 Task 4 interim gate closed.** Bugbot clean; suite **399** (pre–Task 3 tests). Detail: [phase-13-leaderboard-scope Review gate](changes/phase-13-leaderboard-scope.md#review-gate).

> **Info:** **Phase 13 Task 5 interim gate pending.** SPA implemented; Bugbot + Jasmine smoke still required. Markdown docs cleared this pass. Detail: [phase-13-spa Review gate](changes/phase-13-spa.md#review-gate).

> **Success:** **Phase 13 Task 6 tests cleared.** Suite **419**; isolation integration + unit. Bugbot formal still open — phase not Done. Detail: [phase-13-tests-gate Review gate](changes/phase-13-tests-gate.md#review-gate).

> **Success:** **Phase 13 Task 2 interim gate closed.** Bugbot clean; suite **391**. Full Phase 13 gate remains Task 6 Bugbot. Detail: [phase-13-company-model Review gate](changes/phase-13-company-model.md#review-gate).

#### Phase 14 Task 4 — Production Compose (ops gate)

High/critical items that must clear **before** treating Task 4 as closed:

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Local Dev `.env` silently satisfies prod `${VAR:?}` | High | **Cleared (Bugbot)** | Distinct `PROD_*` interpolation names |
| Prod path still `Development` | Critical | **Cleared** | Override sets `Production` |
| Weak JWT/DB defaults without required env | Critical | **Cleared** | `${PROD_*:?…}` fail if unset/empty |
| Real secrets in override YAML | Critical | **Cleared by design** | Host env only |
| DevAdmin/DevUser seed in Production | High | **Cleared** | Env cleared; seed `IsDevelopment()` only |
| Local Dev Compose broken | High | **Cleared** | Base file unchanged |
| Duplicate Postgres port from override merge | High | **Cleared by design** | No `ports` in prod file |
| Healthcheck invalid | High | **Cleared** | Inherited `/health` |
| Dual docs mirror | High | **Cleared this pass** | VitePress ↔ git mirror |
| Postgres published via base `5432` | Medium | **Documented** | Firewall on shared hosts |

#### Phase 14 Task 3 — CORS + JWT (runtime gate)

High/critical items that must clear **before** testing or treating Task 3 as closed:

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| `AllowAnyOrigin` + credentials | Critical | **Cleared by design** | `WithOrigins` only; `*` throws in `CorsOriginsResolver` |
| Production override path | High | **Cleared** | `Cors__AllowedOrigins` or indexed `__N` |
| Env scalar ignored under JSON array merge | High | **Cleared (Bugbot)** | Prefer `section.Value` over children; regression test |
| Dev localhost default preserved | High | **Cleared** | appsettings + resolver fallback + Compose default |
| Policy name `AddPolicy` / `UseCors` match | High | **Cleared** | `CorsOriginsResolver.PolicyName` (`Spa`) |
| JWT issuer/audience prod path | High | **Cleared** | Already env-driven; documented (no new JWT code) |
| Dual docs mirror | High | **Cleared this pass** | VitePress `docs/` ↔ `WorldCup-System/docs/` |
| Trailing-slash mismatch | Medium | **Cleared** | Strip `/`; docs say no slash |
| Unit/integration tests present | High (pre formal gate) | **Cleared** | Resolver + CORS integration; suite **462** passed |

#### Phase 14 Task 2 — Prod secrets inventory (docs gate)

High/critical items that must clear **before** treating Task 2 as ready (docs-only — no `dotnet test` required):

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Real passwords / JWT secrets / connection strings in git or Markdown | Critical | **Cleared** | Placeholders only (`REPLACE_ME_*`); no filled `.env` |
| Inventory covers API boot-required keys | High | **Cleared** | `ConnectionStrings__DefaultConnection`, `JWT__Secret`, issuer/audience, `ASPNETCORE_ENVIRONMENT` |
| Dual docs VitePress ↔ git mirror | High | **Cleared** | `docs/` and `WorldCup-System/docs/` same content |
| Runtime / `Program.cs` changed under Task 2 | High | **N/A — docs only** | CORS runtime landed in Task 3 |
| Weak local Compose defaults in `.env.example` mistaken for prod | Medium | **Cleared (Task 4)** | [phase-14-compose](changes/phase-14-compose.md); do not reuse `.env.example` values |
| CORS allow-list env documented as shipped | Medium | **Cleared Task 3** | [phase-14-cors](changes/phase-14-cors.md) |

#### Phase 13 Task 6 — Tests & gate (formal)

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Two-company leaderboard leak (integration) | High (gate) | **Cleared (tests)** | `GetLeaderboard_TwoCompanies_NeverLeaksUsersAcrossCompanies` |
| Null company → empty board (integration) | High (gate) | **Cleared (tests)** | `GetLeaderboard_WhenUserHasNoCompany_ReturnsEmptyArray` |
| Anonymous GetLeaderboard → 401 | High (gate) | **Cleared (tests)** | `GetLeaderboard_WithoutJwt_ReturnsUnauthorized` |
| Bidirectional unit isolation | High (gate) | **Cleared (tests)** | `GetLeaderboard_Bidirectional_NeitherCompanyIncludesTheOther` |
| Invite uniqueness (service) | High (gate) | **Cleared (tests)** | `CreateAsync_GeneratesDistinctInviteCodes_AndDoesNotReuseExisting` |
| `dotnet test` green | High (gate) | **Cleared** | Suite **419** passed |
| Bugbot formal Task 6 review | High (gate) | **Open** | Required to mark Phase 13 Done |
| Markdown docs (changes-review + Task 6 page) | High (gate) | **Cleared this pass** | Dual-docs mirror VitePress + git |
| Task 5 SPA interim Bugbot/Jasmine | High (phase) | **Pending** | [phase-13-spa](changes/phase-13-spa.md) |

#### Phase 13 Task 5 — SPA company UX (interim gate)

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Bugbot high/critical on Task 5 SPA files | High (interim gate) | **Pending** | Uncommitted `worldcup-client` company feature + routes/auth/board |
| Markdown docs review (changes-review + Task 5 page) | High (interim gate) | **Cleared this pass** | Dual-docs mirror VitePress + git |
| Jasmine company/auth specs | High (interim gate) | **Pending** | 4 API + 2 auth role specs; run after Bugbot |
| Join applies CompanyAdmin JWT | High | **Cleared by design** | `applyAccessToken` after Join `token` |
| Leaderboard without login | High | **Cleared** | `authGuard` on `/leaderboard` |
| Leaderboard without company | High | **Cleared** | Empty CTA → `/company` |
| Client-supplied company id on board | Critical | **N/A** | Server scope only; SPA never sends board `companyId` |
| Branding / custom themes | Info | Out of scope | Existing WC tokens only |
| Formal phase gate | Info | **Tests cleared; Bugbot open** | [phase-13-tests-gate](changes/phase-13-tests-gate.md) |

#### Phase 13 Task 3 — Company API (interim gate)

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Bugbot high/critical on Task 3 API files | High (interim gate) | **Cleared** | Role-before-join; Join token fail-soft + flat; orphan Admin strip |
| Markdown docs review (changes-review + Task 3 page) | High (interim gate) | **Cleared** | Dual-docs mirror VitePress + git |
| Task 3 unit tests + `dotnet test` | High (interim gate) | **Cleared** | **15** CompanyService tests; suite **414** |
| Cross-company member / invite leak | High | **Mitigated in service** | ResolveManagedCompanyAsync; CompanyAdmin own-company only |
| Invite uppercase normalize at Create/Join/Rotate | High | **Cleared this task** | `NormalizeInviteCode` + unique generate |
| CompanyAdmin first-join + JWT re-issue | High | **Cleared this task** | Role before membership; flat token/expiration |
| `UnauthorizedAccessException` → 403 | High | **Cleared this task** | `ApiErrorHelper` |
| Concurrent first-join race | Medium | Accepted v1 | Rare dual CompanyAdmin |
| SPA join UI | Info | **Cleared Task 5** | [phase-13-spa](changes/phase-13-spa.md) |
| Full isolation / leak tests | Info | **Cleared Task 6 tests** | Suite **419**; Bugbot formal still open |

#### Phase 13 Task 4 — Leaderboard scope (interim gate)

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Bugbot high/critical on Task 4 leaderboard files | High (interim gate) | **Cleared** | Bugbot found no bugs |
| Markdown docs review (changes-review + Task 4 page) | High (interim gate) | **Cleared** | Dual-docs mirror VitePress + git |
| Leaderboard unit tests + `dotnet test` | High (interim gate) | **Cleared** | **14** Task 4 cases; suite **399** (pre–Task 3) |
| Client-supplied `companyId` query | Critical | **Cleared by design** | No param; scope from JWT → DB `User.CompanyId` |
| Unauthenticated `GetLeaderboard` public | High | **Cleared** | `[Authorize]` on endpoint |
| Cross-company ranking leak | High | **Cleared by design** | Filter bets to same-company user ids |
| Null company → 400 | Medium | **Cleared by design** | Empty list (SPA Task 5) |
| Company Create/Join API | Info | **Cleared Task 3** | [phase-13-company-api](changes/phase-13-company-api.md) |
| Two-company integration leak test | Info | **Cleared Task 6 tests** | [phase-13-tests-gate](changes/phase-13-tests-gate.md) |

#### Phase 13 Task 2 — Company model (interim gate)

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Bugbot high/critical on Task 2 model files | High (interim gate) | **Cleared** | No bugs found |
| Markdown docs review (changes-review + Task 2 page) | High (interim gate) | **Cleared** | Dual-docs mirror |
| Task 2 smoke + `dotnet test` | High (interim gate) | **Cleared** | **4** new; suite **391** |
| Omit `AddCompany` migration / snapshot from commit | Critical | **Cleared** | Landed on origin with Phase 13 (`5fdd1c4`) |
| EF migration vs `Program.cs` IF NOT EXISTS repair | High | **Cleared by design** | Startup SQL mirrors Company table + `CompanyId` FK |
| Invite uppercase normalize at Create/Join | High | **Cleared Task 3** | Write-path in CompanyService |
| Dual docs VitePress vs git mirror drift | High | **Clearing this pass** | Mirror both trees |
| CompanyAdmin never assigned yet | Medium | **Cleared Task 3** | First-join bootstrap + JWT re-issue |
| Isolation unit/integration tests | Info | **Cleared Task 6 tests** | Suite **419**; Bugbot formal still open |

#### Upload packaging (must clear before / during commit)

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Omit EF migrations (`ExternalStageId`, `ExternalPlayerId`, `AllowMultipleTeamsPerCountry`, **`AddCompany`**) | Critical | **Cleared** | Landed with Phases 11–13 packaging / Phase 13 push |
| Omit Phase 11 MatchSync providers / unit tests | Critical | **Open until commit** | Include untracked MatchSync + Timeline* tests |
| Omit Phase 12 `simulate_wc2030.py` / unittest | High | **Open until commit** | Include scripts + TeamService multi-cup |
| Dual docs VitePress vs git mirror drift | High | **Clearing this pass** | Mirror Markdown both trees |
| Accidental WC 2026 wipe via importer | High | **Documented** | Prefer `add_sf2_and_final.py` |
| Unfixed Phase 11/12 Bugbot highs | High | **Cleared** | See Task 8 / Task 4 gates |

High/critical items that were fixed **before** declaring phase Done (historical):

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Phase 9 — wrong Final scoreline / scorer | High | **Cleared** | DB 0–1; GoalId **298** → Ferran Torres 106' |
| Phase 9 — bets open after FT | High | **Cleared** | MatchId **116** bets **2/2** resolved |
| Phase 9 — accidental wipe via importer | High | **Documented** | Prefer `add_sf2_and_final.py`; importer warns in docstring |
| Phase 9 — C# / migration drift | Critical | **N/A** | No C# in close-out |
| Phase 11 Task 8 Bugbot highs/criticals | High (gate) | **Cleared** | Cancel rethrow + jersey-99 name-only; [tests-gate](changes/phase-11-tests-gate.md) |
| Phase 11 Task 8 — `dotnet test` green | High (gate) | **Cleared** | Suite **382** |
| Phase 11 Task 8 — roadmap / plan Status → Done | High (acceptance) | **Cleared** | Phase 11 Done |
| Phase 11 Task 7 — never show literal `Tournament Scorer` | High (acceptance) | **Cleared by design** | API nulls name; fixtures skip empty `playerName` |
| Phase 11 Task 7 — match detail / admin live same rule | High (acceptance) | **Cleared by design** | `BuildTeamStatsDto` + `allEvents()` |
| Phase 11 Task 7 — real names still shown | High | **Cleared by design** | Name-only omit of placeholder string |
| Phase 11 Task 7 Bugbot highs/criticals | High (gate) | **Cleared** | Bugbot found no bugs |
| Phase 11 Task 7 — honesty unit suite | High (gate) | **Cleared** | Honesty **3** + suite **380** |
| Phase 11 Task 6 — no FT rewrite / bets / knockout on SyncScorers | High | **Cleared** | ScoreChanged=false; BetsResolved=0; no ApplyScoreIdempotent |
| Phase 11 Task 6 — Applied only for Applied/AlreadyMatched | High | **Cleared** | Skipped/Warning → Applied=false |
| Phase 11 Task 6 — empty timeline → Skipped | High | **Cleared** | `skipWhenNoTimelineGoals` |
| Phase 11 Task 6 — batch hard fail → Warning | High | **Cleared** | Unlike SyncFinishedResults (null ScorerStatus) |
| Phase 11 Task 6 — cancel not swallowed | High | **Cleared** | Rethrow in core + batch catch |
| Phase 11 Task 6 — BatchDelayMilliseconds on WC backfill | High | **Cleared** | Same option as FT batch |
| Phase 11 Task 6 Bugbot highs/criticals | High (gate) | **Cleared** | Cancel swallow + local scores |
| Phase 11 Task 6 — backfill unit suite | High (gate) | **Cleared** | **14** cases; suite **375** |
| Phase 11 Task 5 Bugbot highs/criticals | High (gate) | **Cleared** | No bugs found |
| Phase 11 Task 5 — wiring unit suite | High (gate) | **Cleared** | **8** wiring cases; suite **361** |
| Phase 11 Task 5 — FT still succeeds when timeline throws | High | **Cleared** | Fail-soft → `ScorerStatus=Warning` |
| Phase 11 Task 5 — missing stage does not abort FT | High | **Cleared** | `Skipped`; no timeline HTTP |
| Phase 11 Task 5 — batch continues on scorer warning | High | **Cleared** | Per-match scorer status; hard FT errors still isolated |
| Phase 11 Task 5 — orientation to apply | High | **Cleared** | `homeMapsToTeamOne` from calendar orient |
| Phase 11 Task 4 Bugbot highs/criticals | High (gate) | **Cleared** | Same-minute fix; suite **353** |
| Phase 11 Task 4 — apply unit suite | High (gate) | **Cleared** | **10** apply + suite **353** |
| Phase 11 Task 4 — count mismatch mutates goals | High | **Cleared by design** | Throws; no Goal writes / SaveAsync |
| Phase 11 Task 4 — scorer-only re-resolve bets / knockout | High | **Cleared by design** | No bet/knockout deps; test asserts |
| Phase 11 Task 4 — own-goal wrong team | High | **Cleared** | Conceding team id to resolver |
| Task 3 Bugbot highs/criticals | High (gate) | **Cleared** | Jersey-99 filter fixed; suite **342** |
| Task 3 — resolver unit suite | High (gate) | **Cleared** | 32 resolver cases |
| Task 3 — ambiguous name / cross-team id | High | **Cleared** | Throws `InvalidOperationException` |
| Task 3 — Tournament Scorer when real name exists | High | **Cleared** | Eligible squad excludes placeholder by name |
| Task 2 Bugbot highs/criticals | High (gate) | **Cleared** | Timeline provider gate closed; suite **310** |
| Task 2 — provider unit suite | High (gate) | **Cleared** | Mexico–SA + 32 provider tests |
| Task 1 — `ExternalStageId` unit tests | High | **Cleared** | SetExternal + SyncResult stage cases; suite **278** |
| Task 1 Bugbot highs/criticals | High (gate) | **Cleared** | No high/critical; medium batch DTO refresh fixed |
| Phase 10 Task 4 — wrong SPA deleted | Critical | **Cleared** | Root stale client only; `WorldCup-System/worldcup-client` kept |
| Phase 10 Task 4 — git docs tree deleted | High | **Cleared** | Dual docs retained (VitePress + git mirror) |
| Phase 10 Task 4 — broken client path in docs/README | High | **Cleared** | Canonical path documented |
| Phase 10 Task 4 Bugbot highs/criticals | High (gate) | **None** | Filesystem/docs cleanup only |
| Phase 10 Task 3 Bugbot review | High (gate) | **Cleared** | Mediums fixed; Jasmine **25** + API **268** |
| Phase 10 Task 2 Bugbot review | High (gate) | **Cleared** | No high/critical findings |
| Live snapshot stale `canBet` | High | **Fixed** | `canBet` forced false when live status ≠ `Scheduled` |
| Sync missing MatchStatus / home-away orientation | High | **Fixed** | Explicit status + side orientation in MatchSync |
| MatchResultSync unit tests | High | **Fixed** | 19 new tests; suite 268 green (pre–Task 1 stage) |
| Parallel possession updates race (live console) | High | **Fixed** | Sequential `updateTeamStats` + possession sum check |
| Edited applied migration `IdentityLongKeys` | Critical | **Cleared** | Forward `AddMatchStage` only |

Accepted / medium (non-blocking):

| Item | Severity | Status | Notes |
| --- | --- | --- | --- |
| Phase 14 Task 2 — weak local Compose defaults in `.env.example` | Medium | Documented | Local Dev only; prod path uses `:?` required env — [phase-14-compose](changes/phase-14-compose.md) |
| Phase 14 Task 3 — trailing-slash / wrong Origin form | Medium | Mitigated | Resolver strips `/`; docs forbid `*` and trailing slash — [phase-14-cors](changes/phase-14-cors.md) |
| Phase 14 Task 5 — ship SPA with `REPLACE_ME` / wrong `apiUrl` | Medium | Documented | Pre-deploy checklist — [phase-14-spa-prod](changes/phase-14-spa-prod.md#pre-deploy-checklist) |
| Phase 14 Task 6 — wrong env / CORS mistaken for schema failure | Medium | Documented | Failure tips — [phase-14-migrate-smoke](changes/phase-14-migrate-smoke.md#failure-tips) |
| Phase 14 Task 6 — accidental wipe importer on target DB | Medium | Documented | Explicit do-not — [phase-14-migrate-smoke](changes/phase-14-migrate-smoke.md#do-not) |
| Phase 14 Task 6 — second User required for join/isolation smoke | Medium | Documented | Smoke notes — [phase-14-migrate-smoke](changes/phase-14-migrate-smoke.md#smoke-checklist) |
| Task 7 — filter jersey 99 only for display | Medium | **Rejected** | Name-only; jersey 99 may be real (Task 3) |
| Admin live picker still excludes jersey 99 | Low | Accepted | Picker UX; display honesty remains name-only |
| Apply not wired to SyncResult | Info | **Cleared** | Task 5 wired fail-soft |
| BatchDelayMilliseconds default 250 | Medium | Accepted v1 | Tests force `0`; FIFA public API courtesy |
| Sync message appends scorer note always on FT | Low | Accepted | `Scorers: {scorerMessage}` even when Skipped |
| Auto-create named Forwards | Medium | Accepted v1 | Prefer named player over Tournament Scorer |
| Name truncate to 64 chars | Medium | Accepted | Matches `CK_Player_Name_Length` |
| `AddPlayerExternalPlayerId` migrate | Medium | Ops | Migrate + IF NOT EXISTS on API start |
| Timeline HTTP / empty payload | Medium | Documented | Provider throws; Task 5 catches as scorer warning |
| Own Goal / Penalty label variants | Medium | Accepted v1 | Common `TypeLocalized` strings; Mexico sample was `Goal!` only |
| Side inference via score delta | Medium | Accepted v1 | No FIFA HomeOrAway on event; ambiguous OG defaults away |
| Zip by minute order only | Medium | Accepted v1 | Same-minute goals ordered by ExternalPlayerId / name |
| `AddMatchExternalStageId` migrate | Medium | Ops | Migrate + IF NOT EXISTS on API start |
| Squad round-robin scorers | Medium | Accepted | Better than Tournament Scorer; real names = Phase 11 Tasks 3–4 |
| Google Fonts CDN / CSP / offline | Medium | Open | Fallbacks in `--font-*`; self-host later if needed |
| `color-mix` / `backdrop-filter` | Medium | Accepted v1 | Readable without blur on older engines |
| Gold-on-dark contrast | Medium | Mitigated | Dark text on gold CTAs; bright gold accents |
| Jump scroll after filter change | Medium | **Fixed** | Live/Finished → Action; `setTimeout` before scroll |
| Unknown status → Open bucket | Medium | Accepted v1 | [phase-10-fixtures-ux](changes/phase-10-fixtures-ux.md) |
| Re-sync placeholder scorers | Medium | Accepted | Score-only model until timeline apply |
| Fixtures auto-resolve / TBD update / stage lock | Medium | **Fixed** | Snapshot on load; nullable UpdateMatch |
| `AddMatchStage` + Feeder* + `ExternalMatchId` migrate | Medium | Ops | Migrate on API start |
| Silent swallow of incomplete-stats / advance errors | Medium | Accepted | Optional Serilog later |

### Risk Summary

| Risk | Severity | Details | Action |
| --- | --- | --- | --- |
| Phase 14 Task 7 Bugbot / formal gate | High (gate) | Tasks 1–6 impl Done; phase not Done until Bugbot + consented tests | **Open — review running** — [phase-14-gate](changes/phase-14-gate.md#review-gate) |
| Phase 14 Task 7 secrets in uncommitted tree | Critical | Real passwords / JWT / connection strings must not land in git | **Open (check)** — placeholders + Dev defaults only |
| Phase 14 Task 7 `dotnet test` without consent | High | CORS API + tests changed; user rule ask-before test/build | **Blocked on consent** |
| Phase 14 Task 3 `AllowAnyOrigin` / `*` | Critical | Credentialed CORS must stay allow-list only | **Cleared** — [phase-14-cors](changes/phase-14-cors.md#review-gate) |
| Phase 14 Task 3 policy name mismatch | High | `AddPolicy` vs `UseCors` | **Cleared** — shared `CorsOriginsResolver.PolicyName` |
| Phase 14 Task 4 still ships Dev Compose defaults | High (ops) | Base Compose stays Development; prod override sets Production + required secrets | **Cleared** — [phase-14-compose](changes/phase-14-compose.md#review-gate) |
| Phase 14 Task 5 prod SPA silent localhost `apiUrl` | Critical | Default prod env pointed at Dev API | **Cleared** — `REPLACE_ME` placeholder — [phase-14-spa-prod](changes/phase-14-spa-prod.md#review-gate) |
| Phase 13 Task 6 Bugbot formal gate | High (gate) | Isolation tests green; phase not Done until Bugbot | **Cleared** — suite **438**; [tests gate](changes/phase-13-tests-gate.md#review-gate) |
| Phase 13 Task 5 interim Bugbot / Jasmine | High (interim) | SPA `/company` + leaderboard auth | **Pending** — Markdown cleared; [SPA](changes/phase-13-spa.md#review-gate) |
| Cross-company leaderboard leak | High | Must filter by caller `CompanyId` | **Cleared (tests)** — integration bidirectional + unit; suite **419** |
| Client-supplied company id on GetLeaderboard | Critical | Query param must not override DB membership | **Cleared by design** — no `companyId` query |
| Unauthenticated GetLeaderboard still public | High | Breaking vs Phase 4 public board | **Cleared** — `[Authorize]` + integration 401 |
| Phase 13 Task 4 Bugbot / interim docs + tests gate | High (interim) | Leaderboard scope | **Cleared** — suite **399**; [leaderboard scope](changes/phase-13-leaderboard-scope.md#review-gate) |
| Phase 13 Task 3 Bugbot / interim docs + tests gate | High (interim) | Company API | **Cleared** — Bugbot highs fixed; suite **414**; [company API](changes/phase-13-company-api.md#review-gate) |
| Phase 13 Task 2 Bugbot / interim docs + tests gate | High (interim) | Company model | **Cleared** — Bugbot clean; suite **391**; [company model](changes/phase-13-company-model.md#review-gate); formal gate Task 6 Bugbot |
| Upload omits `AddCompany` migration | Critical | New Company table + `User.CompanyId` | **Cleared** — on origin (`5fdd1c4`); Task 6 must-include list |
| Invite code case / uniqueness at write | High | Create/Join/Rotate normalize | **Cleared Task 3** — uppercase store + unique generate |
| Cross-company member list / invite leak | High | CompanyAdmin override | **Mitigated Task 3** — ResolveManagedCompanyAsync |
| CompanyAdmin orphan companies | Medium | **Cleared Task 3** | First-join bootstrap assigns CompanyAdmin |
| Upload omits migrations / MatchSync | Critical | Working tree has untracked schema + providers | **Cleared** — packaging closed (`ac0608e` + Phase 13) — [upload gate](changes/whole-project-upload-gate.md) |
| Dual docs out of sync on push | High | VitePress `docs/` vs git `WorldCup-System/docs/` | Mirror both; dual-docs policy |
| Phase 11 Task 7 shows `Tournament Scorer` as real | High | Snapshot / timelines must omit placeholder name | **Cleared by design** — [honesty](changes/phase-11-recent-events-honesty.md) |
| Phase 11 Task 7 Bugbot / unit suite | High | Gate closed | Bugbot clean; honesty **3**; suite **380** |
| Phase 11 Task 6 accidentally changes FT / bets | High | Scorers-only path must skip ApplyScoreIdempotent | **Cleared** — [backfill](changes/phase-11-backfill.md) |
| Phase 11 Task 6 empty timeline Warning noise | High | Backfill should Skip, not count-mismatch | **Cleared** — `skipWhenNoTimelineGoals` |
| Phase 11 Task 6 Bugbot / unit suite | High | Gate closed | **Cleared** — suite **375** |
| Phase 12 Task 2 wipe scope | High | Must not delete WC 2026 matches | Cleared — team-scoped wipe; smoke OK |
| Phase 12 Task 2 redraw without wipe | Medium | Orphan fixtures / team reassignment | Cleared — exit unless `--wipe-matches` |
| Phase 12 Task 4 / formal Bugbot | High (gate) | Unittest **23**; smoke **103**; highs fixed | **Cleared** — [tests-gate](changes/phase-12-tests-gate.md) |
| Phase 11 Task 5 Bugbot / review gate | High | Cleared | No bugs; suite **361** |
| Phase 11 Task 5 wiring unit tests | High | Cleared | **8** wiring cases; suite **361** |
| Phase 11 Task 4 Bugbot / review gate | High | Cleared | Suite **353** |
| Phase 11 Task 4 apply unit tests | High | Cleared | Suite **353** |
| Phase 11 Task 4 not wired into SyncResult | Info | **Cleared** | Task 5 |
| Auto-create players on miss | Medium | New Forward rows during apply/resolve | Accepted; monitor jersey exhaustion |
| Task 3 Bugbot / review gate | High | Cleared | Suite **342** |
| Task 2 Bugbot / review gate | High | Cleared | Suite **310** |
| Task 2 not wired into SyncResult | Info | **Cleared** | Task 5 |
| Task 1 ExternalStageId tests | High | Cleared | Suite **278** |
| Task 1 Bugbot / review gate | High | Cleared | No high/critical |
| Phase 10 Task 4 review/test gate | High | Highs cleared | Jasmine + `dotnet test` from canonical paths |
| Phase 10 Task 3 review/test gate | High | Closed | Jasmine **25** + API **268** |
| Phase 10 Task 2 review/test gate | High | Closed | Jasmine **12** + API **268** |
| Migration rewrite (IdentityLongKeys) | Critical | Removed from working tree | Cleared |
| Sync orientation / MatchStatus | High | Fixed in Phase 10 Task 1 | Cleared |
| Sync deletes Admin goals on score change | Medium | Squad/placeholder scorers when counts differ | Accepted until timeline apply |
| External Google Fonts | Medium | CDN in `index.html` | Fallbacks; monitor CSP |
| Outside-git deletes invisible in `git diff` | Low | Root client + dump never tracked | Documented in [phase-10-cleanup](changes/phase-10-cleanup.md) |
| FIFA calendar/timeline ToS / season ids | Medium | Public JSON; config-driven | Monitor; paid provider fallback |
| Final not yet finished in DB | Low | Kickoff 19 Jul | **Cleared** — Phase 9 Done; MatchId 116 FT recorded |
| Accidental WC 2026 wipe via importer | High | `import_wc2026_finished_matches.py` deletes then reloads | Documented — prefer `add_sf2_and_final.py` |
| Missing `ExternalStageId` blocks timeline URL | Medium | Timeline needs stage | Backfill via SetExternal or SyncResult |

### Per-File Summary

#### `docs/changes/phase-14-gate.md` (NEW) — Phase 14 Task 7


**[p14-gate · docs]**

Formal gate page: Review gate table, Bugbot findings placeholder, secrets check, test consent note, Phase 14 working-tree git status, representative diffs, agent do/don't. Phase 14 stays In progress until gate clears.

#### Docs nav / roadmap / VitePress — Phase 14 Task 7


**[p14-gate · docs]**

Modified: `roadmap.md` — Task 7 still `[ ]`, gate in progress (not Done); `index.md` — Task 7 In progress, Recommended next = gate; `changes-review.md` — latest + mapping + gate + risks + per-file; `phase-14-planned.md` — Task 7 In progress; `.vitepress/config.mts` — sidebar `phase-14-gate`. Dual mirror VitePress ↔ nested git docs.

#### `environment.ts` (MOD) — Phase 14 Task 5


**[p14-spa-prod]**

Production env default: `apiUrl` changed from `http://localhost:5055` to `https://api.REPLACE_ME.example` plus deploy comment (no trailing slash; Dev file untouched). Detail: [phase-14-spa-prod](changes/phase-14-spa-prod.md).

#### `worldcup-client/README.md` (MOD) — Phase 14 Task 5


**[p14-spa-prod]**

Splits Building into Development vs Production: set `apiUrl`, `npm ci` + `npm run build`, serve `dist/worldcup-client/browser/`, link to detail page.

#### `docs/changes/phase-14-spa-prod.md` (NEW) — Phase 14 Task 5


**[p14-spa-prod · docs]**

Full Task 5 detail: Review gate, locked approach, operator steps, pre-deploy checklist, dist path, CORS alignment, verify, related links.

#### Docs nav / roadmap / VitePress — Phase 14 Task 5


**[p14-spa-prod · docs]**

Modified: `roadmap.md` — Task 5 `[x]` Done, next = Task 6 migrate/smoke; `index.md` — Task 5 Done, next migrate/smoke; `changes-review.md` — latest + mapping + gate + per-file; `phase-14-planned.md` — Task 5 Done; `.vitepress/config.mts` — sidebar `phase-14-spa-prod`. Workspace + nested `README.md` Production SPA pointers.

#### `docker-compose.prod.yml` (NEW) — Phase 14 Task 4


**[p14-compose]**

Production override for `docker-compose.yml`. Sets `ASPNETCORE_ENVIRONMENT=Production`; requires distinct `PROD_*` host vars (`PROD_POSTGRES_USER`, `PROD_POSTGRES_PASSWORD`, `PROD_POSTGRES_DB`, `PROD_JWT_SECRET`, `PROD_JWT_VALID_ISSUER`, `PROD_JWT_VALID_AUDIENCE`, `PROD_CORS_ALLOWED_ORIGINS`) via `${VAR:?…}` (Compose fails if unset/empty — Dev `.env` keys alone do not satisfy); clears `DevAdmin__*` / `DevUser__*`; no `ports:` (avoids duplicate Postgres bind). Healthcheck inherited from base (`/health`). Detail: [phase-14-compose](changes/phase-14-compose.md).

#### `.env.example` (MOD) — Phase 14 Task 4 (+ Task 3 CORS / Task 2 header)


**[p14-compose · p14-cors · p14-secrets]**

Header points at secrets / CORS / Compose detail pages and prod run command. Local Dev defaults unchanged. Production commented block + Compose required-var note: do not reuse local weak values with `-f docker-compose.prod.yml`.

#### `README.md` (NEW in nested git) — Phase 14 Task 4


**[p14-compose]**

Nested repo README: Quick start mentions optional Dev Compose and Production override command + link to `docs/changes/phase-14-compose.md`. Workspace root README (outside nested git) has **Production Docker Compose** subsection with the same command and required env list.

#### `docs/changes/phase-14-compose.md` (NEW) — Phase 14 Task 4


**[p14-compose · docs]**

Full Task 4 detail: Review gate, locked Option A, required env table, behavior, git status, diff snippets, verify, related links.

#### Docs nav / roadmap / VitePress — Phase 14 Task 4


**[p14-compose · docs]**

Modified: `roadmap.md` — Task 4 `[x]` Done, next = Task 5 SPA; `index.md` — Task 4 Done, next SPA prod; `changes-review.md` — latest + mapping + gate + per-file; `phase-14-planned.md` — Task 4 Done; `.vitepress/config.mts` — sidebar `phase-14-compose`.

#### `CorsOriginsResolver.cs` (NEW) — Phase 14 Task 3


**[p14-cors]**

New static resolver: policy name `Spa`; default `http://localhost:4200`; reads `Cors:AllowedOrigins` as JSON array children or semicolon/comma string; trims; strips trailing `/`; rejects `*`; case-insensitive distinct. Detail: [phase-14-cors](changes/phase-14-cors.md).

#### `Program.cs` (MOD) — Phase 14 Task 3


**[p14-cors]**

Replaces hardcoded `AngularDev` + `WithOrigins("http://localhost:4200")` with `CorsOriginsResolver.Resolve` + `AddPolicy(PolicyName)` / `UseCors(PolicyName)` before auth. JWT issuer/audience unchanged (already config-driven).

#### `appsettings.json` (MOD) — Phase 14 Task 3


**[p14-cors]**

Adds `"Cors": { "AllowedOrigins": [ "http://localhost:4200" ] }`.

#### `docker-compose.yml` (MOD) — Phase 14 Task 3


**[p14-cors]**

Maps `Cors__AllowedOrigins: ${CORS_ALLOWED_ORIGINS:-http://localhost:4200}` on the API service.

#### `.env.example` (MOD) — Phase 14 Task 3 (+ Task 2 header)


**[p14-cors · p14-secrets]**

Local `CORS_ALLOWED_ORIGINS=http://localhost:4200`; Production commented `Cors__AllowedOrigins` / multi-origin / indexed forms; pointer to `phase-14-cors.md` + secrets inventory. No real secrets.

#### CORS tests (NEW) — Phase 14 Task 3


**[p14-cors]**

`CorsOriginsResolverTests` — default, array, `;`/`,`, slash strip, dedupe, `*` throw, empty entries, `PolicyName`. `CorsIntegrationTests` — allowed Origin reflected; evil Origin not reflected on `/health`.

#### `docs/changes/phase-14-cors.md` (NEW) — Phase 14 Task 3


**[p14-cors · docs]**

Full Task 3 detail: Review gate, behavior, git status, files, diff snippets, verify, operator snippet.

#### Docs nav / roadmap / VitePress — Phase 14 Task 3


**[p14-cors · docs]**

Modified: `roadmap.md` — Task 3 `[x]` Done; `index.md` — Task 3 Done, next = Task 4 Compose; `changes-review.md` — latest + mapping + gate + per-file; `phase-14-planned.md` — Task 3 Done; `phase-14-secrets.md` — CORS implemented pointer; `.vitepress/config.mts` — sidebar `phase-14-cors`.

#### `.env.example` (MOD) — Phase 14 Task 2


**[p14-secrets]**

Rewrote header: SAFE TO COMMIT; never commit real `.env`; pointer to `docs/changes/phase-14-secrets.md`. Local Docker defaults unchanged (`postgres` / weak JWT / `Admin123!` / `User123!`) — marked Development-only. Added commented Production block with `REPLACE_ME_*` for `ASPNETCORE_ENVIRONMENT`, `ConnectionStrings__DefaultConnection`, `JWT__Secret`, `JWT__ValidIssuer`, `JWT__ValidAudience`; notes omit DevAdmin/DevUser in Production; CORS wiring completed in Task 3.

#### `docs/changes/phase-14-secrets.md` (NEW) — Phase 14 Task 2


**[p14-secrets]**

Canonical Production env inventory: Review gate, required boot keys, Dev-only omit list, Compose helper mapping, optional `MatchResultSync`, inventory sources (`Program.cs` / appsettings / Compose), operator checklist. Placeholders only. Detail: [phase-14-secrets](changes/phase-14-secrets.md).

#### Workspace `README.md` (MOD, outside nested git) — Phase 14 Task 2


**[p14-secrets]**

Configuration table extended with Production env names (`JWT__ValidIssuer` / `JWT__ValidAudience`, `ASPNETCORE_ENVIRONMENT`). New **Production secrets inventory** subsection links [`docs/changes/phase-14-secrets.md`](changes/phase-14-secrets.md) and `WorldCup-System/.env.example`; notes Task 3 CORS / Task 4 Compose.

#### Docs nav / roadmap / VitePress — Phase 14 Task 2


**[p14-secrets · docs]**

Modified: `roadmap.md` — Task 2 `[x]` Done; `index.md` — Phase 14 table Task 2 Done, next = Task 3; `changes-review.md` — latest + mapping + this gate; `phase-14-planned.md` — Task 2 Done; `whole-project-upload-gate.md` — historical packaging; `workflow.md` — Phase 14 ask-before note; `.vitepress/config.mts` — sidebar links for `phase-14-secrets` + `phase-14-planned`.

#### Isolation tests (NEW/MOD) — Phase 13 Task 6


**[p13-tests-gate]**

New (untracked): `WorldCup-System.Tests/Integration/LeaderboardIsolationIntegrationTests.cs` — **3** HTTP cases via `WorldCupWebApplicationFactory` (two-company never leak; null company → `[]`; no JWT → 401). Modified: `LeaderboardServiceTests.cs` — `GetLeaderboard_Bidirectional_NeitherCompanyIncludesTheOther` (+ Task 4 company cases). New (untracked): `CompanyServiceTests.cs` includes `CreateAsync_GeneratesDistinctInviteCodes_AndDoesNotReuseExisting` (**16** facts total). Suite **419** passed. Bugbot formal **open**. Detail: [phase-13-tests-gate](changes/phase-13-tests-gate.md).

#### Docs — Phase 13 Task 6 + review


**[p13-tests-gate · docs]**

New (untracked in git): `docs/changes/phase-13-tests-gate.md` (Review gate + acceptance + diff snippets). Modified: `changes-review.md`, `roadmap.md` (Task 6 tests cleared; Bugbot open), `phase-13-planned.md`, `index.md`. Formal Bugbot still required to mark Phase 13 Done.

#### SPA — CompanyApiService + CompanyComponent (NEW) — Phase 13 Task 5


**[p13-spa]**

New (untracked): `worldcup-client/src/app/core/api/company-api.service.ts` (~48 LOC) — Mine / Join / RotateInviteCode / Members / Create / GetAll. New: `company.component.{ts,html,scss}` (~143 / 141 / 95 LOC) — `/company` join form; CompanyAdmin invite + rotate + members; Admin create + GetAll list. New Jasmine: `company-api.service.spec.ts` (4), `auth.service.company.spec.ts` (2). Detail: [phase-13-spa](changes/phase-13-spa.md).

#### SPA — auth / routes / models / shell / board / dashboard — Phase 13 Task 5


**[p13-spa]**

Modified: `auth.service.ts` — `isCompanyAdmin` + `applyAccessToken` (Join JWT → GetMe). Modified: `api.models.ts` — `Company`, `CompanyMine`, `JoinCompanyResponse`, etc. (+39). Modified: `app.routes.ts` — `/company` + `authGuard` on `/leaderboard`. Modified: shell **Company** nav; leaderboard company empty CTA; dashboard join banner / chip; admin hub Companies card. Client diff **+201/−60** (11 modified files) + untracked company feature.

#### Docs — Phase 13 Task 5 + review


**[p13-spa · docs]**

New (untracked): `docs/changes/phase-13-spa.md` (Review gate + behavior + acceptance + diff snippets). Modified: `changes-review.md`, `roadmap.md` (Task 5 Done; interim pending), `phase-13-planned.md`, `index.md`, `api-status.md`, VitePress sidebar. Interim Bugbot/Jasmine **pending**; Task 6 tests already cleared (suite **419**).

#### CompanyController + CompanyService (NEW) — Phase 13 Task 3


**[p13-company-api]**

New: `WorldCup-System/Controllers/CompanyController.cs` — Create (Admin), Join/Mine (JWT), RotateInviteCode/Members (CompanyAdmin/Admin), GetAll (Admin); Join re-issues JWT when `BecameCompanyAdmin`. New: `Core/Services/Companies/ICompanyService.cs` + `CompanyService.cs` — uppercase invite generate/normalize; first joiner → `CompanyAdmin`; `ResolveManagedCompanyAsync` blocks cross-company for CompanyAdmin. New: `Core/DTOs/Companies/CompanyDTO.cs`. Modified: `Program.cs` DI; `ApiErrorHelper` → 403 for `UnauthorizedAccessException`. Detail: [phase-13-company-api](changes/phase-13-company-api.md).

#### Company DTOs (NEW)


**[p13-company-api]**

New: `Core/DTOs/Companies/CompanyDTO.cs` — `CompanyDTO`, `CreateCompanyDTO`, `JoinCompanyDTO`, `RotateInviteCodeDTO`, `CompanyMineDTO`, `CompanyMemberDTO`, `JoinCompanyResultDTO`. Invite optional on DTO (Admin/CompanyAdmin only).

#### ApiErrorHelper — 403 mapping


**[p13-company-api]**

Modified: `WorldCup-System/Controllers/ApiErrorHelper.cs` — `UnauthorizedAccessException` → `ObjectResult` status **403** (CompanyAdmin isolation denials).

#### Program.cs — ICompanyService DI


**[p13-company-api]**

Modified: `WorldCup-System/Program.cs` — `AddScoped<ICompanyService, CompanyService>` (CompanyAdmin role seed + Company startup repair remain Task 2).

#### Docs — Phase 13 Task 3 + review


**[p13-company-api · docs]**

New: `docs/changes/phase-13-company-api.md` (Review gate + endpoints + acceptance). Modified: `changes-review.md`, `roadmap.md` (Task 3 Done), `phase-13-planned.md`, `index.md`, `api-status.md`, VitePress sidebar. Interim Bugbot + docs/tests gate; full phase gate Task 6.

#### ILeaderboardService / LeaderboardService — Phase 13 Task 4


**[p13-leaderboard-scope]**

Modified: `Core/Services/Bets/ILeaderboardService.cs` — `GetLeaderboard(long? companyId, int? worldCupId)`. Modified: `LeaderboardService.cs` — null company → empty; load company members; filter bets by member ids; `GetMySummary` rank uses caller `CompanyId` (`Rank = null` when none). Detail: [phase-13-leaderboard-scope](changes/phase-13-leaderboard-scope.md).

#### BetController.GetLeaderboard — Phase 13 Task 4


**[p13-leaderboard-scope]**

Modified: `WorldCup-System/Controllers/BetController.cs` — `[Authorize]`; resolve user from JWT; pass `user.CompanyId` (never client company id); return `Ok(entries)` / `Unauthorized` / `ApiErrorHelper`. Breaking vs Phase 4 public endpoint.

#### LeaderboardServiceTests / BetControllerTests — Phase 13 Task 4


**[p13-leaderboard-scope]**

Modified: `WorldCup-System.Tests/Services/Bets/LeaderboardServiceTests.cs` — null company, within-company ranking, cross-company exclusion, null-company summary, worldCup filter. Modified: `BetControllerTests.cs` — authenticated passes `CompanyId`; null company passes `null`. **+200/−30** on Task 4 code/test slice.

#### Docs — Phase 13 Task 4 + review


**[p13-leaderboard-scope · docs]**

New: `docs/changes/phase-13-leaderboard-scope.md` (Review gate + full diff). Modified: `changes-review.md`, `roadmap.md` (Task 4 Done), `phase-13-planned.md`, `index.md`, `api-status.md`, `overview.md`, VitePress sidebar. Interim Bugbot + docs gate pending; full phase gate Task 6.

#### Company entity (NEW) — Phase 13 Task 2


**[p13-company-model]**

New: `Data/Entities/Company.cs` — workplace pool (`Name`, optional `Slug`, unique `InviteCode`, `CreatedAt`, optional `CreatedByUserId` audit FK, `Members`). Soft multi-tenancy; tournament data stays shared. Detail: [phase-13-company-model](changes/phase-13-company-model.md).

#### User — CompanyId FK


**[p13-company-model]**

Modified: `Data/Entities/User.cs` — nullable `CompanyId` + nav `Company`. Null = not in a workplace pool (browse/bet OK). One company per user in v1.

#### ApplicationDbContext — Company Fluent config


**[p13-company-model]**

Modified: `Data/Context/ApplicationDbContext.cs` — `DbSet<Company>`; max lengths + check constraints; unique `Uq_Company_InviteCode`; filtered unique `Uq_Company_Slug`; `FK_Company_CreatedByUser` / `FK_User_Company` (`ClientSetNull`); `IX_AspNetUsers_CompanyId`.

#### Migration AddCompany + snapshot


**[p13-company-model]**

New: `Data/Migrations/20260723120000_AddCompany.cs` — create `Company`, indexes, add `AspNetUsers.CompanyId` + FK. Modified: `ApplicationDbContextModelSnapshot.cs` — Company entity + User FK + `Members` navigation. **Must include in upload commit.**

#### RepositoryManager — Company repo


**[p13-company-model]**

Modified: `Data/Repos/IRepositoryManager.cs` + `RepositoryManager.cs` — lazy `IRepository<Company> Company`.

#### Program.cs — CompanyAdmin + startup repair


**[p13-company-model]**

Modified: `WorldCup-System/Program.cs` — seed roles `Admin` / `User` / `CompanyAdmin`; IF NOT EXISTS `CREATE TABLE "Company"` + unique indexes + `AspNetUsers.CompanyId` + `FK_User_Company` (duplicate_object safe).

#### Docs — Phase 13 Task 2 + review


**[p13-company-model · docs]**

New: `docs/changes/phase-13-company-model.md` (Review gate + deliverables). Modified: `changes-review.md`, `roadmap.md` (Task 2 Done), `phase-13-planned.md`, `index.md`, `data-model.md`, `overview.md`, VitePress sidebar link. Interim Bugbot + docs gate; full phase gate Task 6.

#### Whole-project upload gate


**[upload-packaging]**

New: `docs/changes/whole-project-upload-gate.md` — inventory of uncommitted Phases 9–12 + Phase 10 leftovers + Phase 13 plan; critical must-include migrations/MatchSync/scripts (**now also `AddCompany`**); dual-docs mirror checklist. Product Bugbot highs cleared for 9–12; packaging open until commit. Detail: [whole-project-upload-gate](changes/whole-project-upload-gate.md).

#### Multi-cup SPA context (Phase 12 companion)


**[p12-draw · spa]**

Modified: `worldcup-context.service.ts` — sort cups by year; persist `worldcup.selectedId` in localStorage; resolve stored / current / preferred 2026 then first. Selector + fixtures / standings / dashboard / bracket / leaderboard / admin hub·schedule·live reload against selected cup. Supports WC 2026 + WC 2030 side by side after seed.

#### Phase 9 — Final FT ops close-out


**[p9-final-ft · p9-ops]**

Modified (uncommitted): `scripts/add_sf2_and_final.py` — idempotent `apply_final_ft` (insert 0–1 or correct scorer to Ferran Torres @ 106'); WC 2026–scoped teams; squad Forward preference; no wipe. Modified: `scripts/import_wc2026_finished_matches.py` — `FINAL = 5`; Final `MATCHES` row Argentina 0–1 Spain; `wipe_world_cup_matches` (2026-only); prefer upsert script for named Torres. Local DB: MatchId **116**; GoalId **298** Morata → Torres; bets **2/2**. No C# / migration. Commits already Done: `dcb322b`, `7886b40`. **Gate cleared** — [phase-9-ops-closeout](changes/phase-9-ops-closeout.md).

#### Recent events honesty — Task 7


**[p11-recent-events-honesty]**

Modified (uncommitted): `Core/Services/Matches/MatchService.cs` — `ToHonestPlayerName` on `GetLiveSnapshot` recent events + `BuildTeamStatsDto` goal/card `PlayerName`; reuses `MatchResultSyncService.PlaceholderScorerName`. New (untracked): `worldcup-client/src/app/core/utils/placeholder-scorer.ts` — `PLACEHOLDER_SCORER_NAME` / `toHonestPlayerName`. Modified: `fixtures.component.ts` — map snapshot `recentEvents` through sanitizer; `admin-live-console.component.ts` — timeline labels omit placeholder; picker uses shared constant (+ jersey 99 for picker only). Name-only display rule (not jersey 99 alone). **Gate closed** — Bugbot clean; honesty **3**; suite **380**. Detail: [phase-11-recent-events-honesty](changes/phase-11-recent-events-honesty.md).

#### Scorers-only backfill — Task 6


**[p11-backfill]**

Modified (uncommitted): `IMatchResultSyncService` + `MatchResultSyncService` — `SyncScorers` / `SyncScorersForWorldCup`; calendar finished + `OrientScoresToLocalSides` + stage persist only; **no** `ApplyScoreIdempotent` / bets / knockout; `TryApplyTimelineScorersForBackfillAsync` (`skipWhenNoTimelineGoals: true` → empty Goal! list = `Skipped`); `Applied=true` when `ScorerStatus` is `Applied` or `AlreadyMatched`; `ScoreChanged=false`; `BetsResolved=0`; batch catch sets `ScorerStatus=Warning` (unlike FT batch); `BatchDelayMilliseconds` between matches. `MatchController` — Admin `POST SyncScorers/{matchId}`, `POST SyncScorersForWorldCup?worldCupId=`. Client — `match-api.service.ts` `syncScorers` / `syncScorersForWorldCup`. Untracked: `scripts/sync_scorers_backfill.ps1`. **Gate closed** — Bugbot highs fixed; backfill **14**; suite **375**. Detail: [phase-11-backfill](changes/phase-11-backfill.md).

#### WC 2030 smoke + review gate — Phase 12 Task 4


**[p12-tests-gate]**

Formal phase close for Tasks 1–3: unittest **23** OK; smoke `--seed 2030 --wipe-matches` → **103** matches; `demo_kickoff_anchors` (group Finished / KO Scheduled); `EnsureSameWorldCup` on Team assign/update; Bugbot highs cleared; medium H2H accepted. Detail: [phase-12-tests-gate](changes/phase-12-tests-gate.md).

#### WC 2030 group simulation — Phase 12 Task 2


**[p12-group-sim]**

Extended `scripts/simulate_wc2030.py`: `build_group_stage_schedule` (72 fixtures, 3 non-overlapping matchdays from 2030-06-13), Poisson λ=1.35 (cap 7), `simulate_group_stage` inserts Match + TeamStats + Goal, Tournament Scorer for empty squads, stadium cycle, `--wipe-matches` / `--skip-group-sim`, block redraw when 2030 matches exist. Tests: `test_simulate_wc2030_draw.py` **14** cases. No C# / migrations. Detail: [phase-12-group-sim](changes/phase-12-group-sim.md).

#### WC 2030 draw — Phase 12 Task 1


**[p12-draw]**

Cup ensure, hosts+random 42, groups A–L; multi-cup `Team.CountryId`. Detail: [phase-12-draw](changes/phase-12-draw.md).

#### Sync wire — Task 5


**[p11-sync-wire]**

Modified (uncommitted): `MatchResultSyncService` — inject `IExternalMatchEventsProvider` + `ITimelineScorerApplyService` + `IOptions<MatchResultSyncOptions>`; after FT + bets/advance call fail-soft `TryApplyTimelineScorersAsync`; merge scorer warning into `Warning`; batch `ScorersApplied` / `ScorerWarnings` + `BatchDelayMilliseconds` between matches. `MatchResultSyncDTO` — `SyncScorerStatuses`, `ScorerStatus` / `ScorerGoalsUpdated` / `ScorerMessage`, batch counts. `MatchResultSyncOptions` + `appsettings.json` — `BatchDelayMilliseconds` (default 250). Tests — constructor Moqs + **8** wiring cases (`BatchDelayMilliseconds = 0`). Client — `api.models.ts` `scorerStatus` / `scorerGoalsUpdated` / `scorerMessage` / `scorersApplied` / `scorerWarnings`. Calendar FT still succeeds when timeline/apply throws (`ScorerStatus=Warning`). Missing `ExternalStageId` → `Skipped`. **Gate closed** — Bugbot clean; suite **361**. Detail: [phase-11-sync-wire](changes/phase-11-sync-wire.md).

#### Real-scorer apply — Task 4


**[p11-scorer-apply]**

New: `ITimelineScorerApplyService`, `TimelineScorerApplyService`, `TimelineScorerApplyResult`, `TimelineScorerApplyServiceTests` (10 cases). Modified: `Program.cs` scoped DI. Orient timeline home/away via `homeMapsToTeamOne`; compare per-side counts; mismatch → throw (no Goal writes); align → in-place `PlayerId` / `TimeScored` / `IsOwnGoal`; own goal → resolve on conceding team; `SaveAsync` only when changed; no bet/knockout. Wired by Task 5 SyncResult. Detail: [phase-11-scorer-apply](changes/phase-11-scorer-apply.md).

#### Player identity resolver — Task 3


**[p11-player-resolve]**

New: `ITimelinePlayerResolver`, `TimelinePlayerResolver`, `TimelinePlayerResolveResult`, `TimelinePlayerMatchMethod`, migration `20260720140000_AddPlayerExternalPlayerId` (+ Designer). Modified: `Player.ExternalPlayerId`; `ApplicationDbContext` unique filtered index; model snapshot; `Program.cs` scoped DI + `IF NOT EXISTS` column/index; `PlayerDTO` + `PlayerService` map; client `api.models.ts` `externalPlayerId`. Resolve: ExternalPlayerId → exact name → normalized → create Forward (jersey 1–98). Ambiguous / cross-team id → throw. Used by Task 4 apply / Task 5 SyncResult. Detail: [phase-11-player-resolve](changes/phase-11-player-resolve.md).

#### FIFA timeline provider — Task 2


**[p11-timeline-provider]**

New: `FifaTimelineEventsProvider`, `IExternalMatchEventsProvider`, `ExternalMatchEvents`, `ExternalMatchGoalEvent`, `FifaTimelineEventsProviderTests`. Modified: `MatchResultSyncOptions.TimelineBaseUrl`, `appsettings.json`, `Program.cs` HttpClient registration. Parses Goal! / Own Goal / Penalty Goal; minute + display name helpers; throws on HTTP/empty/missing ids (Task 5 soft-fail). Detail: [phase-11-timeline-provider](changes/phase-11-timeline-provider.md).

#### External stage id — Task 1


**[p11-external-stage]**

`Match.ExternalStageId` (varchar 64 nullable); migration `20260720120000_AddMatchExternalStageId`; Program.cs IF NOT EXISTS; `SetExternalMatchIdDTO` optional stage; calendar `IdStage` → `ExternalMatchResult`; `PersistExternalStageIdIfNeededAsync` on SyncResult; MatchDTO + client `externalStageId`; options document timeline path reuse of IdCompetition/IdSeason. Detail: [phase-11-external-stage](changes/phase-11-external-stage.md).

#### MatchSync — squad scorer preference (same Task 1 tree)


**[p11-external-stage · related]**

When FT goal counts change, prefer seeded forwards / non-placeholder squad (cycle by number) before `"Tournament Scorer"`. Tests: placeholder-only, midfielder fallback, cycle. Not real FIFA timeline scorers.

#### Admin live console — hide placeholders


**[related]**

`playersForTeam` filters `Tournament Scorer` / number `99` from goal/card pickers.

#### Project cleanup — Phase 10 Task 4


**[p10-cleanup]**

Workspace deletes **outside git**: root `worldcup-client/`, `TestDatabase_backup_*.dump`. Empty untracked dirs removed under admin. Docs in git: `phase-10-cleanup.md` (new), roadmap/index/changes-review/planned, VitePress sidebar. Dual docs kept. Detail: [phase-10-cleanup](changes/phase-10-cleanup.md).

#### WC styling (theme + shell + home + accents) — Task 3


**[p10-wc-styling]**

Client theme: `styles.scss` black/gold tokens + night-pitch atmosphere + motions; `index.html` Bebas Neue / Manrope; shell glass topbar + shimmer brand; home brand-first hero (no cards); fixtures/leaderboard/auth/dashboard/bracket/bets gold + display typography. Detail: [phase-10-styling](changes/phase-10-styling.md).

#### Fixtures UX (NEW helpers + component) — Action / chips / sections


**[p10-fixtures-ux]**

`fixture-sections.ts` (+ Jasmine spec): group, filter, next bettable. Fixtures component: default Action, toolbar chips, Open → Live → Finished, collapsible finished, jump CTA, live poll clears `canBet`. Detail: [phase-10-fixtures-ux](changes/phase-10-fixtures-ux.md).

#### MatchSync (NEW) — FIFA provider + apply + resolve


**[p10-match-sync]**

`FifaCalendarMatchResultProvider`, `MatchResultSyncService`, options, DTOs; Admin SetExternal / SyncResult / SyncFinishedResults; client models + match-api methods. Detail: [phase-10-match-sync](changes/phase-10-match-sync.md).

#### Match entity / migration — ExternalMatchId


**[p10-match-sync]**

Nullable unique `ExternalMatchId`; migration `20260717120000_AddMatchExternalMatchId`.

#### BetService / BetController / BetDTO


**[p8-bet-auto-resolve · p8-admin-leaderboard]**

Resolve returns per-user +3/0 breakdown; world-cup bulk resolve; TryAuto helper for finished matches. Sync reuses `ResolveBetsForMatch`.

#### MatchService / MatchController — stage + GetLiveSnapshot + sync


**[p8-live-snapshot · p7-knockout · p10-match-sync · p11-external-stage]**

Stage on create/update; nullable teams + feeders in DTOs; batch live snapshot; Admin post-match sync actions; `ExternalStageId` on list/detail DTOs.

#### Knockout (NEW) + GoalService advance


**[p7-knockout]**

Generate/get bracket; advance winners into feeder slots; AddGoal/DeleteGoal call `TryResolveAndAdvance`. Sync also calls `TryAdvanceFromMatch`.

#### StandingsService — H2H


**[p7-knockout]**

FIFA-style head-to-head among tied clusters after points / GD / GF.

#### SPA — dashboard / admin hub / live / bracket (NEW)


**[p8-user-dashboard · p8-admin-hub · p8-admin-live-console · p7-knockout]**

Untracked feature folders + write/knockout API services; routes in `app.routes.ts`.

#### Guards · login · shell · admin-teams


**[p8-login-return · p8-admin-team-import]**

`returnUrl` restore; role landing; UpdateTeam create/edit UI; Bracket nav link.

#### Fixtures + admin schedule · Program.cs


**[p8-live-snapshot · p8-admin-leaderboard · p7-knockout · p10-match-sync · p10-fixtures-ux · p11-external-stage · p11-player-resolve · p11-scorer-apply · p11-sync-wire]**

30s snapshot poll (+ fixtures Task 2 `canBet` clear); resolve breakdown; live console links; Knockout DI + Feeder* startup repair SQL; MatchResultSync calendar + timeline HttpClient registration; `ITimelinePlayerResolver` + `ITimelineScorerApplyService` DI; SyncResult fail-soft timeline apply (Task 5); ExternalStageId + ExternalPlayerId IF NOT EXISTS; fixtures sectioned UI.

#### Scripts + tests + docs


**[coverage · ops · p9-final-ft · p11 · p12 · upload]**

WC 2026 group/results import scripts (through Final; prefer `add_sf2_and_final.py` for Final-only); Phase 12 `simulate_wc2030.py` + `test_simulate_wc2030_draw.py` (**23**); optional `sync_scorers_backfill.ps1`, `seed_wc2026_players.py`, `scripts/data/wc2026_players.csv`. Unit tests: MatchSync timeline/resolve/apply/wire/backfill + TeamService multi-cup (suite **382**). Docs: Phase 9–13 change pages + [whole-project-upload-gate](changes/whole-project-upload-gate.md); VitePress sidebar updated.

### Prior Phases — Committed

| Phase | Status | Detail page |
| --- | --- | --- |
| Phase 1 — Foundation | Committed | [Phase 1 detail](changes/phase-1-identity.md) |
| Phase 2 — Tournament setup | Committed | [Phase 2 detail](changes/phase-2-tournament-setup.md) |
| Phase 3 — Match lifecycle | Committed | [Phase 3 detail](changes/phase-3-match-lifecycle.md) |
| Phase 4 — Betting | Committed | [Phase 4 detail](changes/phase-4-betting.md) |
| Phase 5 — Frontend SPA | Committed | [Phase 5 detail](changes/phase-5-frontend-spa.md) |
| Phase 6 — Quality & Operations | Committed | [Phase 6 detail](changes/phase-6-quality-ops.md) |
| Phase 7 — Bugfixes & Live Data | **Done** · committed (`dcb322b` / `7886b40`) | [Phase 7 detail](changes/phase-7-bugfixes.md) |
| Phase 8 — User & Admin Dashboards | **Done** · committed (`7886b40`) | [Phase 8 detail](changes/phase-8-dashboards.md) |
| Phase 9 — Ops & Tournament Close-Out | **Done** (Final FT 21 Jul) | [Phase 9 detail](changes/phase-9-ops-closeout.md) |
| Phase 10 — Match Sync, Fixtures UX, Styling & Cleanup | **Done** (Tasks 1–4; cleanup 20 Jul) | [sync](changes/phase-10-match-sync.md) · [fixtures UX](changes/phase-10-fixtures-ux.md) · [styling](changes/phase-10-styling.md) · [cleanup](changes/phase-10-cleanup.md) · [plan](changes/phase-10-planned.md) |
| Phase 11 — Real Goalscorers (FIFA Timeline) | **Done** | [Task 8 gate](changes/phase-11-tests-gate.md) · [Task 1](changes/phase-11-external-stage.md) · [Task 2](changes/phase-11-timeline-provider.md) · [Task 3](changes/phase-11-player-resolve.md) · [Task 4](changes/phase-11-scorer-apply.md) · [Task 5](changes/phase-11-sync-wire.md) · [Task 6](changes/phase-11-backfill.md) · [Task 7](changes/phase-11-recent-events-honesty.md) · [plan](changes/phase-11-planned.md) |
| Phase 12 — 2030 World Cup Simulation | **Done** | [plan](changes/phase-12-planned.md) · [draw](changes/phase-12-draw.md) · [group sim](changes/phase-12-group-sim.md) · [bracket](changes/phase-12-bracket.md) · [tests gate](changes/phase-12-tests-gate.md) |
| Phase 13 — Company-Scoped Competitions | **Done** (suite **438**) | [plan](changes/phase-13-planned.md) · [tests gate](changes/phase-13-tests-gate.md) · [SPA](changes/phase-13-spa.md) · [company API](changes/phase-13-company-api.md) |
| Phase 14 — Deploy Readiness | **In progress** (Tasks 1–6 Done; next Task 7 gate) | [plan](changes/phase-14-planned.md) · [secrets](changes/phase-14-secrets.md) · [CORS](changes/phase-14-cors.md) · [Compose](changes/phase-14-compose.md) · [SPA prod](changes/phase-14-spa-prod.md) · [migrate/smoke](changes/phase-14-migrate-smoke.md) |

### Suggested Next Steps

#### Phase 14 — Deploy readiness

**Task 6 Done** (migrate + smoke checklist) — [phase-14-migrate-smoke](changes/phase-14-migrate-smoke.md). **Next:** Task 7 gate (`p14-gate`) — [phase-14-planned](changes/phase-14-planned.md) · [Roadmap Phase 14](roadmap.md#phase-14).

#### Upload packaging (historical)

Phases 9–12 packaging committed (`ac0608e`); Phase 13 on origin (`5fdd1c4`). Phase 14 Tasks 2–6 still uncommitted locally — commit only when asked — [whole-project-upload-gate](changes/whole-project-upload-gate.md).

#### Phase 13 — Company-scoped competitions

**Done** (suite **438**; Bugbot clean). Historical: [phase-13-tests-gate](changes/phase-13-tests-gate.md) · [phase-13-spa](changes/phase-13-spa.md).

#### Maintenance / polish

Phases **1–13** Done. Prefer ops hygiene (do not re-run the WC 2026 wipe importer unless a full reload is intended) — [Phase 9 detail](changes/phase-9-ops-closeout.md) · [roadmap](roadmap.md).
