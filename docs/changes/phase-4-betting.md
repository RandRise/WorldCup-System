# WorldCup System — Phase 4 Betting Module Changes

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md)

## Phase 4 — Betting Module

Full diff detail for unstaged changes. [← Back to Changes Review](../changes-review.md)

> **Info:** Source: `git status` + `git diff` on `master` (uncommitted, 1 Jul 2026). Repo: `WorldCup-System/` — branch ahead of `origin/master` by 1 commit (Phase 2). Current diff: **6 modified**, **0 deleted**, **41 untracked files** (20 top-level paths). Working tree also includes Phase 3 match lifecycle and unit tests.

### Review Gate — Fix Before Testing

| Blocker | Severity | File / area | Required fix |
| --- | --- | --- | --- |
| Phase 4 source untracked / unstaged | Critical | Bet services, DTOs, controller, migration, tests | `git add` all betting paths and modified entity/repo files before commit; CI cannot reproduce build without them. |
| `BetDrawSupport` migration not applied | Critical | `Data/Migrations/20260701144245_BetDrawSupport.cs` | Run `dotnet ef database update` so `Bet.IsDraw` and nullable `TeamId` exist in PostgreSQL before integration or smoke tests. |
| Early bet resolution allowed | High | `BetService.ResolveBetsForMatch` | Only checks `match.Date <= DateTime.Now` — admin can resolve at kickoff with partial goal counts. Require match-finished signal or minimum elapsed time before scoring. |
| Kickoff uses `DateTime.Now` | High | `BetService.PlaceBet`, `ResolveBetsForMatch` | Local server clock vs UTC/unspecified `match.Date` can allow late bets or block valid ones. Align with UTC or consistent `DateTimeKind` (same issue as Phase 3 standings). |
| Phase 3 blockers in same tree | High | `MatchService`, `StandingsService`, `TeamStatsService` | Bet resolution reads goals via Phase 3 match stats — fixture filter, standings UTC, and possession bugs affect end-to-end betting smoke tests. Clear Phase 3 gate items first. |

### Architecture — Betting Layer

### Phase 4 Roadmap Mapping

| Task | Status | Files |
| --- | --- | --- |
| `p4-place-bet` | Done | PlaceBetDTO, BetService.PlaceBet, BetController.PlaceBet |
| `p4-view-bets` | Done | BetDTO, GetUserBets, GetMyActiveBets, BetController |
| `p4-resolve` | Done | BetService.ResolveBetsForMatch, BetResult entity usage, Admin endpoint |
| `p4-leaderboard` | Done | LeaderboardService, LeaderboardEntryDTO, GetLeaderboard |
| `p4-rules` | Done | BetScoringRules, GetScoringRules endpoint |

<a id="entity"></a>

### Data/Entities/Bet.cs — draw support

Navigation properties initialized; `TeamId` nullable for draw bets; `IsDraw` flag added.

<a id="context"></a>

### ApplicationDbContext — optional Team FK

Bet → Team relationship no longer required when predicting a draw.

<a id="migration"></a>

### Migration — BetDrawSupport

Alters `Bet.TeamId` to nullable; adds `IsDraw` boolean (default false).

<a id="repos"></a>

### Data/Repos — Bet + BetResult wiring

Lazy repositories for betting entities (also extends Phase 3 match repos in same diff).

<a id="program"></a>

### WorldCup-System/Program.cs — DI registration

Registers betting services as scoped (alongside Phase 3 match services).

<a id="bet-service"></a>

### BetService — p4-place-bet · p4-view-bets · p4-resolve · p4-rules

One bet per user per match; validates draw vs team prediction; blocks bets after kickoff. Resolution counts goals per TeamStats to determine winner/draw. Idempotent resolve skips bets with existing BetResult.

<a id="leaderboard"></a>

### LeaderboardService — p4-leaderboard

Sums `BetResult.Point` per user; tie-break by resolved bet count, then username. Assigns rank 1..n.

<a id="scoring-rules"></a>

### BetScoringRules — p4-rules

Static rules document: 3 points correct, 0 incorrect; bet before kickoff; one per match; resolution from recorded goals.

<a id="controller"></a>

### BetController — API surface

Resolves current user from JWT email claim via `UserManager`. Uses `ApiErrorHelper` for exceptions.

<a id="dtos"></a>

### Core/DTOs/Bets — DTOs

Rich bet view with match names, predicted outcome label, resolution flags. PlaceBetDTO validates positive MatchId.

<a id="tests"></a>

### Unit Tests — WorldCup-System.Tests

Phase 4 test coverage added (alongside Phase 3 controller/service tests in same working tree).

<a id="file-list"></a>

### Complete File List — Phase 4 focus

**Modified (6):**

**Untracked — Phase 4 (11 files):**

**Also in working tree (Phase 3):**

### Suggested Next Steps

#### Clear review gate blockers


Stage all files; apply `BetDrawSupport` migration; fix kickoff timezone and early-resolution guards.

#### Run unit tests

`dotnet test WorldCup-System.Tests\WorldCup-System.Tests.csproj`

#### Smoke test betting flow

User: login → GetScoringRules → PlaceBet on future match → Admin: record goals → ResolveBetsForMatch → verify GetMyBets points and GetLeaderboard rank.

#### Commit Phase 4

After tests pass and gate cleared — see [Phase 4 checklist](../roadmap.md#phase-4).
