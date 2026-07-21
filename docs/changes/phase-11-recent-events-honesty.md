# WorldCup System — Phase 11 Task 7: Recent Events Honesty

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 11 plan](phase-11-planned.md)

## Phase 11 Task 7 — Recent events honesty (`p11-recent-events-honesty`)

Opened **21 Jul 2026**. Checklist: [Roadmap Phase 11](../roadmap.md#phase-11).

> **Success:** **Task 7 complete (21 Jul 2026).** `GetLiveSnapshot` and match-detail goal/card DTOs omit `"Tournament Scorer"` via `MatchService.ToHonestPlayerName`. Fixtures Recent events + Admin live Event timeline sanitize the same way (`placeholder-scorer.ts`). Jersey **99 alone** is not filtered for display (real squad players may wear 99; name-only rule matches Task 3). Prefer Tasks 4–6 data fix; this is a UI/API safety net. Review gate closed — Bugbot clean; honesty **3** cases; suite **380** passed.

### Review gate

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Recent events never show literal `Tournament Scorer` | High (acceptance) | **Cleared** | API nulls `PlayerName`; fixtures `@if (event.playerName)` skips empty |
| Match detail / admin live timeline same rule | High (acceptance) | **Cleared** | `BuildTeamStatsDto` + `allEvents()` labels |
| Filter jersey 99 only (hide real #99 players) | Medium | **Rejected** for display | Name-only; aligns with `TimelinePlayerResolver` |
| Real names still shown | High | **Cleared** | Only exact `"Tournament Scorer"` omitted |
| Bugbot high/critical findings | High (gate) | **Cleared** | Bugbot found no bugs |
| Unit suite green | High (gate) | **Cleared** | Honesty **3** + suite **380** |

### Decision — omit name, keep event

| Choice | Why |
| --- | --- |
| **Null `PlayerName`** (keep Goal/Card event) | Scores and event type remain useful; do not invent alternate names |
| **Name-only** (not jersey 99) | Jersey 99 may be a real player; placeholder is identified by `"Tournament Scorer"` |
| **API + client** | API is source of truth for all consumers; client is defense in depth |

### Deliverables

| Item | Detail | Status |
| --- | --- | --- |
| API | `MatchService.ToHonestPlayerName` on `GetLiveSnapshot` + `BuildTeamStatsDto` | **Done** |
| Constant | Reuse `MatchResultSyncService.PlaceholderScorerName` (`"Tournament Scorer"`) | **Done** |
| Client util | `placeholder-scorer.ts` — `PLACEHOLDER_SCORER_NAME` / `toHonestPlayerName` | **Done** |
| Fixtures | Sanitize `recentEvents` on snapshot apply | **Done** |
| Admin live | Timeline labels omit placeholder; picker uses shared constant (+ jersey 99 for picker only) | **Done** |
| Review gate | Bugbot + dedicated tests + suite green | **Closed** |

### Acceptance

Fixtures Recent events never display the literal string `Tournament Scorer` as if it were a real player. Team + event type (and minute) still appear.

### Flow

```text
GetLiveSnapshot / GetMatchById
  → resolve Player.Name for Goal/Card
  → ToHonestPlayerName(name)
       → null if blank or "Tournament Scorer"
       → else original name
  → DTO PlayerName null → UI shows "Team — Goal" only

Fixtures applyLiveSnapshot
  → map recentEvents through toHonestPlayerName (defense)

Admin live allEvents()
  → goal label: honestName ?? "Goal" (+ OG)
  → card label: type only when name omitted
```

### Git status (Task 7 focus)

From `WorldCup-System` working tree (`git status --short`):

| Status | Path |
| --- | --- |
| Modified | `Core/Services/Matches/MatchService.cs` |
| **Untracked (new)** | `worldcup-client/src/app/core/utils/placeholder-scorer.ts` |
| Modified | `worldcup-client/src/app/features/fixtures/fixtures.component.ts` |
| Modified | `worldcup-client/src/app/features/admin/live/admin-live-console.component.ts` |

Same `MatchService.cs` diff also includes `ExternalStageId` on match DTOs (Phase 11 Task 1 surface; not honesty-specific).

### Diff detail (from `git diff`)

#### API — `MatchService.cs`

`using Core.Services.MatchSync;` added so the helper can reference `MatchResultSyncService.PlaceholderScorerName`.

**Live snapshot** — goal and card recent events:

```csharp
PlayerName = ToHonestPlayerName(
    players.FirstOrDefault(player => player.Id == goal.PlayerId)?.Name),
// …
PlayerName = ToHonestPlayerName(
    players.FirstOrDefault(player => player.Id == card.PlayerId)?.Name),
```

**Match detail** — `BuildTeamStatsDto` goals and cards:

```csharp
PlayerName = ToHonestPlayerName(
    players.FirstOrDefault(player => player.Id == goal.PlayerId)?.Name),
// …
PlayerName = ToHonestPlayerName(
    players.FirstOrDefault(player => player.Id == card.PlayerId)?.Name),
```

**Helper:**

```csharp
/// <summary>
/// Omits import/sync placeholder names so Recent events and match timelines
/// never present "Tournament Scorer" as a real player (Phase 11 Task 7).
/// </summary>
private static string? ToHonestPlayerName(string? playerName)
{
    if (string.IsNullOrWhiteSpace(playerName))
    {
        return null;
    }

    if (string.Equals(
            playerName.Trim(),
            MatchResultSyncService.PlaceholderScorerName,
            StringComparison.Ordinal))
    {
        return null;
    }

    return playerName;
}
```

#### Client util — `placeholder-scorer.ts` (new)

```typescript
export const PLACEHOLDER_SCORER_NAME = 'Tournament Scorer';

export function toHonestPlayerName(name?: string | null): string | null {
  if (name == null) {
    return null;
  }
  const trimmed = name.trim();
  if (!trimmed || trimmed === PLACEHOLDER_SCORER_NAME) {
    return null;
  }
  return name;
}
```

#### Fixtures — `fixtures.component.ts`

```typescript
import { toHonestPlayerName } from '../../core/utils/placeholder-scorer';

// applyLiveSnapshot:
this.recentEvents.set(
  snapshot.recentEvents.map((event) => ({
    ...event,
    playerName: toHonestPlayerName(event.playerName),
  })),
);
```

#### Admin live — `admin-live-console.component.ts`

- Import `PLACEHOLDER_SCORER_NAME` + `toHonestPlayerName`.
- Default goal/card `playerId` uses `playersForTeam(...)` (excludes placeholder).
- `playersForTeam`: filter `player.name !== PLACEHOLDER_SCORER_NAME && player.number !== 99` (picker UX; display honesty remains name-only).
- `allEvents()`: goal label `${honestName ?? 'Goal'}${… OG}`; card label type-only when name omitted.

```typescript
const honestName = toHonestPlayerName(goal.playerName);
label: `${honestName ?? 'Goal'}${goal.isOwnGoal ? ' (OG)' : ''}`,
// …
label: honestName ? `${card.type} — ${honestName}` : card.type,
```

### Roadmap mapping

| Task id | Status | Notes |
| --- | --- | --- |
| `p11-external-stage` … `p11-backfill` | **Done** | Tasks 1–6; gates closed; suite **375** |
| `p11-recent-events-honesty` | **Done** | This page — gate closed (**380**) |
| `p11-tests-gate` | Planned | Phase close |

### Related

- [Phase 11 plan](phase-11-planned.md)
- [Task 6 Backfill](phase-11-backfill.md)
- [Task 3 Player resolve](phase-11-player-resolve.md) — name-only placeholder rule
- [Changes Review](../changes-review.md)
- [API Status](../api-status.md)
