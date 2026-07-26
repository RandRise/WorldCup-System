# WorldCup System — Completion Roadmap

## Docs navigation

[Dashboard](index.md) | [System Overview](overview.md) | [Completion Roadmap](roadmap.md) | [Dev Workflow](workflow.md) | [API Status](api-status.md) | [Data Model](data-model.md) | [Changes Review](changes-review.md)

---
## Completion Roadmap

Thirteen phases. Checkboxes below reflect a **code audit as of 21 Jul 2026** — checkboxes use Markdown task lists (`[x]` / `[ ]`).

> **Success:** **Where you are:** Phases **1–13 complete.** Phase 13 gate closed (Bugbot clean; suite **438**). **Upload packaging open** until commit — [whole-project-upload-gate](changes/whole-project-upload-gate.md). **Phase 12 Done** (unittest **23** / smoke **103**). **Phase 11 Done** (suite **382**). See [phase-13 SPA](changes/phase-13-spa.md) · [phase-13 tests gate](changes/phase-13-tests-gate.md) · [Changes Review](changes-review.md).

<a id="phase-1"></a>

### Phase 1 — Foundation & Auth Hardening **[Done]**

Core infrastructure — Identity, auth, CORS, secrets, cleanup.

- [x] Fix Identity integration — align `User : IdentityUser<long>` with controllers and role seeding — *Program.cs, UserController.cs, User.cs*
- [x] Implement `Repository.GetByIdAsync` (remove NotImplementedException) — *Data/Repos/Repository.cs*
- [x] Add `[Authorize]` and role policies to mutating endpoints — *All controllers*
- [x] Configure CORS for `http://localhost:4200` — *Program.cs — AngularDev*
- [x] Resolve `RequireConfirmedAccount` — disable or add email confirmation — *Program.cs — set to false*
- [x] Remove `WeatherForecastController` and fix `CityService.Cities` stub — *Controllers/, Core/Services/Cities/*
- [x] Move secrets to User Secrets / environment variables — *UserSecretsId; no secrets in appsettings.json*
- [x] Add root README with setup, migrations, and API overview — *README.md at workspace root*
- [x] Add missing IDs to list DTOs (City, Stadium, Group, WorldCup) — *Core/DTOs/*

<a id="phase-2"></a>

### Phase 2 — Tournament Setup APIs **[Done]**

Teams, coaches, players, validation. Bulk stadium CSV cancelled.

- [x] Extend RepositoryManager with Team, Coach, Player, PlayerPosition repos — *Data/Repos/RepositoryManager.cs*
- [x] Create Team DTOs and TeamService — *Core/DTOs/, Core/Services/Teams/*
- [x] Build TeamController — CRUD + assign to group — *Controllers/TeamController.cs — GetTeams returns all; client filters by WC*
- [x] Build CoachService + CoachController (linked to team) — *Core/Services/Coaches/, Controllers/*
- [x] Seed PlayerPosition lookup + list endpoint — *GK/DEF/MID/FWD seeded; PlayerPositionController*
- [x] Build PlayerService + PlayerController — squad management — *GetPlayersByTeam*
- [x] ~~Add bulk stadium import from CSV~~ — cancelled; stadiums via `AddStadium` — *Cancelled*
- [x] Add input validation and consistent error responses — *DataAnnotations + ApiErrorHelper*

<a id="phase-3"></a>

### Phase 3 — Match Lifecycle **[Done]**

Scheduling, goals, cards, stats, standings, fixtures, and knockout bracket progression.

- [x] MatchService + MatchController — create/update schedule (teams, stadium, date) — *MatchStage enum supports Group → Final scheduling*
- [x] Record goals — scorer, minute, own-goal flag — *GoalController / GoalService*
- [x] Record yellow/red cards — *CardController / CardService*
- [x] TeamStats updates — possession, shots, points per match — *TeamStatsController*
- [x] Group standings calculation from match results — *StandingController — group stage only*
- [x] Public fixtures endpoint — list by WorldCup, group, date — *+ GetLiveSnapshot*
- [x] Knockout bracket progression (auto-advance winners, bracket UI) — *KnockoutService + /bracket + admin GenerateBracket*

<a id="phase-4"></a>

### Phase 4 — Betting Module **[Done]**

Predictions, scoring (3/0), leaderboard, auto-resolve.

- [x] Place bet endpoint — authenticated user predicts match outcome — *BetController.PlaceBet*
- [x] View user's bets and active predictions — *GetMyBets, GetMyActiveBets, per-WC / per-match*
- [x] Resolve bets after match completion → BetResult points — *Auto-resolve from GoalService; Admin force resolve*
- [x] Leaderboard / user rankings endpoint — *LeaderboardService*
- [x] Define and document betting scoring rules — *BetScoringRules + GetScoringRules API*

<a id="phase-5"></a>

### Phase 5 — Frontend SPA **[Done]**

Angular app at `WorldCup-System/worldcup-client` (port 4200). Reference admin UI and CSV seed cancelled.

- [x] Scaffold Angular project (`worldcup-client`) — *Port 4200 — solution client under WorldCup-System/*
- [x] Auth module — login, JWT interceptor, route guards — *+ register, adminGuard*
- [x] ~~Admin: countries, cities, stadiums management UI~~ — cancelled; list APIs for dropdowns — *Cancelled*
- [x] Admin: teams, coaches, squad management — */admin/teams*
- [x] Admin: match scheduling UI — */admin/schedule*
- [x] Public: fixtures, live scores, group standings — */fixtures, /standings — 30s live poll*
- [x] Betting UI and leaderboard page — */bets, /leaderboard*
- [x] ~~Wire CSV seed data for demo tournament~~ — cancelled — *Cancelled*

