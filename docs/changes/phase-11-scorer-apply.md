# WorldCup System — Phase 11 Task 4: Real-Scorer Apply

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 11 plan](phase-11-planned.md)

## Phase 11 Task 4 — Scorer apply (`p11-scorer-apply`)

Opened **20 Jul 2026**. Checklist: [Roadmap Phase 11](../roadmap.md#phase-11).

> **Success:** **Task 4 complete (20 Jul 2026).** `ITimelineScorerApplyService` + `TimelineScorerApplyService` rewrite existing Goal `PlayerId` / `TimeScored` / `IsOwnGoal` when timeline per-side counts match. Count mismatch fails hard (goals unchanged). Preserve FIFA Event order (same-minute safe). No bet resolve / knockout. Wired into SyncResult by Task 5. Review gate closed — Bugbot high fixed; apply **10** cases; suite **353** passed.

### Review gate

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Same-minute goals mis-paired by re-sort | High (Bugbot) | **Fixed** | Preserve timeline Event order when filtering sides |
| Count mismatch mutates goals / invents scores | High | **Cleared by design** | Throws `InvalidOperationException`; leaves Goal rows unchanged |
| Scorer-only change re-resolves bets / advances knockout | High | **Cleared by design** | Apply service has no bet/knockout deps |
| Own-goal player resolved on wrong team | High | **Cleared** | Conceding team id passed to `ITimelinePlayerResolver` |
| Idempotent second apply | High (acceptance) | **Cleared** | Same PlayerIds / minutes → `AlreadyMatched`; one `SaveAsync` |
| ExternalMatchId mismatch / missing TeamStats | High | **Cleared** | Throws before Goal writes |
| Bugbot high/critical | High (gate) | **Cleared** | Same-minute fix |
| Apply suite green | High (gate) | **Cleared** | **10** apply + suite **353** |
| Not wired into SyncResult | Info | By design | Task 5 |

### Decision — count mismatch

| Choice | Why |
| --- | --- |
| **Fail with clear `InvalidOperationException`** | Calendar FT + existing Goal counts stay authoritative; no silent placeholder rebuild. Task 5 catches as scorer warning so FT sync still succeeds. |

### Algorithm

1. Load match; require both teams + TeamStats (FT sync first).
2. Guard: timeline `ExternalMatchId` must match `Match.ExternalMatchId` when both set.
3. Orient timeline `IsHomeSide` → TeamOne/TeamTwo via `homeMapsToTeamOne` (same as Phase 10 calendar orientation).
4. Compare timeline goal counts per local side to existing Goal counts (ordered by `TimeScored` / Id).
5. If disagree → throw (no Goal writes, no `SaveAsync`).
6. If both sides empty (0–0) → `AlreadyMatched` without save.
7. If agree → zip each side preserving timeline Event order; resolve player; update in place when `PlayerId`, `TimeScored`, or `IsOwnGoal` differ.
8. Own goal: credit stays on benefiting side’s TeamStats; resolver uses **conceding** team id.
9. Single `SaveAsync` only when at least one row changed. No bet / knockout calls.

### Deliverables

| Item | Detail | Status |
| --- | --- | --- |
| Interface | `ITimelineScorerApplyService.ApplyAsync(matchId, events, homeMapsToTeamOne)` | Done |
| Result | `TimelineScorerApplyResult` — Applied / AlreadyMatched / GoalsUpdated / Message / Warning | Done |
| Service | `TimelineScorerApplyService` | Done |
| DI | `AddScoped<ITimelineScorerApplyService, TimelineScorerApplyService>` | Done |
| Count mismatch | Throw; leave goals unchanged | Done |
| Own goal | Preserve `IsOwnGoal`; resolve on conceding squad | Done |
| Tests | Align / idempotent / mismatch / orientation / OG / 0-0 / id mismatch / missing stats / same-minute order | Done (10 cases; suite **353**) |
| SyncResult wire | — | Task 5 |

### Acceptance

Re-apply on a finished match with correct score but wrong scorers updates player names and minutes; second apply is idempotent (`AlreadyMatched`). Count mismatch does not change Goal rows. Scorer-only apply never touches bets or knockout.

### Git file list (Task 4 — from `git status` / `git diff`)

| Status | Path |
| --- | --- |
| **New** | `Core/Services/MatchSync/ITimelineScorerApplyService.cs` |
| **New** | `Core/Services/MatchSync/TimelineScorerApplyService.cs` |
| **New** | `Core/Services/MatchSync/TimelineScorerApplyResult.cs` |
| **New** | `WorldCup-System.Tests/Services/MatchSync/TimelineScorerApplyServiceTests.cs` |
| Modified | `WorldCup-System/Program.cs` — `AddScoped<ITimelineScorerApplyService, TimelineScorerApplyService>` |
| Docs | `docs/changes/phase-11-scorer-apply.md`, planned / roadmap / changes-review / api-status / index |

### Diff snippets

#### Interface

```csharp
public interface ITimelineScorerApplyService
{
    Task<TimelineScorerApplyResult> ApplyAsync(
        int matchId,
        ExternalMatchEvents events,
        bool homeMapsToTeamOne,
        CancellationToken cancellationToken = default);
}
```

#### Result DTO

```csharp
public class TimelineScorerApplyResult
{
    public int MatchId { get; init; }
    public bool Applied { get; init; }
    public bool AlreadyMatched { get; init; }
    public int GoalsUpdated { get; init; }
    public required string Message { get; init; }
    public string? Warning { get; init; }
}
```

#### Program.cs — DI

```csharp
builder.Services.AddScoped<ITimelineScorerApplyService, TimelineScorerApplyService>();
```

#### Count mismatch (fail hard here; soft at Task 5 sync layer)

```csharp
if (teamOneEvents.Count != teamOneGoals.Count || teamTwoEvents.Count != teamTwoGoals.Count)
{
    throw new InvalidOperationException(
        $"Timeline goal counts do not match existing Goal rows for match {matchId}. " +
        $"Timeline TeamOne/TeamTwo={teamOneEvents.Count}/{teamTwoEvents.Count}; " +
        $"existing={teamOneGoals.Count}/{teamTwoGoals.Count}. " +
        "Leaving scorers unchanged (calendar FT remains authoritative).");
}
```

#### Own-goal player team + in-place update

```csharp
int playerTeamId = timelineEvent.IsOwnGoal ? opponentTeamId : creditedTeamId;
TimelinePlayerResolveResult resolveResult = await _playerResolver.ResolveAsync(
    playerTeamId,
    timelineEvent.ExternalPlayerId,
    timelineEvent.PlayerDisplayName,
    cancellationToken);

int minute = Math.Max(0, timelineEvent.Minute);
DateTime timeScored = match.Date.AddMinutes(minute);
int isOwnGoal = timelineEvent.IsOwnGoal ? 1 : 0;

bool changed =
    goal.PlayerId != resolveResult.Player.Id
    || goal.TimeScored != timeScored
    || goal.IsOwnGoal != isOwnGoal;

if (changed)
{
    goal.PlayerId = resolveResult.Player.Id;
    goal.TimeScored = timeScored;
    goal.IsOwnGoal = isOwnGoal;
    _repository.Goal.Update(goal);
}
```

#### Save only when something changed

```csharp
if (goalsUpdated > 0)
{
    await _repository.SaveAsync();
}
```

### Unit tests (`TimelineScorerApplyServiceTests`)

| Test | Asserts |
| --- | --- |
| `ApplyAsync_WhenCountsAlignWithWrongScorers_UpdatesPlayerIdAndMinute` | Mexico-style 2 home goals → PlayerId + minutes |
| `ApplyAsync_WhenAlreadyMatched_IsIdempotent` | Second apply → AlreadyMatched; one SaveAsync |
| `ApplyAsync_WhenCountsDisagree_ThrowsAndDoesNotUpdate` | No SaveAsync / no Resolve |
| `ApplyAsync_WhenHomeMapsToTeamTwo_OrientsEventsCorrectly` | Home events → TeamTwo |
| `ApplyAsync_OwnGoal_ResolvesPlayerOnConcedingTeam` | Resolver gets opponent; IsOwnGoal=1 |
| `ApplyAsync_WhenZeroZero_ReturnsAlreadyMatchedWithoutSave` | Empty timeline + empty goals |
| `ApplyAsync_WhenExternalMatchIdMismatch_Throws` | Guard before writes |
| `ApplyAsync_WhenTeamStatsMissing_Throws` | FT sync prerequisite |
| `ApplyAsync_DoesNotInvokeBetOrKnockoutDependencies` | No bet/knockout deps on apply path |
| `ApplyAsync_WhenSameMinuteGoals_PreservesTimelineOrder` | Same minute keeps Event order; second apply idempotent |

### Roadmap mapping

| Task id | Status | Notes |
| --- | --- | --- |
| `p11-external-stage` | **Done** | [phase-11-external-stage](phase-11-external-stage.md) |
| `p11-timeline-provider` | **Done** | [phase-11-timeline-provider](phase-11-timeline-provider.md) |
| `p11-player-resolve` | **Done** | [phase-11-player-resolve](phase-11-player-resolve.md) |
| `p11-scorer-apply` | **Done** | This page — apply only; wired by Task 5; gate closed (**353**) |
| `p11-sync-wire` | **Done** | SyncResult / SyncFinishedResults wire; suite **361**; [detail](phase-11-sync-wire.md) |

### Related

- [Phase 11 plan](phase-11-planned.md)
- [Task 3 Player resolve](phase-11-player-resolve.md)
- [Task 2 Timeline provider](phase-11-timeline-provider.md)
- [Phase 10 match sync](phase-10-match-sync.md)
- [Changes Review](../changes-review.md)
