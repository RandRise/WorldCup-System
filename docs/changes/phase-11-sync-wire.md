# WorldCup System — Phase 11 Task 5: Wire Sync APIs

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 11 plan](phase-11-planned.md)

## Phase 11 Task 5 — Wire sync APIs (`p11-sync-wire`)

Opened **20 Jul 2026**. Checklist: [Roadmap Phase 11](../roadmap.md#phase-11).

> **Success:** **Task 5 complete (20 Jul 2026).** `SyncResult` / `SyncFinishedResults` call timeline + scorer apply after calendar FT (fail-soft). DTO carries `ScorerStatus` / `ScorerMessage` / `ScorerGoalsUpdated`. Batch continues on per-match scorer warnings; optional `BatchDelayMilliseconds`. Client models updated. Review gate closed — Bugbot found no bugs; wiring **8** cases; suite **361** passed.

### Review gate

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Calendar FT still succeeds when timeline/apply throws | High (acceptance) | **Cleared** | `TryApplyTimelineScorersAsync` catches; sets `ScorerStatus=Warning`; FT `Applied` stays true |
| Missing `ExternalStageId` does not abort FT sync | High | **Cleared** | `Skipped` + message; no timeline HTTP |
| Batch continues when one match has scorer warning | High | **Cleared** | Scorer errors stay inside `SyncResult`; hard FT failure still caught per match |
| Orientation passed to apply (`homeMapsToTeamOne`) | High | **Cleared** | Same calendar orientation tuple from `OrientScoresToLocalSides` |
| `OperationCanceledException` not swallowed | High | **Cleared** | Re-thrown from fail-soft catch |
| Bugbot high/critical | High (gate) | **Cleared** | No bugs found |
| Wiring unit suite green | High (gate) | **Cleared** | **8** wiring cases; suite **361** passed |

### Decision — fail soft at sync layer

| Layer | Behavior |
| --- | --- |
| Calendar FT + bets + knockout | Unchanged; hard failures still throw / abort that match |
| Timeline fetch / scorer apply | Catch → `ScorerStatus=Warning`; FT `Applied` stays true |
| Missing stage id / match id | `ScorerStatus=Skipped` (no timeline HTTP) |
| Apply count mismatch | Apply throws → sync maps to `Warning` (goals unchanged) |

### Deliverables

| Item | Detail | Status |
| --- | --- | --- |
| Wire | After FT apply (or score already matched) + bets/advance, call `IExternalMatchEventsProvider` + `ITimelineScorerApplyService` | Done |
| DTO | `ScorerStatus`, `ScorerGoalsUpdated`, `ScorerMessage` on `SyncMatchResultDTO`; `SyncScorerStatuses` constants | Done |
| Batch | `ScorersApplied` / `ScorerWarnings` counts; scorer warnings do not abort WC run | Done |
| Rate limit | `MatchResultSync:BatchDelayMilliseconds` (default 250; tests use 0) | Done |
| Client | `SyncMatchResult` / `SyncFinishedResults` fields in `api.models.ts` | Done |
| Tests | Skip / apply / warning / orientation / batch / apply-throw / cancel / neither | Done (**8** wiring; suite **361**) |
| Review gate | Bugbot + docs + suite green | **Closed** |

### Acceptance

Admin `SyncResult` on a mapped finished match with `ExternalStageId` yields scorers applied (or warning) in the sync message; Recent events show real names after a successful apply. Calendar FT still applies when timeline fails.

### Flow

```text
SyncResult(matchId)
  → calendar FetchResultAsync
  → PersistExternalStageIdIfNeeded
  → if not finished → return (no scorers; ScorerStatus null)
  → OrientScoresToLocalSides → (scores, homeMapsToTeamOne)
  → ApplyScoreIdempotentAsync
  → TryResolveAndAdvance (bets + knockout)
  → TryApplyTimelineScorersAsync (fail-soft)
       → skip if no ExternalMatchId / ExternalStageId
       → else FetchGoalEventsAsync + ApplyAsync
       → catch → ScorerStatus=Warning (rethrow cancel)
  → return SyncMatchResultDTO (+ Scorer* + merged Warning)

SyncFinishedResults(worldCupId)
  → mapped matches OrderBy Date, Id
  → per match: SyncResult (catch hard FT errors)
  → count ScorersApplied / ScorerWarnings
  → BatchDelayMilliseconds between attempts (not after last)
```

### ScorerStatus values

| Value | Meaning |
| --- | --- |
| `Applied` | At least one Goal row updated (`SyncScorerStatuses.Applied`) |
| `AlreadyMatched` | Counts aligned; PlayerId/minute/OG already correct |
| `Skipped` | Not attempted (no stage id, etc.) or apply returned non-applied/non-matched |
| `Warning` | Timeline/apply threw; FT sync still succeeded |
| `null` | Scorers not attempted (e.g. external not finished) |

### Config (`MatchResultSync`)

| Key | Default | Role |
| --- | --- | --- |
| `TimelineBaseUrl` | `https://api.fifa.com/api/v3/timelines` | Timeline HttpClient (Task 2) |
| `BatchDelayMilliseconds` | `250` | Pause between batch matches; `0` disables (tests) |

### Git file list (Task 5 — from `git diff` / `git status`)

| Status | Path |
| --- | --- |
| Modified | `Core/Services/MatchSync/MatchResultSyncService.cs` — inject events + apply + options; `TryApplyTimelineScorersAsync`; batch delay + scorer counts |
| Modified | `Core/DTOs/Matches/MatchResultSyncDTO.cs` — `SyncScorerStatuses`; scorer fields on `SyncMatchResultDTO`; batch counts |
| Modified | `Core/Options/MatchResultSyncOptions.cs` — `BatchDelayMilliseconds` (+ `TimelineBaseUrl` from Task 2) |
| Modified | `WorldCup-System/appsettings.json` — `BatchDelayMilliseconds`: 250 |
| Modified | `WorldCup-System.Tests/Services/MatchSync/MatchResultSyncServiceTests.cs` — Moq deps + **8** wiring cases (`BatchDelayMilliseconds = 0`) |
| Modified | `worldcup-client/src/app/core/models/api.models.ts` — client sync scorer fields |
| Docs | `docs/changes/phase-11-sync-wire.md`, planned / roadmap / changes-review / api-status / index |

### Diff snippets

#### Constructor DI

```csharp
public MatchResultSyncService(
    IRepositoryManager repository,
    IExternalMatchResultProvider resultProvider,
    IExternalMatchEventsProvider eventsProvider,
    ITimelineScorerApplyService scorerApplyService,
    IBetService betService,
    IKnockoutService knockoutService,
    IMatchService matchService,
    IOptions<MatchResultSyncOptions> options)
```

#### SyncMatchResultDTO (scorer fields)

```csharp
public static class SyncScorerStatuses
{
    public const string Applied = "Applied";
    public const string AlreadyMatched = "AlreadyMatched";
    public const string Skipped = "Skipped";
    public const string Warning = "Warning";
}

// on SyncMatchResultDTO:
public string? ScorerStatus { get; set; }
public int ScorerGoalsUpdated { get; set; }
public string? ScorerMessage { get; set; }

// on SyncFinishedResultsDTO:
public int ScorersApplied { get; set; }
public int ScorerWarnings { get; set; }
```

#### Fail-soft catch

```csharp
catch (OperationCanceledException)
{
    throw;
}
catch (Exception ex)
{
    return (
        SyncScorerStatuses.Warning,
        0,
        "not applied (see warning).",
        $"Scorer sync warning: {ex.Message}");
}
```

#### Batch delay + counts

```csharp
if (result.ScorerStatus == SyncScorerStatuses.Applied)
{
    scorersApplied++;
}
else if (result.ScorerStatus == SyncScorerStatuses.Warning)
{
    scorerWarnings++;
}

if (_options.BatchDelayMilliseconds > 0 && index < mappedMatches.Count - 1)
{
    await Task.Delay(_options.BatchDelayMilliseconds, cancellationToken);
}
```

#### Client (`api.models.ts`)

```typescript
export interface SyncMatchResult {
  // ...
  scorerStatus?: string | null;
  scorerGoalsUpdated?: number;
  scorerMessage?: string | null;
}

export interface SyncFinishedResults {
  // ...
  scorersApplied?: number;
  scorerWarnings?: number;
}
```

### Unit tests (wiring)

| Test | Asserts |
| --- | --- |
| `SyncResult_WhenNoExternalStageId_SkipsScorersWithoutCallingTimeline` | `Skipped`; no HTTP/apply |
| `SyncResult_WhenStagePresent_AppliesTimelineScorersAfterScore` | `Applied`; goals updated; message |
| `SyncResult_WhenTimelineFails_StillAppliesScoreAndReportsScorerWarning` | FT `Applied` + `Warning` |
| `SyncResult_WhenFifaHomeAwayReversed_PassesHomeMapsToTeamOneFalse` | Orientation to apply |
| `SyncFinishedResults_CountsScorerAppliedAndWarningsWithoutAbortingBatch` | Counts; batch continues |
| `SyncResult_WhenApplyThrows_StillAppliesScoreAndReportsScorerWarning` | Apply throw → `Warning`; FT `Applied` |
| `SyncResult_WhenScorerApplyCanceled_RethrowsOperationCanceledException` | Cancel not swallowed |
| `SyncResult_WhenApplyReturnsNeitherAppliedNorMatched_ReportsSkipped` | Status → `Skipped` |

### Roadmap mapping

| Task id | Status | Notes |
| --- | --- | --- |
| `p11-external-stage` … `p11-scorer-apply` | **Done** (gates closed) | Prior tasks |
| `p11-sync-wire` | **Done** (gate closed; suite **361**) | This page |
| `p11-backfill` … `p11-tests-gate` | Planned | [phase-11-planned](phase-11-planned.md) |

### Related

- [Phase 11 plan](phase-11-planned.md)
- [Task 4 Scorer apply](phase-11-scorer-apply.md)
- [Task 2 Timeline provider](phase-11-timeline-provider.md)
- [Phase 10 match sync](phase-10-match-sync.md)
- [API Status — Sync DTO fields](../api-status.md#sync-result-dto-fields-phase-11-task-5)
- [Changes Review](../changes-review.md)