<a id="phase-6"></a>

### Phase 6 — Quality & Operations **[Done]**

Unit/integration tests, Docker, health, Serilog, GitHub Actions CI. Demo seed script cancelled.

- [x] Unit tests for Core services — *WorldCup-System.Tests/Services/*
- [x] Integration tests for API controllers — *WebApplicationFactory + controller tests*
- [x] Docker Compose — API + PostgreSQL — *docker-compose.yml, Dockerfile*
- [x] Health checks and structured logging — */health + Serilog JSON console*
- [x] CI pipeline — build, test, lint — *.github/workflows/ci.yml*
- [x] Offline SQL/Python fixtures for WC 2026 groups and finished matches — *scripts/ under solution — not startup auto-seed*

<a id="phase-7"></a>

### Phase 7 — Post-Launch Bugfixes & Live Data **[Done]**

Auth/session, live refresh, knockout bracket, and offline WC 2026 groups + finished schedule through both semi-finals done. External football API cancelled — manual import + Admin live console. See [Phase 7 log](changes/phase-7-bugfixes.md) · [WC 2026 data](changes/wc2026-live-data.md).

- [x] Fix login session — JWT `sub` claim + `GetMe` resolves user correctly — *CurrentUserResolver, MapInboundClaims*
- [x] Fix My Bets redirect to login (same auth root cause) — *authGuard + GetMe*
- [x] Default to FIFA World Cup 2026 (June 2026) not 2022 or duplicate year — *worldcup-context.service.ts*
- [x] Offline WC 2026 data — groups A–L (48 teams) + finished fixtures/scores through both SFs — *scripts/seed-wc2026-groups.sql · import_wc2026_finished_matches.py · RoundOf32 · add_sf2_and_final.py for Final schedule*
- [x] Auto-refresh fixtures while matches are live — *fixtures — 30s polling + GetLiveSnapshot*
- [x] Auto-resolve bets when matches finish; show active bettors on leaderboard — *GoalService, LeaderboardService*
- [x] ~~External football API~~ — cancelled (manual import) — *Cancelled — offline scripts + Admin live console*
- [x] Knockout bracket fixtures and standings tiebreakers — *Feeder auto-advance + H2H standings + bracket UI*

<a id="phase-8"></a>

### Phase 8 — User & Admin Dashboards **[Done]**

User/admin hubs, live console, bet resolve hardening. See [Changes Review](changes-review.md) · [Phase 8 detail](changes/phase-8-dashboards.md).

