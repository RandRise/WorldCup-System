# WorldCup System — Phase 11 Task 6: Scorers-only Backfill

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 11 plan](phase-11-planned.md)

## Phase 11 Task 6 — Real Goalscorers backfill (`p11-backfill`)

Opened **20 Jul 2026**. Checklist: [Roadmap Phase 11](../roadmap.md#phase-11).

> **Success:** **Task 6 complete (20 Jul 2026).** Admin `SyncScorers` / `SyncScorersForWorldCup` + client `match-api` + optional `scripts/sync_scorers_backfill.ps1`. Calendar for finished + orientation only; **no** `ApplyScoreIdempotent`, bets, or knockout. Empty timeline → `Skipped`. Batch hard failures → `Warning` (unlike FT batch). Local Goal counts in DTO. Review gate closed — Bugbot highs fixed; backfill **14** cases; suite **375** passed.

### Review gate

High/critical items that must be fixed **before** testing:

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| SyncScorers must not rewrite FT / resolve bets / advance knockout | High (acceptance) | **Cleared** | No `ApplyScoreIdempotent` / `TryResolveAndAdvance`; `ScoreChanged=false`, `BetsResolved=0` |
| `Applied=true` only for `Applied` / `AlreadyMatched` | High | **Cleared** | Map from `ScorerStatus`; Skipped/Warning → `Applied=false` |
| Empty timeline Goal! list → `Skipped` (not count-mismatch Warning) | High | **Cleared** | `skipWhenNoTimelineGoals: true` |
| Batch hard failures → per-match `Warning` (continue WC run) | High | **Cleared** | Catch → `ScorerStatus=Warning` |
| `OperationCanceledException` not swallowed | High | **Fixed** | Re-throw in `TryApplyTimelineScorersCoreAsync` and `SyncScorersForWorldCup` batch catch |
| DTO scores from calendar vs local Goals | Medium | **Fixed** | `GetLocalGoalCounts` for TeamOne/TeamTwo reporting |
| `BatchDelayMilliseconds` applies to `SyncScorersForWorldCup` | High | **Cleared** | Same delay as FT batch; tests use `0` |
| Bugbot high/critical findings | High (gate) | **Cleared** | Cancel swallow fixed; medium local scores fixed |
| Backfill unit suite | High (gate) | **Cleared** | **14** backfill cases; suite **375** passed |

### Decision — scorers-only path

| Layer | Behavior |
| --- | --- |
| Calendar `FetchResult` | Finished check + `OrientScoresToLocalSides` + `PersistExternalStageIdIfNeeded` |
| FT Goal rewrite | **Never** — no `ApplyScoreIdempotent` |
| Bets / knockout | **Never** — `BetsResolved=0` |
| Timeline / apply | Fail-soft via `TryApplyTimelineScorersForBackfillAsync` |
| No Goal! events on timeline | `ScorerStatus=Skipped` (do not treat empty vs existing Goals as mismatch) |
| Apply / timeline errors | `ScorerStatus=Warning`; rethrow cancel |
| Batch hard throw | Catch → synthetic result with `ScorerStatus=Warning`; continue |

### Deliverables

| Item | Detail | Status |
| --- | --- | --- |
| Interface | `IMatchResultSyncService.SyncScorers` / `SyncScorersForWorldCup` | **Done** |
| Service | Calendar orient only → backfill timeline apply | **Done** |
| Controller | Admin `POST Match/SyncScorers/{matchId}`, `POST Match/SyncScorersForWorldCup?worldCupId=` | **Done** |
| Client | `match-api.service.ts` `syncScorers` / `syncScorersForWorldCup` | **Done** |
| Script | `scripts/sync_scorers_backfill.ps1` optional Admin API caller | **Done** |
| Rate limit | `BatchDelayMilliseconds` on WC batch | **Done** |
| Ops runbook | Map External ids → backfill → verify Recent events | **Done** (this page) |
| Review gate | Bugbot + dedicated tests + suite green | **Closed** |

### Ops runbook

```text
1. Map External ids
   Admin POST Match/SetExternalMatchId
     { matchId, externalMatchId, externalStageId? }
   Or re-run SyncResult once so calendar auto-persists ExternalStageId.

2. Backfill scorers (pick one)
   a) Single:  POST Match/SyncScorers/{matchId}
   b) Batch:   POST Match/SyncScorersForWorldCup?worldCupId={id}
   c) Script:  .\scripts\sync_scorers_backfill.ps1
               .\scripts\sync_scorers_backfill.ps1 -WorldCupId 1
               .\scripts\sync_scorers_backfill.ps1 -MatchId 42
               .\scripts\sync_scorers_backfill.ps1 -ApiBaseUrl http://localhost:5055

3. Verify Recent events
   GET Match/GetLiveSnapshot?worldCupId=…  (or fixtures poll)
   Confirm player names match FIFA Goal! events (not Tournament Scorer / round-robin).
```

Script defaults: `ApiBaseUrl=http://localhost:5055`, `AdminEmail=admin@localhost`, `AdminPassword=Admin123!`. If `-WorldCupId` omitted and `-MatchId` is 0, uses first World Cup from `GetWorldCups`.

### Acceptance

One Admin (or script) run updates scorers for mapped finished matches without changing FT scores or re-awarding bet points. Empty timelines skip cleanly; per-match failures do not abort the World Cup batch.

### Flow

```text
SyncScorers(matchId)
  → require ExternalMatchId + both teams
  → calendar FetchResultAsync
  → PersistExternalStageIdIfNeeded
  → if not finished → return (Applied=false; ScorerStatus null)
  → OrientScoresToLocalSides → (scores, homeMapsToTeamOne)
  → TryApplyTimelineScorersForBackfillAsync (skipWhenNoTimelineGoals: true)
       → skip if no ExternalMatchId / ExternalStageId
       → skip if timeline has zero Goal! events
       → else FetchGoalEventsAsync + ApplyAsync
       → catch → ScorerStatus=Warning (rethrow cancel)
  → Applied = (Applied | AlreadyMatched)
  → ScoreChanged=false; BetsResolved=0
  → return SyncMatchResultDTO

SyncScorersForWorldCup(worldCupId)
  → mapped fixtures (ExternalMatchId set) OrderBy Date, Id
  → per match: SyncScorers (catch hard errors → Warning result)
  → count ScorersApplied / AlreadyMatched / Skipped / ScorerWarnings
  → BatchDelayMilliseconds between attempts (not after last)
  → TotalBetsResolved=0
```

### vs SyncFinishedResults (FT batch)

| Behavior | `SyncFinishedResults` | `SyncScorersForWorldCup` |
| --- | --- | --- |
| FT / bets / knockout | Yes | No |
| Hard failure catch | Per-match result; `ScorerStatus` often **null** | Per-match `ScorerStatus=Warning` |
| Empty timeline | May Warning via count mismatch on wire path | **Skipped** |
| `BatchDelayMilliseconds` | Yes | Yes |

### Git file list (Task 6 — from `git status` / `git diff`)

| Status | Path |
| --- | --- |
| Modified | `Core/Services/MatchSync/IMatchResultSyncService.cs` — `SyncScorers` / `SyncScorersForWorldCup` |
| Modified | `Core/Services/MatchSync/MatchResultSyncService.cs` — backfill methods + `TryApplyTimelineScorersForBackfillAsync` |
| Modified | `WorldCup-System/Controllers/MatchController.cs` — Admin SyncScorers endpoints |
| Modified | `worldcup-client/src/app/core/api/match-api.service.ts` — client callers |
| Untracked | `scripts/sync_scorers_backfill.ps1` — optional Admin API script |
| Shared (prior tasks) | `MatchResultSyncDTO` scorer fields; `BatchDelayMilliseconds` (also documented Task 5) |
| Docs | `docs/changes/phase-11-backfill.md`, planned / roadmap / changes-review / api-status / index |

### Diff snippets

#### Interface

```csharp
Task<SyncMatchResultDTO> SyncScorers(int matchId, CancellationToken cancellationToken = default);

Task<SyncFinishedResultsDTO> SyncScorersForWorldCup(
    int worldCupId,
    CancellationToken cancellationToken = default);
```

#### SyncScorers (no FT / bets)

```csharp
(int teamOneScore, int teamTwoScore, bool homeMapsToTeamOne) =
    OrientScoresToLocalSides(match, external);

(string scorerStatus, int scorerGoalsUpdated, string scorerMessage, string? scorerWarning) =
    await TryApplyTimelineScorersForBackfillAsync(match, homeMapsToTeamOne, cancellationToken);

bool applied =
    scorerStatus == SyncScorerStatuses.Applied
    || scorerStatus == SyncScorerStatuses.AlreadyMatched;

// ScoreChanged = false; BetsResolved = 0
```

#### Backfill skip when no Goal! events

```csharp
private async Task<...> TryApplyTimelineScorersForBackfillAsync(...)
{
    return await TryApplyTimelineScorersCoreAsync(
        match,
        homeMapsToTeamOne,
        skipWhenNoTimelineGoals: true,
        cancellationToken);
}

// in core:
if (skipWhenNoTimelineGoals
    && (events.Goals == null || events.Goals.Count == 0))
{
    return (SyncScorerStatuses.Skipped, 0, "skipped (no timeline Goal! events).", null);
}
```

#### Batch hard failure → Warning

```csharp
catch (Exception ex)
{
    results.Add(new SyncMatchResultDTO
    {
        MatchId = match.Id,
        Applied = false,
        ScoreChanged = false,
        Message = ex.Message,
        ScorerStatus = SyncScorerStatuses.Warning,
        ScorerMessage = "not applied (see message)."
    });
    scorerWarnings++;
}
```

#### Controller

```csharp
[Authorize(Roles = "Admin")]
[HttpPost("{matchId}")]
public async Task<IActionResult> SyncScorers(int matchId, CancellationToken cancellationToken)

[Authorize(Roles = "Admin")]
[HttpPost]
public async Task<IActionResult> SyncScorersForWorldCup(
    [FromQuery] int worldCupId,
    CancellationToken cancellationToken)
```

Routes (controller `[action]`): `POST Match/SyncScorers/{matchId}`, `POST Match/SyncScorersForWorldCup?worldCupId=`.

#### Client (`match-api.service.ts`)

```typescript
syncScorers(matchId: number): Promise<SyncMatchResult> {
  return firstValueFrom(
    this.http.post<SyncMatchResult>(`${this.baseUrl}/Match/SyncScorers/${matchId}`, {}),
  );
}

syncScorersForWorldCup(worldCupId: number): Promise<SyncFinishedResults> {
  const params = new HttpParams().set('worldCupId', String(worldCupId));
  return firstValueFrom(
    this.http.post<SyncFinishedResults>(
      `${this.baseUrl}/Match/SyncScorersForWorldCup`, {}, { params }),
  );
}
```

#### Script (`scripts/sync_scorers_backfill.ps1`)

```powershell
# Usage:
#   .\scripts\sync_scorers_backfill.ps1
#   .\scripts\sync_scorers_backfill.ps1 -WorldCupId 1
#   .\scripts\sync_scorers_backfill.ps1 -MatchId 42

if ($MatchId -gt 0) {
    Invoke-RestMethod -Uri "$ApiBaseUrl/Match/SyncScorers/$MatchId" -Method Post -Headers $headers
} else {
    Invoke-RestMethod `
        -Uri "$ApiBaseUrl/Match/SyncScorersForWorldCup?worldCupId=$WorldCupId" `
        -Method Post -Headers $headers
}
```

### Roadmap mapping

| Task id | Status | Notes |
| --- | --- | --- |
| `p11-external-stage` … `p11-sync-wire` | **Done** (gates closed) | Prior tasks |
| `p11-backfill` | **Done** (gate closed; suite **375**) | This page |
| `p11-recent-events-honesty` … `p11-tests-gate` | Task 7 **Done** (gate closed; **380**); Task 8 Planned | [honesty](phase-11-recent-events-honesty.md) · [plan](phase-11-planned.md) |

### Related

- [Phase 11 plan](phase-11-planned.md)
- [Task 5 Sync wire](phase-11-sync-wire.md)
- [Task 4 Scorer apply](phase-11-scorer-apply.md)
- [API Status — Scorers backfill](../api-status.md#phase-11--real-scorers-in-progress)
- [Changes Review](../changes-review.md)
