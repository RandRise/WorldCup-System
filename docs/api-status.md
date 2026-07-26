# WorldCup System — API Status

## Docs navigation

[Dashboard](index.md) | [System Overview](overview.md) | [Completion Roadmap](roadmap.md) | [Dev Workflow](workflow.md) | [API Status](api-status.md) | [Data Model](data-model.md) | [Changes Review](changes-review.md)

---
## API Status

All domain controllers implemented (audit 17 Jul 2026). Base URL: `http://localhost:5055`. Knockout bracket API + offline WC 2026 schedule import in place. Live scores: manual import / Admin console; **post-match** FIFA calendar sync (Phase 10 Task 1) also available.

- **Controllers:** 19
- **Domain Services:** 21 (20 interfaces + `BetScoringRules`; includes `IMatchResultSyncService` / FIFA provider + `ICompanyService`)
- **Missing Domains:** 0

### Implemented Controllers

| Controller | Endpoints | Auth | Status |
| --- | --- | --- | --- |
| `UserController` | `Login`, `CreateNewUser` (public); `GetMe` (JWT); Admin `GetAllUsers`, `UpdateUser`, `RemoveUser` | JWT / Admin on mutators | Done |
| `WorldCupController` | `GetWorldCups`, `CreateNewWorldCup` | Admin on create | Done |
| `GroupController` | `GetGroups`, `AddGroup` | Admin on create | Done |
| `CountryController` | `GetAllCountries` | None | Done |
| `CityController` | `GetAllCities` | None | Done |
| `StadiumController` | `GetStadiums`, `AddStadium`, `UpdateStadium` | Admin on mutating | Done |
| `TeamController` | CRUD, assign to group, `GetTeams` (all teams — client filters by WC/group) | Admin on mutating | Done |
| `CoachController` | CRUD, get by team | Admin on mutating | Done |
| `PlayerController` | CRUD, squad list by team | Admin on mutating | Done |
| `PlayerPositionController` | List lookup values | None | Done |
| `MatchController` | `GetMatches`, `GetMatchById`, `GetFixturesByWorldCup`, `GetFixturesByGroup`, `GetFixturesByDate`, `GetLiveSnapshot`; Admin `AddMatch`, `UpdateMatch`, `DeleteMatch`; Admin sync `SetExternalMatchId` (optional `ExternalStageId`), `SyncResult/{matchId}`, `SyncFinishedResults?worldCupId=`, `SyncScorers/{matchId}`, `SyncScorersForWorldCup?worldCupId=` (stages include `RoundOf32`; `ExternalMatchId` / `ExternalStageId` on DTOs) | Admin on mutating / sync | Done |
| `KnockoutController` | `GetBracket`; Admin `GenerateBracket` (feeder auto-advance) | Admin on generate | Done |
| `GoalController` | `GetGoalsByMatch`; Admin `AddGoal`, `DeleteGoal` | Admin on mutating | Done |
| `CardController` | `GetCardsByMatch`; Admin `AddCard`, `DeleteCard` | Admin on mutating | Done |
| `TeamStatsController` | `GetTeamStatsByMatch`; Admin `UpdateTeamStats` | Admin on update | Done |
| `StandingController` | `GetGroupStandings` | None | Done |
| `BetController` | `GetScoringRules`, `GetLeaderboard` (JWT + company-scoped), `GetMySummary`; `GetMyBetsForWorldCup`, `GetMyBetForMatch`; `PlaceBet`, `GetMyBets`, `GetMyActiveBets`; Admin `ResolveBetsForMatch` (per-user +3/0 breakdown), `ResolveBetsForWorldCup` | JWT on place/view/leaderboard; Admin on resolve | Done |
| `CompanyController` | `Create`, `Join`, `Mine`, `RotateInviteCode`, `Members`, `GetAll` | Admin create/list; JWT join/mine; CompanyAdmin/Admin rotate + members | Done (Phase 13 Task 3) |

### Missing Controllers (Schema Exists)

All domain entities with DB tables now have controllers. `Bet` and `BetResult` are served via `BetController` (Phase 4). Demo CSV seeding and `SeedController` were removed — countries, cities, and stadiums are ordinary DB rows (populate once). `WeatherForecastController` was removed.

### Phase 8 — Live / resolve (implemented)

