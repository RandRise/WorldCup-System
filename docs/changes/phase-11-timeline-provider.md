# WorldCup System — Phase 11 Task 2: FIFA Timeline Provider

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 11 plan](phase-11-planned.md)

## Phase 11 Task 2 — FIFA timeline provider (`p11-timeline-provider`)

Opened **20 Jul 2026**. Checklist: [Roadmap Phase 11](../roadmap.md#phase-11).

> **Success:** **Task 2 complete (20 Jul 2026).** `IExternalMatchEventsProvider` + `FifaTimelineEventsProvider` parse Goal! / Own Goal / Penalty Goal; minute normalization; HttpClient + `TimelineBaseUrl`. Wired into SyncResult by Task 5. Review gate closed — Bugbot highs fixed; timeline **32** cases; suite **310** passed.

### Review gate

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Mexico–SA fixture parse (Quiñones 9' / Jiménez 67') | High (acceptance) | **Cleared** | `FifaTimelineEventsProviderTests` |
| Goal! without HomeGoals/AwayGoals delta | High | **Fixed** | Skip when FT score unchanged |
| Empty payload / missing Event array | Medium | **Fixed** | Require `Event`; IdMatch mismatch throws |
| Bugbot high/critical findings | High (gate) | **Cleared** | High fixed |
| Provider unit suite green | High (gate) | **Cleared** | 32 timeline + **310** suite |
| Own Goal / Penalty labels | Medium | Accepted v1 | Common TypeLocalized strings |
| Side inference without HomeOrAway | Medium | Accepted v1 | Infer from score delta |
| Not applied to DB at Task 2 ship | Info | **Cleared** | Tasks 3–5 wired resolve + apply + SyncResult |

### Decision — fail soft at sync layer

| Layer | Behavior |
| --- | --- |
| `FifaTimelineEventsProvider` | Throws `InvalidOperationException` / `ArgumentException` on hard failures (same as calendar) |
| Future `SyncResult` scorer step (Task 5) | Catch and report warning; calendar FT apply must still succeed |

### Deliverables

| Item | Detail | Status |
| --- | --- | --- |
| Interface | `IExternalMatchEventsProvider.FetchGoalEventsAsync(matchId, stageId)` | Done |
| DTOs | `ExternalMatchEvents`, `ExternalMatchGoalEvent` | Done |
| Provider | `FifaTimelineEventsProvider` (~296 lines) | Done |
| Options | `MatchResultSync:TimelineBaseUrl` (+ appsettings) | Done |
| DI | `AddHttpClient<IExternalMatchEventsProvider, FifaTimelineEventsProvider>` | Done |
| Parse types | `Goal!`, `Own Goal` / `Own Goal!`, `Penalty Goal` / `Penalty!` | Done |
| Minute | `9'` → 9; `90'+2'` → 92 | Done |
| Tests | Mexico–SA + HTTP fail + stage required + non-goals + OG/penalty + minute/name theories | Done (gate: `dotnet test`) |

### URL shape

```text
GET {TimelineBaseUrl}/{IdCompetition}/{IdSeason}/{ExternalStageId}/{ExternalMatchId}?language={Language}
```

Defaults: `https://api.fifa.com/api/v3/timelines/17/285023/{stage}/{match}?language=en`.

Requires Task 1 `Match.ExternalStageId` (and `ExternalMatchId`) on the row.

### Goal event mapping

| FIFA field | Mapped to |
| --- | --- |
| `IdPlayer` | `ExternalPlayerId` |
| `IdTeam` | `ExternalTeamId` |
| `EventDescription` → name before `(` | `PlayerDisplayName` |
| `MatchMinute` | `Minute` (integer) |
| `HomeGoals`/`AwayGoals` delta | `IsHomeSide` |
| `TypeLocalized` Own Goal* | `IsOwnGoal` |
| `TypeLocalized` Penalty* | `IsPenalty` |

Non-goal events (`Attempt at Goal`, cards, etc.) are ignored.

Ambiguous score delta: regular goals default home; own goals without a clear delta default away (avoid inventing home credit).

### Acceptance

Unit test with recorded JSON: Mexico–South Africa (`IdMatch` `400021443`, `IdStage` `289273`) yields two `Goal!` events — Quiñones / Jiménez, minutes 9 / 67. Request path contains `17/285023/289273/400021443` and `language=en`.

### Git file list (Task 2 — from `git status` / `git diff`)

| Status | Path |
| --- | --- |
| **New** | `Core/Services/MatchSync/FifaTimelineEventsProvider.cs` |
| **New** | `Core/Services/MatchSync/IExternalMatchEventsProvider.cs` |
| **New** | `Core/Services/MatchSync/ExternalMatchEvents.cs` |
| **New** | `Core/Services/MatchSync/ExternalMatchGoalEvent.cs` |
| **New** | `WorldCup-System.Tests/Services/MatchSync/FifaTimelineEventsProviderTests.cs` |
| Modified | `Core/Options/MatchResultSyncOptions.cs` — `TimelineBaseUrl` + path docs |
| Modified | `WorldCup-System/Program.cs` — `AddHttpClient` for timeline provider |
| Modified | `WorldCup-System/appsettings.json` — `MatchResultSync:TimelineBaseUrl` |

Task 1 files in the same working tree (`ExternalStageId`, calendar `IdStage`, SyncResult persist) are documented separately: [phase-11-external-stage](phase-11-external-stage.md).

### Diff snippets (git)

#### Options — `TimelineBaseUrl`

```csharp
/// <summary>
/// FIFA timeline API root. Path appended:
/// {IdCompetition}/{IdSeason}/{ExternalStageId}/{ExternalMatchId}?language=
/// </summary>
public string TimelineBaseUrl { get; set; } = "https://api.fifa.com/api/v3/timelines";
```

#### appsettings

```json
"MatchResultSync": {
  "Provider": "FifaCalendar",
  "BaseUrl": "https://api.fifa.com/api/v3/calendar/matches",
  "TimelineBaseUrl": "https://api.fifa.com/api/v3/timelines",
  "IdCompetition": "17",
  "IdSeason": "285023",
  "Language": "en"
}
```

#### Program.cs — HttpClient registration

```csharp
builder.Services.AddHttpClient<IExternalMatchEventsProvider, FifaTimelineEventsProvider>((serviceProvider, client) =>
{
    MatchResultSyncOptions options = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<MatchResultSyncOptions>>()
        .Value;
    client.BaseAddress = new Uri(options.TimelineBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
});
```

#### Interface

```csharp
public interface IExternalMatchEventsProvider
{
    Task<ExternalMatchEvents> FetchGoalEventsAsync(
        string externalMatchId,
        string externalStageId,
        CancellationToken cancellationToken = default);
}
```

#### Goal DTO (subset)

```csharp
public class ExternalMatchGoalEvent
{
    public required string ExternalMatchId { get; init; }
    public string? ExternalTeamId { get; init; }
    public string? ExternalPlayerId { get; init; }
    public string? PlayerDisplayName { get; init; }
    public int Minute { get; init; }
    public bool IsHomeSide { get; init; }
    public bool IsOwnGoal { get; init; }
    public bool IsPenalty { get; init; }
    public string? EventTypeLabel { get; init; }
}
```

#### Provider — request + hard fail

```csharp
string requestUri =
    $"{Uri.EscapeDataString(_options.IdCompetition)}/" +
    $"{Uri.EscapeDataString(_options.IdSeason)}/" +
    $"{Uri.EscapeDataString(trimmedStageId)}/" +
    $"{Uri.EscapeDataString(trimmedMatchId)}" +
    $"?language={Uri.EscapeDataString(_options.Language)}";

using HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken);
if (!response.IsSuccessStatusCode)
{
    throw new InvalidOperationException(
        $"FIFA timeline request failed with status {(int)response.StatusCode} ({response.ReasonPhrase}).");
}
```

### Unit tests (`FifaTimelineEventsProviderTests`)

| Test | Asserts |
| --- | --- |
| `FetchGoalEventsAsync_MexicoSouthAfrica_ReturnsQuiñonesAndJimenezGoals` | 2 goals; ids; names; minutes 9/67; home; URI path + language |
| `FetchGoalEventsAsync_WhenHttpFails_ThrowsInvalidOperationException` | 502 → throw |
| `FetchGoalEventsAsync_WhenStageIdMissing_ThrowsArgumentException` | blank stage |
| `FetchGoalEventsAsync_IgnoresNonGoalEvents` | Attempt at Goal skipped; away Goal! kept |
| `FetchGoalEventsAsync_ParsesOwnGoalAndPenaltyTypes` | `IsOwnGoal` / `IsPenalty` |
| `NormalizeMinute_ParsesFifaMatchMinuteFormats` | `9'`, `90'+2'` → 92, empty → 0 |
| `ExtractPlayerDisplayName_ParsesDescription` | name before `(` |

### Roadmap mapping

| Task id | Status | Notes |
| --- | --- | --- |
| `p11-external-stage` | **Done** | [phase-11-external-stage](phase-11-external-stage.md) |
| `p11-timeline-provider` | **Done** (impl; gate open) | This page — fetch/parse only; no DB apply |
| `p11-player-resolve` … `p11-tests-gate` | Planned | [phase-11-planned](phase-11-planned.md) |

### Related

- [Phase 11 plan](phase-11-planned.md)
- [Task 1 External stage](phase-11-external-stage.md)
- [Phase 10 match sync](phase-10-match-sync.md)
- [API Status](../api-status.md)
- [Changes Review](../changes-review.md)
