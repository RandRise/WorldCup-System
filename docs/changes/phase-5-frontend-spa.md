# WorldCup System — Phase 5 Frontend SPA Changes

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md)

## Phase 5 — Frontend SPA

Full diff detail for Angular `worldcup-client` and supporting backend seed APIs. [← Back to Changes Review](../changes-review.md)

> **Info:** **Sources (2 Jul 2026).** Backend: `git status` + `git diff` in `WorldCup-System/` — branch `master`, ahead of `origin/master` by 3 commits. Working tree: **3 modified**, **6 untracked paths** (demo seed support). Frontend: `worldcup-client/` at workspace root — **not tracked by git** — **69 source files** under `src/` (Angular 19, default port 4200).

### Review Gate — Fix Before Testing

| Blocker | Severity | File / area | Required fix |
| --- | --- | --- | --- |
| Angular SPA outside version control | Critical | `worldcup-client/` in git repo | Cleared — monorepo at `WorldCup-System/worldcup-client/` |
| Demo seed backend untracked / unstaged | Critical | `DemoSeedService`, `SeedController`, `Core/DemoSeedData/` | Cleared — all paths staged |
| Seed unit tests not verified in gate run | High | `DemoSeedServiceTests`, `SeedControllerTests` | Cleared — `dotnet test` 195 passed |
| No meaningful Angular test coverage | High | `worldcup-client/src/` | Cleared — 13 Karma tests (auth, guards, standings) |
| Fixtures page N+1 bet lookups | Medium | `fixtures.component.ts` | Calls `GetMyBetForMatch` once per fixture in parallel — acceptable for demo (~48 matches) but may need batch endpoint for scale. |
| Dev auto-seed on API startup | Medium | `Program.cs`, `DevSeed:Qatar2022` | Development startup auto-runs `SeedQatar2022Async` when flag is true — can mask admin seed UI failures; disable flag when testing `AdminSeedComponent` manually. |

### Architecture — Angular SPA + Seed API

### Phase 5 Roadmap Mapping

| Task | Status | Evidence |
| --- | --- | --- |
| `p5-scaffold` — Angular project on port 4200 | Done | `worldcup-client/` — Angular 19.2, SCSS, standalone components, `ng serve` default port 4200 |
| `p5-auth` — Login, JWT interceptor, route guards | Done | `AuthService`, `authInterceptor`, `authGuard`, `adminGuard`, `guestGuard`, login/register routes |
| `p5-admin-ref` — Countries, cities, stadiums | Done | `AdminReferenceComponent` + `ReferenceApiService` — CRUD tabs, city CSV import |
| `p5-admin-teams` — Teams, coaches, squad | Done | `AdminTeamsComponent` + `TournamentApiService` — team/coach/player management |
| `p5-schedule` — Match scheduling UI | Done | `AdminScheduleComponent` + `MatchApiService` — create/edit fixtures filtered by World Cup |
| `p5-public` — Fixtures, live scores, standings | Done | `FixturesComponent` (status badges), `StandingsComponent` (per-group tables), `WorldCupSelectorComponent` |
| `p5-betting-ui` — Betting UI and leaderboard | Done | `FixturesComponent.placeBet`, `MyBetsComponent`, `LeaderboardComponent` + `GetMySummary` |
| `p5-seed` — Qatar 2022 demo CSV seed | Done | `DemoSeedService`, `AdminSeedComponent`, `Seed/LoadQatar2022Demo`, embedded CSVs |

<a id="backend-diff"></a>

### Backend Git Diff — Demo Seed Support

#### Core/Core.csproj


**[Embedded CSVs]**

Embeds existing city/stadium CSVs plus new `DemoSeedData/` for runtime seeding without disk paths.

#### WorldCup-System/Program.cs


**[DI + dev auto-seed]**

Registers `IDemoSeedService`; optionally seeds Qatar 2022 on Development startup when `DevSeed:Qatar2022` is true.

#### WorldCup-System/appsettings.Development.json


**[Dev flag]**

Enables automatic demo seed on API boot in Development.

#### Core/Services/Seeding/DemoSeedService.cs


