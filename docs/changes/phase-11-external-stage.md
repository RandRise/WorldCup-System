# WorldCup System — Phase 11 Task 1: External Stage Id

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 11 plan](phase-11-planned.md)

## Phase 11 Task 1 — External stage id (`p11-external-stage`)

Opened **20 Jul 2026**. Checklist: [Roadmap Phase 11](../roadmap.md#phase-11).

> **Success:** **Task 1 complete (20 Jul 2026).** Persist `Match.ExternalStageId`, migration + startup repair, optional Admin set, calendar `IdStage` parse, auto-persist on `SyncResult`, DTOs + client model. Review gate closed — **278** tests passed.

### Review gate

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Dedicated `ExternalStageId` unit tests | High | **Cleared** | Provider IdStage + SetExternal set/omit/clear + SyncResult persist + batch refresh; suite **278** |
| Bugbot high/critical findings | High (gate) | **Cleared** | No high/critical; medium batch stale-stage DTO fixed (refresh on SyncFinishedResults error) |
| `AddMatchExternalStageId` migration | Medium | Ops | Apply via `MigrateAsync` + `IF NOT EXISTS` repair on API start |
| Squad round-robin still invents scorers | Medium | Accepted | Prefers seeded forwards over `"Tournament Scorer"`; real identities are Tasks 2–4 |
| Extra `SaveAsync` when stage auto-fills mid-sync | Low | Accepted v1 | Stage persist before score apply; revisit if batch sync needs single transaction |

### Decision

**Persist** `Match.ExternalStageId` (same pattern as `ExternalMatchId`), not calendar re-fetch on every timeline call.

| Approach | Chosen? | Why |
| --- | --- | --- |
| Column `ExternalStageId` | Yes | One write at map/sync time; timeline URLs need stage without an extra calendar round-trip |
| Resolve stage only via calendar re-fetch | No (as primary) | Extra FIFA call per match; calendar remains the source that *fills* the column |

### Deliverables

| Item | Detail | Status |
| --- | --- | --- |
| Column | `Match.ExternalStageId` — `varchar(64)`, nullable | Done |
| Migration | `20260720120000_AddMatchExternalStageId` (+ snapshot) | Done (untracked until commit) |
| Startup repair | `ALTER TABLE "Match" ADD COLUMN IF NOT EXISTS "ExternalStageId" ...` in `Program.cs` | Done |
| Admin map | `SetExternalMatchIdDTO.ExternalStageId` optional — omit = keep; blank/whitespace = clear | Done |
| Calendar | `FifaCalendarMatchResultProvider` parses `IdStage` → `ExternalMatchResult.ExternalStageId` | Done |
| Auto-fill | `MatchResultSyncService.PersistExternalStageIdIfNeededAsync` on every `SyncResult` | Done |
| DTOs / client | `MatchDTO`, sync DTOs, `api.models.ts` (`externalStageId`) | Done |
| Config | Timeline path reuses `MatchResultSync:IdCompetition` / `IdSeason` (documented on options) | Done |

### Timeline URL shape (ready for Task 2)

```text
GET https://api.fifa.com/api/v3/timelines/{IdCompetition}/{IdSeason}/{ExternalStageId}/{ExternalMatchId}?language=en
```

- `IdCompetition` / `IdSeason` / `language` → `MatchResultSync` options
- `ExternalStageId` / `ExternalMatchId` → columns on `Match`

### Acceptance

Sync code can build a timeline URL for every mapped finished match that has `ExternalStageId` filled (via Admin set or SyncResult calendar auto-fill) without hard-coding stage ids.

### SetExternalMatchId semantics

| Body `externalStageId` | Effect on `Match.ExternalStageId` |
| --- | --- |
| Omitted / `null` | Existing value kept |
| Non-empty string | Trimmed and stored |
| Blank / whitespace | Cleared to `null` |

`externalMatchId` remains required (unchanged from Phase 10).

### SyncResult stage auto-persist

After calendar fetch, before finished/score logic:

1. If provider returns blank `ExternalStageId` → no-op.
2. If match already has the same stage (ordinal) → no-op.
3. Else update column + `SaveAsync`.

Returned `SyncMatchResultDTO` / batch items include `ExternalStageId` from the match entity after persist.

### Accompanying change (same service diff)

When score counts change, goal attribution prefers seeded squad forwards (then any non-placeholder player), cycling by shirt number; falls back to `"Tournament Scorer"` / `99` only when the squad has no real players. This improves Recent events readability but is **not** real FIFA timeline scorers (Tasks 2–4). Admin live console also filters placeholder players from goal/card pickers.

### Files touched (Task 1 — from `git status` / `git diff`)

| Area | Paths |
| --- | --- |
| Entity / EF | `Data/Entities/Match.cs`, `Data/Context/ApplicationDbContext.cs`, `Data/Migrations/ApplicationDbContextModelSnapshot.cs` |
| Migration (new) | `Data/Migrations/20260720120000_AddMatchExternalStageId.cs` (+ `.Designer.cs`) |
| Startup | `WorldCup-System/Program.cs` |
| Options | `Core/Options/MatchResultSyncOptions.cs` (timeline path comment) |
| Provider | `Core/Services/MatchSync/FifaCalendarMatchResultProvider.cs`, `ExternalMatchResult.cs` |
| Sync | `Core/Services/MatchSync/MatchResultSyncService.cs` |
| DTOs / map | `Core/DTOs/Matches/MatchDTO.cs`, `MatchResultSyncDTO.cs`, `Core/Services/Matches/MatchService.cs` |
| Client | `worldcup-client/src/app/core/models/api.models.ts` |
| Related UX | `worldcup-client/.../admin-live-console.component.ts` (exclude Tournament Scorer) |
| Tests (partial) | `WorldCup-System.Tests/.../MatchResultSyncServiceTests.cs` — squad-scorer cases; **stage-id cases still open** |

### Diff snippets (git)

#### Match entity

```csharp
/// <summary>External provider stage id (e.g. FIFA IdStage) for timeline URLs.</summary>
public string? ExternalStageId { get; set; }
```

#### Migration `Up`

```csharp
migrationBuilder.AddColumn<string>(
    name: "ExternalStageId",
    table: "Match",
    type: "character varying(64)",
    maxLength: 64,
    nullable: true);
```

#### Calendar parse

```csharp
ExternalStageId = string.IsNullOrWhiteSpace(match.IdStage) ? null : match.IdStage.Trim(),
// ...
[JsonPropertyName("IdStage")]
public string? IdStage { get; set; }
```

#### SetExternal optional stage

```csharp
if (request.ExternalStageId != null)
{
    string trimmedStageId = request.ExternalStageId.Trim();
    match.ExternalStageId = string.IsNullOrWhiteSpace(trimmedStageId) ? null : trimmedStageId;
}
```

#### Options documentation

```csharp
/// Timeline path: timelines/{IdCompetition}/{IdSeason}/{ExternalStageId}/{ExternalMatchId}.
public string IdCompetition { get; set; } = "17";
public string IdSeason { get; set; } = "285023";
```

### Roadmap mapping

| Task id | Status | Notes |
| --- | --- | --- |
| `p11-external-stage` | **Done** | Column + migration + SetExternal + IdStage + auto-fill |
| `p11-timeline-provider` | **Done** (gate) | [phase-11-timeline-provider](phase-11-timeline-provider.md) |
| `p11-player-resolve` … `p11-tests-gate` | Planned | [phase-11-planned](phase-11-planned.md) |

### Related

- [Phase 11 plan](phase-11-planned.md)
- [Phase 10 match sync](phase-10-match-sync.md)
- [API Status](../api-status.md)
- [Changes Review](../changes-review.md)
