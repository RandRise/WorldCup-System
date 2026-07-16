# WorldCup System — Phase 3 Match Lifecycle Changes

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md)

## Phase 3 — Match Lifecycle

Full diff detail for unstaged changes. [← Back to Changes Review](../changes-review.md)

> **Info:** Source: `git status` + `git diff` on `master` (uncommitted, 1 Jul 2026). Repo: `WorldCup-System/` — branch ahead of `origin/master` by 1 commit (Phase 2). Current diff: **3 modified**, **0 deleted**, **17 untracked entries** (~20 new source files).

### Review Gate — Fix Before Testing

| Blocker | Severity | File / area | Required fix |  |
| --- | --- | --- | --- | --- |
| Phase 3 source untracked | Critical | Match/Goal/Card/TeamStats/Standing controllers, services, DTOs | `git add` all untracked paths before commit; CI cannot reproduce build without them. |  |
| No Phase 3 unit tests | Critical | `WorldCup-System.Tests/` — no Match, Goal, Card, Standings, or TeamStats tests | Add service/controller tests per phase gate; run `dotnet test` before advancing. |  |
| `GetFixturesByWorldCup` over-inclusive filter | High | `MatchService.GetFixturesByWorldCup` | Uses `OR` on team membership — matches with only one World Cup team are included. Require `AND` (both teams in tournament groups) or explicit WorldCup FK on Match. |  |
| Standings kickoff vs UTC clock | High | `StandingsService.GetGroupStandings` | Compares `match.Date` to `DateTime.UtcNow` — unspecified/local kickoff times may mark future matches as played or skip completed ones. | Normalize match dates to UTC on write, or compare using same `DateTimeKind`. |
| Possession not cross-validated | High | `TeamStatsService.UpdateTeamStats` | Each team validated 0–100% independently; both teams can sum to 200%. Standings ignore possession but live stats UI will be wrong. | Validate pair sums to 100 on update, or update both in one transaction. |

### Architecture — Match Lifecycle Layer

### Phase 3 Roadmap Mapping

| Task | Status | Files |
| --- | --- | --- |
| `p3-match-crud` | Done | Core/DTOs/Matches/MatchDTO.cs, MatchService, MatchController |
| `p3-goals` | Done | Core/DTOs/Goals/GoalDTO.cs, GoalService, GoalController |
| `p3-cards` | Done | Core/DTOs/Cards/CardDTO.cs, CardService, CardController |
| `p3-stats` | Done | Core/DTOs/Stats/TeamStatsDTO.cs, TeamStatsService, TeamStatsController |
| `p3-standings` | Done | Core/DTOs/Standings/StandingDTO.cs, StandingsService, StandingController |
| `p3-fixtures` | Done | MatchService.GetFixturesByWorldCup/Group/Date, MatchController fixture actions |
| `p3-knockout` | Not started | — (out of scope in this diff) |

<a id="repos"></a>

### Data/Repos — RepositoryManager extension

Lazy-initialized repos for match lifecycle entities added to interface and manager.

<a id="program"></a>

### WorldCup-System/Program.cs — DI registration

Registers five scoped services for match lifecycle.

<a id="match"></a>

### MatchService + MatchController — p3-match-crud · p3-fixtures

`AddMatch` validates distinct teams and FK existence; creates two `TeamStats` rows with the match. `UpdateMatch` blocks team changes after goals/cards recorded. `DeleteMatch` requires no goals/cards. `GetMatchById` returns nested goals/cards/stats. Fixture queries filter by WorldCup (via group teams), group (both teams in group), or calendar date.

<a id="goals"></a>

### GoalService + GoalController — p3-goals

Goals linked to `TeamStats` via scorer's credited team. Own goals require player on opponent squad; regular goals require player on credited team. Minute stored as `match.Date.AddMinutes(minute)`.

<a id="cards"></a>

### CardService + CardController — p3-cards

Yellow (1) and red (2) cards with minute and team validation. Player must belong to specified team.

<a id="stats"></a>

### TeamStatsService + TeamStatsController — p3-stats

Read possession/shots/shots-on-target per team per match. Admin updates numeric stats; score derived from goal count on read.

<a id="standings"></a>

### StandingsService + StandingController — p3-standings

Builds group table from intra-group matches only (both teams in group). Counts goals per `TeamStats`; awards 3/1/0 points. Sorts by points, goal difference, goals for, team name. Skips matches with `Date > UtcNow`.

<a id="dtos"></a>

### DTOs — validation annotations

All mutating DTOs use `[Range]` and `[Required]` where applicable. `MatchDetailDTO` nests `MatchTeamStatsDTO` with goal/card lists for rich match view.

<a id="file-list"></a>

### Complete File List

**Modified (3):**

**Untracked (17 entries, ~20 files):**

### Suggested Next Steps

#### Clear review gate blockers


Stage untracked files; fix `GetFixturesByWorldCup` filter and standings date comparison; add possession pair validation.

#### Write Phase 3 unit tests

MatchService (CRUD guards, fixtures), GoalService (own goal rules), StandingsService (points/GD ordering).

#### Run test suite

`dotnet test WorldCup-System.Tests\WorldCup-System.Tests.csproj`

#### Smoke test match lifecycle

Admin: schedule match → record goals/cards → update possession → verify standings and fixture endpoints.