Phase 8 closed the live-feed and SPA write gaps. Detail: [phase-8-dashboards](changes/phase-8-dashboards.md) · [Phase 8 checklist](roadmap.md#phase-8).

| Capability | Status | Surface | Task |
| --- | --- | --- | --- |
| Live snapshot | Done | `GET Match/GetLiveSnapshot?worldCupId=` — scores, minute, recentEvents; fixtures 30s poll | `p8-live-snapshot` |
| Live action write | Done | Client `goal-api` / `card-api` / `team-stats-api` → existing Admin controllers (no RecordLiveAction facade) | `p8-live-action-api` |
| Admin live UI | Done | `/admin/live/{matchId}` console + hub / schedule links | `p8-admin-live-console` |
| Bet resolve feedback | Done | `ResolveBetsResultDTO` per-user +3/0; DeleteGoal re-score; `ResolveBetsForWorldCup` bulk | `p8-bet-auto-resolve` |

### Phase 10 — Post-match sync (Task 1 API)

Post-match FIFA calendar sync — not a live in-match feed. Detail: [phase-10-match-sync](changes/phase-10-match-sync.md) · [Phase 10 checklist](roadmap.md#phase-10).

| Capability | Status | Surface | Task |
| --- | --- | --- | --- |
| External match mapping | Done | `Match.ExternalMatchId`; Admin `POST Match/SetExternalMatchId` | `p10-match-sync` |
| Sync one match | Done | Admin `POST Match/SyncResult/{matchId}` → FIFA FT → idempotent goals → resolve + advance → (Phase 11) fail-soft timeline scorers | `p10-match-sync` / `p11-sync-wire` |
| Sync batch | Done | Admin `POST Match/SyncFinishedResults?worldCupId=` (+ scorer counts / batch delay) | `p10-match-sync` / `p11-sync-wire` |
| Provider | Done | `FifaCalendarMatchResultProvider` via `MatchResultSync` config (no RabbitMQ) | `p10-match-sync` |

### Phase 11 — Real scorers (Done)

FIFA timeline Goal! import so Recent events show true scorers. **Tasks 1–8 done** (gate closed; Bugbot highs fixed; suite **382**). Detail: [phase-11-tests-gate](changes/phase-11-tests-gate.md) · [phase-11-planned](changes/phase-11-planned.md) · [Task 1](changes/phase-11-external-stage.md) · [Task 2](changes/phase-11-timeline-provider.md) · [Task 3](changes/phase-11-player-resolve.md) · [Task 4](changes/phase-11-scorer-apply.md) · [Task 5](changes/phase-11-sync-wire.md) · [Task 6](changes/phase-11-backfill.md) · [Task 7](changes/phase-11-recent-events-honesty.md) · [Phase 11 checklist](roadmap.md#phase-11).

| Capability | Status | Surface | Task |
| --- | --- | --- | --- |
| External stage id | Done | `Match.ExternalStageId`; optional on `SetExternalMatchId`; calendar `IdStage` auto-fill on sync | `p11-external-stage` |
| Timeline provider | Done | `FifaTimelineEventsProvider` via `MatchResultSync:TimelineBaseUrl`; Goal! / Own Goal / Penalty Goal parse | `p11-timeline-provider` |
| Player resolve | Done | `ITimelinePlayerResolver` / `TimelinePlayerResolver`; `Player.ExternalPlayerId` | `p11-player-resolve` |
| Scorer apply | Done | `ITimelineScorerApplyService` / `TimelineScorerApplyService` — in-place Goal rewrite when counts align | `p11-scorer-apply` |
| Sync wire | Done | `SyncResult` / `SyncFinishedResults` call timeline + apply (fail-soft); `ScorerStatus` on DTO; `BatchDelayMilliseconds` | `p11-sync-wire` |
| Scorers backfill | Done | Admin `POST Match/SyncScorers/{matchId}`, `POST Match/SyncScorersForWorldCup?worldCupId=`; client `syncScorers` / `syncScorersForWorldCup`; optional `scripts/sync_scorers_backfill.ps1` (FT/bets unchanged; empty timeline → `Skipped`; batch hard fail → `Warning`) | `p11-backfill` |
| Recent events honesty | Done | `GetLiveSnapshot` / match-detail omit `"Tournament Scorer"` via `ToHonestPlayerName`; fixtures + admin live sanitize (name-only) | `p11-recent-events-honesty` |
| Tests + review gate | Done | Bugbot highs fixed; suite **382** | `p11-tests-gate` |

<a id="recent-events-honesty-phase-11-task-7"></a>

### Recent events honesty (Phase 11 Task 7)

| Surface | Behavior |
| --- | --- |
| `GET Match/GetLiveSnapshot` | Goal/Card `PlayerName` null when blank or `"Tournament Scorer"` |
| `GetMatchById` / team stats DTOs | Same via `BuildTeamStatsDto` |
| Fixtures Recent events | Client remaps `playerName` through `toHonestPlayerName` |
| Admin live Event timeline | Labels omit placeholder; picker excludes placeholder (+ jersey 99 for picker only) |

Does not rewrite Goal rows — display safety net until/after Tasks 4–6 backfill. Detail: [phase-11-recent-events-honesty](changes/phase-11-recent-events-honesty.md).

<a id="sync-scorers-phase-11-task-6"></a>

### Scorers-only backfill endpoints (Phase 11 Task 6)

| Endpoint | Auth | Behavior |
| --- | --- | --- |
| `POST Match/SyncScorers/{matchId}` | Admin | Calendar finished + orient + timeline apply only; no Goal-count rewrite, bets, or knockout |
| `POST Match/SyncScorersForWorldCup?worldCupId=` | Admin | Batch mapped fixtures; `BatchDelayMilliseconds`; hard failures → per-match `ScorerStatus=Warning` |

Response uses the same `SyncMatchResultDTO` / `SyncFinishedResultsDTO` scorer fields as Task 5. Ops: map External ids → backfill → verify `GetLiveSnapshot` Recent events. Detail: [phase-11-backfill](changes/phase-11-backfill.md).

<a id="sync-result-dto-fields-phase-11-task-5"></a>

### Sync result DTO fields (Phase 11 Task 5)

Admin sync responses (`SyncMatchResultDTO` / `SyncFinishedResultsDTO`; client `SyncMatchResult` / `SyncFinishedResults`).

| Field | On | Type | Meaning |
| --- | --- | --- | --- |
| `ExternalStageId` | per-match | `string?` | FIFA IdStage on the match after sync / SetExternal |
| `ScorerStatus` | per-match | `string?` | `Applied` \| `AlreadyMatched` \| `Skipped` \| `Warning`, or `null` if scorers not attempted (e.g. not finished) |
| `ScorerGoalsUpdated` | per-match | `int` | Goal rows rewritten on this attempt |
| `ScorerMessage` | per-match | `string?` | Human-readable scorer step note (also appended to `Message` as `Scorers: …`) |
| `Warning` | per-match | `string?` | May include merged bet/advance warning + `Scorer sync warning: …` |
| `ScorersApplied` | batch | `int` | Matches with `ScorerStatus == Applied` |
| `ScorerWarnings` | batch | `int` | Matches with `ScorerStatus == Warning` |

Config: `MatchResultSync:BatchDelayMilliseconds` (default **250**; `0` disables). Detail: [phase-11-sync-wire](changes/phase-11-sync-wire.md).

### Phase 13 — Company API + Leaderboard scope + SPA

**Task 3 Done** (interim gate closed): Admin create + invite; JWT join/mine; CompanyAdmin rotate/members; GetAll. Detail: [phase-13-company-api](changes/phase-13-company-api.md) · [Phase 13 checklist](roadmap.md#phase-13).

| Endpoint | Auth | Behavior |
| --- | --- | --- |
| `POST Company/Create` | Admin | Name + optional slug → company + uppercase invite |
| `POST Company/Join` | JWT | Invite code → `User.CompanyId`; first joiner → CompanyAdmin + JWT re-issue |
| `GET Company/Mine` | JWT | Company summary (`Company: null` if none); invite for CompanyAdmin/Admin |
| `POST Company/RotateInviteCode` | CompanyAdmin / Admin | New invite; Admin may pass `companyId` |
| `GET Company/Members` | CompanyAdmin / Admin | Same-company members only |
| `GET Company/GetAll` | Admin | All companies (ops) |

**Task 4 Done** (interim gate closed): `GET Bet/GetLeaderboard` requires JWT; company from caller `User.CompanyId` (never client `companyId`); null company → `[]`; optional `worldCupId` kept. Detail: [phase-13-leaderboard-scope](changes/phase-13-leaderboard-scope.md).

**Task 5 Done** (interim gate pending): SPA `/company` join + CompanyAdmin; leaderboard `authGuard` + company name / join CTA; Admin create on company page. Detail: [phase-13-spa](changes/phase-13-spa.md).

**Task 6 Open** (full phase gate): isolation tests / formal Bugbot+docs — [phase-13-tests-gate](changes/phase-13-tests-gate.md).

### Repository Manager Coverage

### ✅ In RepositoryManager

- User
- Country
- City
- Stadium
- WorldCup
- Group
- Team
- Coach
- Player
- PlayerPosition
- Match
- Goal
- Card
- TeamStats
- Bet
- BetResult
- Company

### ❌ Not in RepositoryManager

- — (all schema entities wired)

### API Coverage Matrix

| Entity | DB Table | Repo | Service | Controller | DTO |
| --- | --- | --- | --- | --- | --- |
| WorldCup | WorldCups | ✅ | ✅ | ✅ | ✅ |
| Group | Group | ✅ | ✅ | ✅ | ✅ |
| Country | Countries | ✅ | ✅ | ✅ | ✅ |
| City | City | ✅ | ✅ | ✅ | ✅ |
| Stadium | Stadium | ✅ | ✅ | ✅ | ✅ |
| User | Users | ✅ | ✅ | ✅ | ✅ |
| Company | Company | ✅ | ✅ | ✅ | ✅ |
| Team | Team | ✅ | ✅ | ✅ | ✅ |
| Coach | Coach | ✅ | ✅ | ✅ | ✅ |
| Player | Player | ✅ | ✅ | ✅ | ✅ |
| PlayerPosition | PlayerPositions | ✅ | ✅ | ✅ | ✅ |
| Match | Match | ✅ | ✅ | ✅ | ✅ |
| Goal | Goal | ✅ | ✅ | ✅ | ✅ |
| Card | Card | ✅ | ✅ | ✅ | ✅ |
| TeamStats | TeamStats | ✅ | ✅ | ✅ | ✅ |
| Standings | — (computed) | — | ✅ | ✅ | ✅ |
| Bet | Bet | ✅ | ✅ | ✅ | ✅ |
| BetResult | BetResult | ✅ | ✅ | ✅ | ✅ |
