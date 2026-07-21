# WorldCup System — Phase 11 Task 3: Player Identity Resolution

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 11 plan](phase-11-planned.md)

## Phase 11 Task 3 — Player identity (`p11-player-resolve`)

Opened **20 Jul 2026**. Checklist: [Roadmap Phase 11](../roadmap.md#phase-11).

> **Success:** **Task 3 complete (20 Jul 2026).** `ITimelinePlayerResolver` + `TimelinePlayerResolver` map FIFA `IdPlayer` / display name → local `Player`. Persist `Player.ExternalPlayerId`. Auto-create named Forward when missing. Never returns `"Tournament Scorer"` when a real name exists. Wired by Task 4 apply / Task 5 SyncResult. Review gate closed — Bugbot highs fixed; resolver **32** cases; suite **342** passed.

### Review gate

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Ambiguous name matches randomly assigned | High | **Cleared** | Throws `InvalidOperationException` |
| Tournament Scorer when real name exists | High | **Cleared** | Eligible squad excludes placeholder by name only |
| Jersey 99 excludes real squad | High (Bugbot) | **Fixed** | Do not filter by number alone |
| FIFA id already on another team | High | **Cleared** | Throws; same-team global reuse |
| Missing migration / startup column | High | **Cleared** | Migration untracked + IF NOT EXISTS (Bugbot missed untracked) |
| Unit tests for resolve order / create / ambiguous | High (gate) | **Cleared** | `TimelinePlayerResolverTests` — 32 cases |
| Bugbot high/critical | High (gate) | **Cleared** | Highs fixed |
| Provider suite green | High (gate) | **Cleared** | Suite **342** passed |
| Not applied to Goal rows | Info | By design | Tasks 4–5 |

### Decisions (from plan)

| Decision | Choice | Why |
| --- | --- | --- |
| Persist `Player.ExternalPlayerId`? | **Yes** | Same pattern as `ExternalMatchId` / `ExternalStageId`; stable re-resolve; backfill on name match |
| Auto-create missing players? | **Yes** | Prefer a named Forward over `"Tournament Scorer"` when timeline gave a real name |

### Match order

1. `ExternalPlayerId` on eligible squad (excludes `"Tournament Scorer"` by **name** only — jersey 99 may be a real player)
2. Exact name (`OrdinalIgnoreCase`)
3. Normalized name (strip diacritics, uppercase letters/digits, collapse separators)
4. Create named Forward with next free jersey **1–98**; set `ExternalPlayerId` when provided

On name match with a new FIFA id → backfill `ExternalPlayerId` + `SaveAsync` (warning on result).

Ambiguous exact/normalized matches → throw (never random).

If FIFA id is already on another team → throw.

Requires at least one of `externalPlayerId` / `playerDisplayName`; both null/whitespace → `ArgumentException`.

Id-only miss (no display name) → throw (cannot create without a name).

### Own goals

Resolver does **not** infer sides. Caller (Task 4) passes the **conceding** team id so the scorer is found/created on that squad — matching `GoalService` own-goal rules (`Player` on conceding side; goal credited to opponent).

### Deliverables

| Item | Detail | Status |
| --- | --- | --- |
| Column | `Player.ExternalPlayerId` — `varchar(64)`, nullable | Done |
| Index | Unique filtered `IX_Player_ExternalPlayerId` | Done |
| Migration | `20260720140000_AddPlayerExternalPlayerId` | Done |
| Startup repair | `IF NOT EXISTS` column + unique index in `Program.cs` | Done |
| Interface | `ITimelinePlayerResolver.ResolveAsync(teamId, externalPlayerId, displayName)` | Done |
| Service | `TimelinePlayerResolver` | Done |
| Result / enum | `TimelinePlayerResolveResult`, `TimelinePlayerMatchMethod` | Done |
| DI | `AddScoped<ITimelinePlayerResolver, TimelinePlayerResolver>` | Done |
| DTO / client | `PlayerDTO.ExternalPlayerId` + `api.models.ts` `externalPlayerId` | Done |
| Mapping | `PlayerService` maps `ExternalPlayerId` onto `PlayerDTO` | Done |
| Tests | Resolve order, normalize, create, ambiguous, no placeholder | Done (32 cases; suite **342**) |

### Acceptance

Given a timeline goal for a known squad forward (name or `ExternalPlayerId`), resolver returns that `Player.Id`. Ambiguous matches throw. Missing name with FIFA id only → throw (need display name to create). Missing name+id with real display name → create Forward. Never returns Tournament Scorer when a real display name is available.

### Git file list (Task 3 — from `git status` / `git diff`)

| Status | Path |
| --- | --- |
| **New** | `Core/Services/MatchSync/ITimelinePlayerResolver.cs` |
| **New** | `Core/Services/MatchSync/TimelinePlayerResolver.cs` |
| **New** | `Core/Services/MatchSync/TimelinePlayerResolveResult.cs` |
| **New** | `Core/Services/MatchSync/TimelinePlayerMatchMethod.cs` |
| **New** | `Data/Migrations/20260720140000_AddPlayerExternalPlayerId.cs` (+ Designer) |
| Modified | `Data/Entities/Player.cs` — `ExternalPlayerId` |
| Modified | `Data/Context/ApplicationDbContext.cs` — max length + unique filtered index |
| Modified | `Data/Migrations/ApplicationDbContextModelSnapshot.cs` — Player `ExternalPlayerId` |
| Modified | `WorldCup-System/Program.cs` — DI + startup `IF NOT EXISTS` |
| Modified | `Core/DTOs/Players/PlayerDTO.cs` |
| Modified | `Core/Services/Players/PlayerService.cs` |
| Modified | `worldcup-client/src/app/core/models/api.models.ts` — `externalPlayerId` |
| Docs | `docs/changes/phase-11-player-resolve.md`, `docs/changes-review.md`, `docs/changes/phase-11-planned.md`, `docs/roadmap.md`, `docs/index.md` |

Task 1–2 files in the same working tree are documented separately: [phase-11-external-stage](phase-11-external-stage.md) · [phase-11-timeline-provider](phase-11-timeline-provider.md).

### Diff snippets (git)

#### Entity — `Player.ExternalPlayerId`

```csharp
/// <summary>FIFA IdPlayer when known (timeline sync). Unique when set.</summary>
public string? ExternalPlayerId { get; set; }
```

#### EF config — unique filtered index

```csharp
e.Property(player => player.ExternalPlayerId)
    .HasMaxLength(64);
e.HasIndex(player => player.ExternalPlayerId)
    .IsUnique()
    .HasFilter("\"ExternalPlayerId\" IS NOT NULL");
```

#### Migration `AddPlayerExternalPlayerId`

```csharp
migrationBuilder.AddColumn<string>(
    name: "ExternalPlayerId",
    table: "Player",
    type: "character varying(64)",
    maxLength: 64,
    nullable: true);

migrationBuilder.CreateIndex(
    name: "IX_Player_ExternalPlayerId",
    table: "Player",
    column: "ExternalPlayerId",
    unique: true,
    filter: "\"ExternalPlayerId\" IS NOT NULL");
```

#### Program.cs — DI + startup repair

```csharp
builder.Services.AddScoped<ITimelinePlayerResolver, TimelinePlayerResolver>();

// startup SQL (excerpt):
ALTER TABLE "Player" ADD COLUMN IF NOT EXISTS "ExternalPlayerId" character varying(64) NULL;
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Player_ExternalPlayerId"
  ON "Player" ("ExternalPlayerId") WHERE "ExternalPlayerId" IS NOT NULL;
```

#### Interface

```csharp
public interface ITimelinePlayerResolver
{
    Task<TimelinePlayerResolveResult> ResolveAsync(
        int teamId,
        string? externalPlayerId,
        string? playerDisplayName,
        CancellationToken cancellationToken = default);
}
```

#### Match method enum

```csharp
public enum TimelinePlayerMatchMethod
{
    ExternalPlayerId = 0,
    ExactName = 1,
    NormalizedName = 2,
    Created = 3
}
```

#### Resolve order (summary)

```csharp
// 1) ExternalPlayerId on eligible squad
// 2) Exact name (OrdinalIgnoreCase)
// 3) Normalized name (diacritics stripped)
// 4) Create Forward + allocate jersey 1–98
// Never MatchResultSyncService.PlaceholderScorerName when display name present
// Ambiguous exact/normalized → InvalidOperationException
```

#### Normalize

```csharp
/// <summary>
/// Uppercase, strip diacritics, keep letters/digits, collapse other runs to single spaces.
/// </summary>
internal static string NormalizePlayerName(string name)
{
    // FormD → drop NonSpacingMark → uppercase letters/digits → collapse other chars to spaces
}
```

#### DTO + client

```csharp
// PlayerDTO.cs
public string? ExternalPlayerId { get; set; }
```

```typescript
// api.models.ts — Player
externalPlayerId?: string | null;
```

### Roadmap mapping

| Task id | Status | Notes |
| --- | --- | --- |
| `p11-external-stage` | **Done** | [phase-11-external-stage](phase-11-external-stage.md) |
| `p11-timeline-provider` | **Done** | [phase-11-timeline-provider](phase-11-timeline-provider.md) |
| `p11-player-resolve` | **Done** | This page — resolve only; no Goal rewrite; gate closed (**342** tests) |
| `p11-scorer-apply` … `p11-tests-gate` | Planned | [phase-11-planned](phase-11-planned.md) |

### Related

- [Phase 11 plan](phase-11-planned.md)
- [Task 2 Timeline provider](phase-11-timeline-provider.md)
- [Phase 10 match sync](phase-10-match-sync.md)
- [Changes Review](../changes-review.md)
