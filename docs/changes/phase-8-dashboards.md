# WorldCup System — Phase 8 User & Admin Dashboards

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md)

## Phase 8 — User & Admin Dashboards

Post-login navigation, user hub, bet resolve hardening, live snapshot, and admin ops (13–16 Jul 2026). Same uncommitted tree also includes Phase 7 knockout / WC 2026 data — see [phase-7-bugfixes](phase-7-bugfixes.md) · [wc2026-live-data](wc2026-live-data.md). [← Back to Changes Review](../changes-review.md) · [Roadmap Phase 8](../roadmap.md#phase-8)

> **Success:** **Implementation complete (16 Jul 2026 refresh).** All **10 / 10** Phase 8 roadmap tasks are done in code (uncommitted). Scoring remains **3** points correct / **0** wrong. Demo seed & CSV import were removed earlier — see [remove-demo-seed](remove-demo-seed.md).

### Review Gate — Status

> **Success:** **Gate closed (16 Jul 2026).** Bugbot high fixed; `dotnet test` **245 passed**.

| Blocker | Severity | Status | Action |
| --- | --- | --- | --- |
| Edited already-shipped migration `IdentityLongKeys` | Critical | Cleared | Not present in current working tree; schema additions live in forward `AddMatchStage` |
| Parallel possession race + other Bugbot findings | High | Fixed | Sequential stats save; fixtures snapshot on load; TBD UpdateMatch; stage lock |
| Phase 8 + knockout `dotnet test` | High | Passed | 245 passed / 0 failed |
| Large uncommitted Phase 8 (+ Phase 7 knockout) surface | High | Ops | Stage & commit when asked |
| Resolve / advance swallow incomplete-stats errors | Medium | Accepted | Optional Serilog / admin surface later |

### Git status snapshot (17 Jul 2026)

**Repo:** `WorldCup-System/` ·
**64 tracked** changes · **45 untracked** paths · tracked diff **+2716 / −6205** · untracked ~**+5.2k** lines

**Committed already (Phase 8 seed):** DevAdmin / DevUser in `Program.cs`

**Modified (uncommitted) — Phase 8 focus:** Bet resolve DTOs/services, Match live snapshot, fixtures/schedule UI, guards/login, admin teams

**Untracked (new) — Phase 8 SPA:** `dashboard/`, `admin/hub/`, `admin/live/`, goal/card/team-stats API services

**Also in tree (Phase 7 — document here for completeness):** Knockout service/controller, `MatchStage` migration, bracket SPA, WC 2026 scripts, Markdown docs

### Phase 8 Roadmap Mapping — 10 / 10 Done

| Task | Status | Evidence |
| --- | --- | --- |
| `p8-dev-roles` — Dev Admin + User seeds | Done | Committed `Program.cs` DevAdmin / DevUser |
| `p8-login-return` — return URL after login | Done | `auth.guard` / `admin.guard` pass `returnUrl`; `login.component` `sanitizeReturnUrl` + restore; else admin → `/admin`, user → `/dashboard`; `guest.guard` same defaults |
| `p8-user-dashboard` — profile / open bets / rank | Done | New `/dashboard` + `DashboardComponent` (`getMySummary` + active bets); shell nav for non-admins |
| `p8-bet-auto-resolve` — FT resolve + DeleteGoal re-score | Done | `ResolveBetsResultDTO` + per-user breakdown; `GoalService` re-resolves after FT; `TryAutoResolveFinishedMatch` + world-cup bulk; force resolve kept |
| `p8-admin-leaderboard` — resolve UI + points delta | Done | Schedule shows `userBreakdown` (+3 / 0); hub `bulkResolve` via `ResolveBetsForWorldCup` |
| `p8-live-snapshot` — batch live feed | Done | `GET Match/GetLiveSnapshot?worldCupId=` → scores, minute, recentEvents; fixtures 30s poll uses snapshot |
| `p8-live-action-api` — Goal/Card/TeamStats client | Done | New `goal-api`, `card-api`, `team-stats-api` services (existing controllers) |
| `p8-admin-live-console` — live match UI | Done | `/admin/live/:matchId` — goals/cards/stats + resolve; schedule links into console |
| `p8-admin-team-import` — manual team entry | Done | Admin Teams `saveTeam` / `editTeam` / `updateTeam` (create + update, no CSV) |
| `p8-admin-hub` — ops home | Done | `/admin` → `AdminHubComponent` cards (teams, schedule, live counts, bulk resolve, generate bracket) |

### Dev accounts (Development only)

| Role | Email | Password (local default) | SPA surface |
| --- | --- | --- | --- |
| `Admin` | `admin@localhost` | `Admin123!` | `/admin` hub → teams / schedule / live / bracket generate |
| `User` | `user@localhost` | `User123!` | `/dashboard` (default post-login) + My Bets |

### Architecture — Phase 8 flow

### Per-file detail (snippets)

#### BetController + BetService (+ DTOs)


**[p8-bet-auto-resolve · p8-admin-leaderboard]**

Resolve returns structured DTO with per-user points; new world-cup bulk resolve. Routes remain `[controller]/[action]`.

#### GoalService + CardService + TeamStatsService


**[p8-bet-auto-resolve · p8-live-action-api · p7-knockout]**

DeleteGoal/AddGoal re-resolve after FT and advance knockout feeders. Cards blocked until both teams are set. Possession can auto-balance the other side when one was 0%.

#### MatchController / MatchService — GetLiveSnapshot + Stage


**[p8-live-snapshot · p7-knockout]**

Batch snapshot of live/finished scores, computed minute for Live matches, recent goal/card events. Create/Update require valid `MatchStage`; group matches must share a group.

#### Data — Match entity + Program.cs


**[p7-knockout · ops]**

Nullable team FKs and feeder links for TBD bracket slots. Startup registers `IKnockoutService` and repairs Feeder* columns on stuck local DBs after migrate.

#### worldcup-client — dashboard / hub / live (NEW)


**[p8-user-dashboard · p8-admin-hub · p8-admin-live-console]**

Untracked feature folders. Routes: `/dashboard`, `/admin` hub, `/admin/live/:matchId`.

#### Guards + login + shell


**[p8-login-return]**

Deep-link restore; role-based default landing; Dashboard nav for users; Admin link to hub; Bracket nav.

#### Admin teams — UpdateTeam wiring


**[p8-admin-team-import]**

`saveTeam` create/update via `tournamentApi.updateTeam`; Edit loads form; Cancel resets.

#### Admin schedule + fixtures poll


**[p8-admin-leaderboard · p8-live-snapshot]**

Schedule resolves via `betApi` and shows per-user breakdown; Live console link; fixtures apply `getLiveSnapshot` on the 30s timer.

#### api.models.ts + match/bet API clients


**[p8-live-snapshot · p8-bet-auto-resolve]**

Client types for stage/feeders, live snapshot, resolve breakdown DTOs; API methods wired to new endpoints.

#### Tests


**[coverage]**

Expanded BetService (world-cup resolve, TryAutoResolve), MatchService GetLiveSnapshot, GoalService delete re-resolve, TeamStats possession helper, Standings H2H, KnockoutServiceTests (untracked), related controller updates. **Not executed in this documentation pass.**

### Happy path

1. Seed roles → log in as User → land on `/dashboard`; deep-link My Bets restores after login.
2. Admin hub → Teams (add/edit) → Schedule match → users place bets.
3. Admin opens Live console → record goals/cards/stats → fixtures poll shows snapshot.
4. After FT, resolve on goal paths (and knockout advance) or use Resolve / bulk resolve → leaderboard + schedule breakdown (+3 / 0).
5. Optional: Generate bracket from hub → view `/bracket`; import remaining WC results via scripts.