**[p5-seed]**

Idempotent import: countries → Qatar cities → stadiums → World Cup 2022 → groups A–H → 32 teams from `Qatar2022_teams.csv`. Skips when tournament already has teams.

#### WorldCup-System/Controllers/SeedController.cs


**[Admin endpoint]**

Admin-only POST to trigger the same seed logic from the SPA admin panel.

#### WorldCup-System.Tests/Services/Seeding/DemoSeedServiceTests.cs


**[New tests]**

Verifies all four CSV files are embedded in Core assembly; idempotency when teams already exist.

<a id="spa-routing"></a>

### Angular — Routes & Layout

`ShellComponent` topbar with nav links; admin section behind `adminGuard`; bets behind `authGuard`; login/register behind `guestGuard`.

<a id="spa-auth"></a>

### Angular — Auth Module (`p5-auth`)

JWT stored in `localStorage`; session restored via `User/GetMe` on app init; interceptor attaches Bearer token except login/register.

<a id="spa-api"></a>

### Angular — API Services

| Service | Backend endpoints | Used by |
| --- | --- | --- |
| `BetApiService` | GetLeaderboard, GetMySummary, GetMyBets, GetMyActiveBets, GetMyBetForMatch, PlaceBet | Fixtures, Leaderboard, My Bets |
| `MatchApiService` | GetFixturesByWorldCup, CRUD matches | Fixtures, Admin Schedule |
| `StandingsApiService` | GetGroupStandings | Standings |
| `TournamentApiService` | WorldCups, Groups, Teams, Coaches, Players | Admin Teams, WorldCup context |
| `ReferenceApiService` | Countries, Cities, Stadiums, CSV import | Admin Reference |
| `SeedService` | Seed/LoadQatar2022Demo | Admin Seed |

<a id="spa-public"></a>

### Angular — Public Pages (`p5-public`, `p5-betting-ui`)

#### FixturesComponent


**[Betting UI]**

Lists matches for selected World Cup; shows `Status` (Scheduled/Live/Finished); authenticated users place team/draw bets when `canBet`.

#### StandingsComponent


**[Group tables]**

Group tab selector driven by `WorldCupContextService`; loads standings per group.

#### LeaderboardComponent


**[Rankings]**

Public leaderboard with optional World Cup filter; highlights current user row; shows personal summary when logged in.

#### MyBetsComponent


**[Bet history]**

Toggle all bets vs active-only; shows resolution status and points earned.

<a id="spa-admin"></a>

### Angular — Admin Pages

#### AdminSeedComponent


**[p5-seed]**

One-click Qatar 2022 demo load; displays counts from `DemoSeedResultDTO`.

#### AdminReferenceComponent


**[p5-admin-ref]**

Tabbed CRUD for countries, cities (with country filter), stadiums; city CSV import by country.

#### AdminTeamsComponent


**[p5-admin-teams]**

Teams scoped to selected World Cup groups; coach and player squad management.

#### AdminScheduleComponent


**[p5-schedule]**

Create and edit matches; filters teams/stadiums to selected tournament.

<a id="spa-shared"></a>

### Angular — Shared Services & Components

#### WorldCupContextService


**[Tournament selector]**

Loads World Cups and groups on init; auto-selects 2022 tournament when present; drives selector on public and admin pages.

#### environment.development.ts


**[API URL]**

Points SPA at local API.

<a id="file-list"></a>

### Complete File List

**Backend git — modified (3):**

**Backend git — untracked (6 paths, 9 files):**

**Angular SPA — not in git (69 files under `src/`):**

**Prior phases (committed):**

### Suggested Next Steps

#### Clear review gate blockers


Stage backend seed files; add `worldcup-client` to version control; run `dotnet test`.

#### Smoke test full stack

API on :5055 + `ng serve` on :4200 → Admin seed → schedule matches → user login → place bets → standings/leaderboard.

#### Commit Phase 5

Backend seed + SPA sources after gate cleared — see [Phase 5 checklist](../roadmap.md#phase-5).

#### Advance to Phase 6

Docker, CI, integration tests, and production build pipeline.
