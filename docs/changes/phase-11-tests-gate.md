# WorldCup System — Phase 11 Task 8: Tests & Review Gate

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 11 plan](phase-11-planned.md) · [Task 7 honesty](phase-11-recent-events-honesty.md)

## Phase 11 Task 8 — Tests & review gate (`p11-tests-gate`)

Opened **21 Jul 2026**. Checklist: [Roadmap Phase 11](../roadmap.md#phase-11).

> **Success:** **Task 8 / Phase 11 Done (21 Jul 2026).** Formal phase close. Bugbot highs fixed; regression tests **+2**; suite **382** passed. Phase 11 Tasks 1–8 complete.

### Review gate

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Bugbot high/critical findings (phase-wide) | High (gate) | **Cleared** | Two highs fixed (see below) |
| Markdown docs (this page + planned / roadmap / changes-review / api-status / index) | High (gate) | **Cleared** | Phase marked Done |
| Unit coverage for provider / resolver / apply / sync wire / backfill / honesty | High (gate) | **Cleared** | Existing inventory + **+2** regressions |
| `dotnet test` WorldCup-System.Tests green | High (gate) | **Cleared** | **382** passed (0 failed) |
| Phase 11 roadmap Task 8 checkbox + phase **[Done]** | High (acceptance) | **Cleared** | Flipped 21 Jul 2026 |
| No advance to “next phase” work that depends on Phase 11 Done | High (process) | **Cleared** | Gate closed |

### Bugbot summary (Task 8)

| Severity | Location | Finding | Resolution |
| --- | --- | --- | --- |
| High | `MatchResultSyncService.cs` SyncFinishedResults | Batch catch swallowed `OperationCanceledException` | Re-throw before general `Exception` catch (same as SyncScorersForWorldCup) |
| High | `MatchResultSyncService.cs` GetSquadScorers | Excluded jersey **99**, not only `"Tournament Scorer"` by name | Name-only filter (aligns with `TimelinePlayerResolver`) |

### Suite count

| Metric | Value | Notes |
| --- | --- | --- |
| Last closed baseline (Task 7) | **380** | Honesty gate |
| Task 8 confirmed `dotnet test` | **382** | +2 regressions for Bugbot highs |
| Bugbot summary | **2 highs fixed** | Cancel rethrow + jersey-99 name-only |

```powershell
cd c:\Users\Rand-\Documents\WorldCup-System\WorldCup-System
dotnet test WorldCup-System.Tests\WorldCup-System.Tests.csproj
```

### Deliverables checklist

- [x] Tasks 1–7 implemented and individually gated (suite **380** at Task 7)
- [x] Phase 11 detail pages for Tasks 1–7 + [planned](phase-11-planned.md)
- [x] This Task 8 gate page created (`phase-11-tests-gate.md`)
- [x] Bugbot Phase 11 formal review — **no unfixed high/critical**
- [x] Markdown docs review closed (Status column → Done on planned/roadmap)
- [x] `dotnet test` green — suite **382**
- [x] [api-status](../api-status.md) Phase 11 section marked complete
- [x] [roadmap](../roadmap.md#phase-11) Task 8 `[x]` + phase header **[Done]**
- [x] [changes-review](../changes-review.md) Success banner for Phase 11 Done

### Locked scope (Tasks 1–7 already shipped)

| Task | Id | Outcome |
| --- | --- | --- |
| 1 | `p11-external-stage` | `Match.ExternalStageId` + calendar auto-fill |
| 2 | `p11-timeline-provider` | `FifaTimelineEventsProvider` Goal!/OG/Penalty |
| 3 | `p11-player-resolve` | `TimelinePlayerResolver` + `Player.ExternalPlayerId` |
| 4 | `p11-scorer-apply` | `TimelineScorerApplyService` in-place Goal rewrite |
| 5 | `p11-sync-wire` | SyncResult / SyncFinishedResults fail-soft scorers |
| 6 | `p11-backfill` | `SyncScorers` / `SyncScorersForWorldCup` + script |
| 7 | `p11-recent-events-honesty` | Omit `"Tournament Scorer"` on snapshot / UI |

### Test inventory

| File | ~Cases | Focus |
| --- | --- | --- |
| `FifaTimelineEventsProviderTests.cs` | 18 | Timeline JSON parse / minute / Goal! |
| `TimelinePlayerResolverTests.cs` | 24 | External id / name / create / ambiguity |
| `TimelineScorerApplyServiceTests.cs` | 10 | Count align / mismatch / OG / idempotency |
| `MatchResultSyncServiceTests.cs` | **38** | Sync wire + SyncScorers + Task 8 cancel / jersey-99 |
| `MatchServiceTests.cs` | 27 | Includes honesty / DTO surfaces |
| `MatchControllerTests.cs` | (modified) | Admin SyncScorers endpoints |

Task 8 regressions:

- `SyncFinishedResults_WhenCanceled_RethrowsOperationCanceledException`
- `SyncResult_WhenRealForwardWearsJersey99_UsesThatPlayerNotPlaceholder`

### Acceptance (phase Done)

All true as of **21 Jul 2026**:

1. No unfixed Bugbot **high** or **critical** findings for the Phase 11 diff.
2. `dotnet test` on `WorldCup-System.Tests` exits 0; suite **382**.
3. Roadmap Task 8 checked; Phase 11 header **[Done]**; changes-review Success banner posted.

### Related

- [Phase 11 plan](phase-11-planned.md)
- [Task 7 — Recent events honesty](phase-11-recent-events-honesty.md)
- [Task 6 — Backfill](phase-11-backfill.md)
- [Task 5 — Sync wire](phase-11-sync-wire.md)
- [Roadmap Phase 11](../roadmap.md#phase-11)
- [API Status — Phase 11](../api-status.md)
- [Changes Review](../changes-review.md)
