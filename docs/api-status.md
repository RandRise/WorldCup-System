# WorldCup System — API Status

## Docs navigation

[Dashboard](index.md) | [System Overview](overview.md) | [Completion Roadmap](roadmap.md) | [Dev Workflow](workflow.md) | [API Status](api-status.md) | [Data Model](data-model.md) | [Changes Review](changes-review.md)

---
## API Status

All domain controllers implemented (audit 17 Jul 2026). Base URL: `http://localhost:5055`. Knockout bracket API + offline WC 2026 schedule import in place. Live scores: manual import / Admin console; **post-match** FIFA calendar sync (Phase 10 Task 1) also available.

- **Controllers:** 17
- **Domain Services:** 20 (19 interfaces + `BetScoringRules`; includes `IMatchResultSyncService` / FIFA provider)
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
| `MatchController` | `GetMatches`, `GetMatchById`, `GetFixturesByWorldCup`, `GetFixturesByGroup`, `GetFixturesByDate`, `GetLiveSnapshot`; Admin `AddMatch`, `UpdateMatch`, `DeleteMatch`; Admin sync `SetExternalMatchId`, `SyncResult/{matchId}`, `SyncFinishedResults?worldCupId=` (stages include `RoundOf32`; `ExternalMatchId` on DTOs) | Admin on mutating / sync | Done |
| `KnockoutController` | `GetBracket`; Admin `GenerateBracket` (feeder auto-advance) | Admin on generate | Done |
| `GoalController` | `GetGoalsByMatch`; Admin `AddGoal`, `DeleteGoal` | Admin on mutating | Done |
| `CardController` | `GetCardsByMatch`; Admin `AddCard`, `DeleteCard` | Admin on mutating | Done |
| `TeamStatsController` | `GetTeamStatsByMatch`; Admin `UpdateTeamStats` | Admin on update | Done |
| `StandingController` | `GetGroupStandings` | None | Done |
| `BetController` | `GetScoringRules`, `GetLeaderboard`, `GetMySummary`; `GetMyBetsForWorldCup`, `GetMyBetForMatch`; `PlaceBet`, `GetMyBets`, `GetMyActiveBets`; Admin `ResolveBetsForMatch` (per-user +3/0 breakdown), `ResolveBetsForWorldCup` | JWT on place/view; Admin on resolve | Done |

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
| Sync one match | Done | Admin `POST Match/SyncResult/{matchId}` → FIFA FT → idempotent goals → resolve + advance | `p10-match-sync` |
| Sync batch | Done | Admin `POST Match/SyncFinishedResults?worldCupId=` | `p10-match-sync` |
| Provider | Done | `FifaCalendarMatchResultProvider` via `MatchResultSync` config (no RabbitMQ) | `p10-match-sync` |

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