- [x] Dev seed Admin + User test accounts for both dashboards — *Program.cs DevAdmin / DevUser*
- [x] Login return URL — My Bets / deep links restore after login — *auth.guard + login queryParams*
- [x] User dashboard — profile summary, open bets, points rank, quick links — */dashboard*
- [x] Bet resolution hardening — auto-resolve at FT; re-resolve after DeleteGoal — *Per-user 3/0 breakdown; bulk resolve*
- [x] Admin: resolve finished matches / refresh leaderboard in UI — *Schedule + hub bulk resolve*
- [x] Live snapshot API — batch live/finished scores + recent events — *GET Match/GetLiveSnapshot*
- [x] Live action write path — admin records goals/cards/stats from one surface — *goal-api / card-api / team-stats-api*
- [x] Admin live console — record goals/cards/stats + event timeline — */admin/live/{matchId}*
- [x] Admin: manual team entry into groups (no CSV) — *Admin Teams UI*
- [x] Admin dashboard hub — ops cards linking teams, schedule, live, resolve — */admin*

<a id="phase-9"></a>
<a id="whats-left"></a>

### Phase 9 — Ops Hardening & Tournament Close-Out **[Done]**

Ship the Phase 7–8 working tree, keep WC 2026 data current through the Final, and clear accepted knockout debt. Product phases 1–8 are done; this phase is ops / polish. Detail: [phase-9-ops-closeout](changes/phase-9-ops-closeout.md).

- [x] Schedule Final fixture (Argentina vs Spain, 19 Jul) for betting — *`scripts/add_sf2_and_final.py` — MatchId 116 present*
- [x] Record Final result after FT — *Argentina 0–1 Spain a.e.t. (Ferran Torres 106'); `add_sf2_and_final.py` apply + import MATCHES; bets resolved*
- [x] Optional ThirdPlace match — *Skipped for this tournament path*
- [x] Apply `AddMatchStage` migration + Feeder* repair on API start — *`MigrateAsync` + IF NOT EXISTS in `Program.cs`; Feeder* columns confirmed*
- [x] Commit Phase 7–8 working tree — *`dcb322b` (sync/knockout) + `7886b40` (SPA/scripts/standings)*
- [x] Knockout re-advance UX when destination already has events — *Goal Add/Delete returns Warning; Admin Schedule Advance winner*

<a id="phase-10"></a>

### Phase 10 — Match Sync, Fixtures UX, Visual Polish & Cleanup **[Done]**

Post-match result sync + bet resolve, fixtures betting priority UX, World Cup–fitting visual polish, and workspace cleanup. **Tasks 1–4 complete.** Detail: [phase-10-match-sync](changes/phase-10-match-sync.md) · [phase-10-fixtures-ux](changes/phase-10-fixtures-ux.md) · [phase-10-styling](changes/phase-10-styling.md) · [phase-10-cleanup](changes/phase-10-cleanup.md) · [phase-10-planned](changes/phase-10-planned.md).

**Task 1 — Post-match sync (done)**

- [x] Post-match external result sync API — fetch FT/result feed (FIFA calendar JSON), apply to match, then resolve bets — *Admin `SyncResult` / `SyncFinishedResults`; reuse `ResolveBetsForMatch`*
- [x] Decide messaging: **v1 without RabbitMQ** (in-process Admin/API flow) — *Documented + followed; no broker wired*
- [x] `ExternalMatchId` mapping + unique filtered index + Admin `SetExternalMatchId` — *migration `AddMatchExternalMatchId`*
- [x] FIFA calendar provider — explicit `MatchStatus`, home/away orientation + name aliases — *`FifaCalendarMatchResultProvider`*
- [x] Idempotent score apply (placeholder scorers only when counts change) + knockout advance — *`MatchResultSyncService`*
- [x] Client sync models + `match-api` methods — *`api.models.ts`, `match-api.service.ts`*
- [x] Unit tests + review gate — *19 MatchSync/controller tests; suite **268** passed (17 Jul 2026)*

**Task 2 — Fixtures UX (done)**

- [x] Fixtures page UX — surface open/live bettable matches first (sections + filter chips); finished history below or filtered — *Action default; Jasmine **12**; [detail](changes/phase-10-fixtures-ux.md)*

**Task 3 — Visual polish (done)**

- [x] Professional World Cup styling — black/white/gold atmosphere, stadium-fit backgrounds, stronger typography across SPA — *theme tokens + public pages; [detail](changes/phase-10-styling.md); Jasmine **25** + API **268***

**Task 4 — cleanup (done)**

- [x] Project cleanup scan — remove duplicates, dead code, and assets we will never use — *removed stale root `worldcup-client` + orphan dump; dual docs kept as VitePress + git mirror; [detail](changes/phase-10-cleanup.md)*

<a id="phase-11"></a>

### Phase 11 — Real Goalscorers from FIFA Timeline **[Done]**

Post-match import of real Goal! events from FIFA’s timeline API so Recent events and match timelines show true scorers (not round-robin squad forwards / `"Tournament Scorer"`). Extends Phase 10 score-only sync. **Tasks 1–8 done** (gate closed; Bugbot highs fixed; suite **382**). Detail: [phase-11-tests-gate](changes/phase-11-tests-gate.md) · [phase-11-planned](changes/phase-11-planned.md) · [Task 1](changes/phase-11-external-stage.md) · [Task 2](changes/phase-11-timeline-provider.md) · [Task 3](changes/phase-11-player-resolve.md) · [Task 4](changes/phase-11-scorer-apply.md) · [Task 5](changes/phase-11-sync-wire.md) · [Task 6](changes/phase-11-backfill.md) · [Task 7](changes/phase-11-recent-events-honesty.md).

**Task 1 — External stage id (`p11-external-stage`) (done)**

- [x] Persist or resolve FIFA `IdStage` for timeline URLs — *column `ExternalStageId`; calendar parse + auto-persist on sync; `SetExternalMatchId` accepts optional stage; migration `AddMatchExternalStageId`*

**Task 2 — Timeline provider (`p11-timeline-provider`) (done)**

- [x] FIFA timeline HTTP provider — parse Goal! (and own-goal / penalty as decided) → minute, team, player name/id — *`FifaTimelineEventsProvider` + `TimelineBaseUrl`; Goal!/Own Goal/Penalty Goal*

**Task 3 — Player identity (`p11-player-resolve`) (done)**

- [x] Resolve FIFA player → local `Player` — *`TimelinePlayerResolver`; `ExternalPlayerId` column; exact → normalized → create; no Tournament Scorer when real name exists* — *[detail](changes/phase-11-player-resolve.md)*

**Task 4 — Scorer apply (`p11-scorer-apply`) (done)**

- [x] Idempotent Goal rewrite when timeline counts align with FT — *`TimelineScorerApplyService`; update PlayerId + TimeScored + IsOwnGoal; count mismatch throws; no bets/knockout* — *[detail](changes/phase-11-scorer-apply.md)*

**Task 5 — Wire sync APIs (`p11-sync-wire`) (done; gate closed)**

- [x] Call timeline + apply from `SyncResult` / `SyncFinishedResults` — *scorer outcome in sync DTO/message; batch continues on per-match warnings; `BatchDelayMilliseconds`; Bugbot clean; **8** wiring + suite **361*** — *[detail](changes/phase-11-sync-wire.md)*

**Task 6 — Backfill (`p11-backfill`) (done; gate closed)**

- [x] Admin scorers-only sync for finished mapped matches — *`SyncScorers` / `SyncScorersForWorldCup` + `scripts/sync_scorers_backfill.ps1`; no FT/bets; empty timeline → Skipped; Bugbot highs fixed; **14** backfill + suite **375*** — *[detail](changes/phase-11-backfill.md)*

**Task 7 — Recent events honesty (`p11-recent-events-honesty`) (done; gate closed)**

- [x] Hide or omit `"Tournament Scorer"` / placeholder names in Recent events (and match timelines) — *API `ToHonestPlayerName` + fixtures/admin client sanitizer; name-only (not jersey 99 alone); Bugbot clean; honesty **3** + suite **380*** — *[detail](changes/phase-11-recent-events-honesty.md)*

**Task 8 — Tests & review gate (`p11-tests-gate`) (done; gate closed)**

- [x] Unit tests + Bugbot/docs review + `dotnet test` green — *Bugbot highs fixed (cancel rethrow + jersey-99 name-only); **+2** regressions; suite **382*** — *[detail](changes/phase-11-tests-gate.md)*

<a id="phase-12"></a>

### Phase 12 — 2030 World Cup Simulation **[Done]**

Offline Python simulation: lock six 2030 hosts, random 42 other teams, 12 groups, simulate group stage, then R32→Final bracket from standings (knockout scores open for Admin). **Tasks 1–4 done** (cup + draw + 72 group + bracket + smoke/unittest gate closed). Detail: [phase-12-planned](changes/phase-12-planned.md) · [Task 4 gate](changes/phase-12-tests-gate.md) · [Task 1 draw](changes/phase-12-draw.md) · [Task 2 group sim](changes/phase-12-group-sim.md) · [Task 3 bracket](changes/phase-12-bracket.md).

**Task 1 — Draw (`p12-draw`) (done)**

- [x] Document Phase 12 plan (hosts+random, hybrid sim, Python scripts) — *`docs/changes/phase-12-planned.md`*
- [x] `simulate_wc2030.py` — ensure WorldCup 2030, hosts + random 42, draw groups A–L — *multi-cup: non-unique `Team.CountryId`; [detail](changes/phase-12-draw.md)*

**Task 2 — Group simulation (`p12-group-sim`) (done)**

- [x] Schedule 72 group matches; simulate scores/goals/stats — *Poisson λ≈1.35; `--wipe-matches` 2030-only; unittest **14**; [detail](changes/phase-12-group-sim.md)*

**Task 3 — Qualify + bracket (`p12-bracket`) (done)**

- [x] Best-thirds qualification + R32→Final bracket with feeder FKs — *FIFA Art. 12 skeleton; 31 KO matches; unittest **22**; smoke 103; [detail](changes/phase-12-bracket.md)*

**Task 4 — Smoke + gate (`p12-tests-gate`) (done)**

- [x] Smoke run + Bugbot/docs review gate — *unittest **23** OK; smoke **103**; Bugbot highs fixed (cross-cup + demo kickoffs); [detail](changes/phase-12-tests-gate.md)*

<a id="phase-13"></a>

### Phase 13 — Company-Scoped Competitions **[Done]**

Soft multi-tenancy for workplace prediction pools: shared World Cup data, isolated membership and leaderboards. Portfolio-scoped v1 (invite codes; no branding/subdomains/billing). Detail: [phase-13-planned](changes/phase-13-planned.md) · [Task 2](changes/phase-13-company-model.md) · [Task 4](changes/phase-13-leaderboard-scope.md) · [Task 5 SPA](changes/phase-13-spa.md) · [Task 6 tests](changes/phase-13-tests-gate.md).

**Task 1 — Docs (`docs`) (done)**

- [x] Document Phase 13 plan (company model, invite join, scoped leaderboard, non-goals) — *`docs/changes/phase-13-planned.md`*

**Task 2 — Company model (`p13-company-model`) (done)**

- [x] `Company` entity + `User.CompanyId` + migration + repos — *invite code unique; seed `CompanyAdmin` role; [detail](changes/phase-13-company-model.md)*

**Task 3 — Company API (`p13-company-api`) (done; interim gate closed)**

- [x] Create / join / mine / rotate invite / members endpoints — *Admin creates; User joins by code; CompanyAdmin rotates; interim gate closed (suite **414**); [detail](changes/phase-13-company-api.md)*

**Task 4 — Leaderboard scope (`p13-leaderboard-scope`) (done; interim gate closed)**

- [x] Authorize + filter `GetLeaderboard` by caller’s company — *JWT required; scope from `User.CompanyId`; null → empty; optional `worldCupId` kept; interim gate closed (suite **399**); [detail](changes/phase-13-leaderboard-scope.md)*

**Task 5 — SPA (`p13-spa`) (done; interim gate closed)**

- [x] Join-company UX + company-scoped leaderboard + light CompanyAdmin UI — *`/company`; JWT-gated manage actions; leaderboard `authGuard` + join CTA; Admin create; [detail](changes/phase-13-spa.md)*

**Task 6 — Tests & gate (`p13-tests-gate`) (done; gate closed)**

- [x] Unit/integration isolation tests + `dotnet test` green — *two-company leak + CompanyController **17**; suite **438**; [detail](changes/phase-13-tests-gate.md)*
- [x] Bugbot/docs formal Task 6 review — *Bugbot clean after Join JWT session fix; Phase 13 Done*
