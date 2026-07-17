# WorldCup System — Phase 10 Task 1: Post-Match Result Sync

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 10 plan](phase-10-planned.md)

## Phase 10 Task 1 — Post-match sync API

Opened **17 Jul 2026** as implementation of [phase-10-planned](phase-10-planned.md) item 1. Checklist: [Roadmap Phase 10](../roadmap.md#phase-10).

> **Success:** **Task 1 complete (17 Jul 2026).** FIFA calendar sync API, `ExternalMatchId`, Admin endpoints, side orientation, idempotent apply, bet resolve, unit tests. Review gate closed — **268** tests passed. Commit: `dcb322b`. Fixtures UX and WC styling are **not** part of this task.

### Review gate

> **Success:** **Gate closed (17 Jul 2026).** High Bugbot findings fixed; MatchSync unit tests added; `dotnet test` **268** passed.

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Missing `MatchStatus` treated as finished | High | Fixed | Require explicit FIFA `MatchStatus`; refuse if omitted |
| Home/Away assumed = TeamOne/TeamTwo | High | Fixed | Orient/swap scores via team name aliases; throw on mismatch |
| Misleading “resolved bets” message | Medium | Fixed | Message reports resolve count or skip reason |
| Re-sync replaces real Goal rows with placeholders | Medium | Accepted | Score-only ops model; no churn when counts already match |
| MatchResultSync unit tests | High | Fixed | Provider + sync service + controller tests (19 new) |
| `AddMatchExternalMatchId` migration | Medium | Ops | Applied via `MigrateAsync` + IF NOT EXISTS repair on API start |
| FIFA public calendar ToS / rate limits | Medium | Accepted for v1 | Config in `MatchResultSync` |

### Delivered checklist

| Outcome | Status |
| --- | --- |
| Structured post-match FT fetch (FIFA calendar JSON) | Done |
| Map FIFA `IdMatch` ↔ our `Match.Id` (`ExternalMatchId`) | Done |
| Admin sync one match / batch by WorldCup | Done |
| Explicit MatchStatus + home/away orientation | Done |
| Idempotent score apply | Done |
| Resolve bets via existing pipeline | Done |
| Knockout advance after sync | Done |
| Client sync API surface | Done |
| Unit tests (MatchSync + controller) | Done — 268 suite green |
| RabbitMQ | **Not used** — in-process Admin/API flow |

### Provider choice — FIFA calendar JSON

**v1 provider:** `FifaCalendarMatchResultProvider` → `https://api.fifa.com/api/v3/calendar/matches`.

| Config key (`MatchResultSync`) | Default (appsettings) | Role |
| --- | --- | --- |
| `Provider` | `FifaCalendar` | Documented key; v1 wires FIFA only |
| `BaseUrl` | `https://api.fifa.com/api/v3/calendar/matches` | HttpClient base |
| `IdCompetition` | `17` | FIFA competition id |
| `IdSeason` | `285023` | Season id (update when FIFA rotates seasons) |
| `Language` | `en` | Query language |

Request shape: `?idCompetition=&idSeason=&idMatch=&language=&count=1`. Finished only when FIFA `MatchStatus` is present and equals `0` (Played). Scores from `HomeTeamScore` / `AwayTeamScore` (fallback `Home.Score` / `Away.Score`).

**Score-only:** no cards, detailed team stats, or real scorer identities from FIFA.

### ExternalMatchId mapping

- Column: `Match.ExternalMatchId` — `varchar(64)`, nullable
- Unique filtered index: `IX_Match_ExternalMatchId` where not null
- Migration: `20260717120000_AddMatchExternalMatchId`
- Exposed on list/detail DTOs and client `Match.externalMatchId`
- Admin `POST Match/SetExternalMatchId` — rejects blank ids and duplicate mappings

### Admin API surface

| Endpoint | Auth | Behavior |
| --- | --- | --- |
| `POST /api/Match/SetExternalMatchId` | Admin | Body: `{ matchId, externalMatchId }` |
| `POST /api/Match/SyncResult/{matchId}` | Admin | Fetch FIFA FT → orient sides → apply score → resolve bets → try knockout advance |
| `POST /api/Match/SyncFinishedResults?worldCupId=` | Admin | All WC fixtures with a non-empty `ExternalMatchId`; per-match errors collected |

**Client:** `match-api.service.ts` — `setExternalMatchId`, `syncResult`, `syncFinishedResults` + models in `api.models.ts`.

### Pipeline (implemented)

```text
Admin SyncResult / SyncFinishedResults
    → require ExternalMatchId + both TeamOneId/TeamTwoId
    → FifaCalendarMatchResultProvider.FetchResultAsync(IdMatch)
    → if MatchStatus missing or not finished: Applied=false (no DB write)
    → OrientScoresToLocalSides (aliases: Korea Republic→South Korea, USA→United States, …)
    → ApplyScoreIdempotentAsync
    → if kickoff+MatchDurationMinutes ≤ UtcNow: BetService.ResolveBetsForMatch
    → KnockoutService.TryAdvanceFromMatch (warning on conflict)
```

### Idempotent score apply

1. Ensure `TeamStats` for both teams (default possession 50/50 if missing).
2. Count existing goals per side.
3. If counts already equal external FT → `ScoreChanged=false` (no Goal churn).
4. Else delete existing goals for those stats, create N placeholder goals per side.
5. Placeholder player: name `"Tournament Scorer"`, number `99`, position Forward (created once per team).

### RabbitMQ — decision (done)

**v1: no RabbitMQ / MassTransit.** Sync runs in-process on Admin HTTP calls. Revisit only if continuous multi-match crawl, durable cross-restart retries, or multiple consumers are required. See [phase-10-planned](phase-10-planned.md).

### Explicit non-goals (this task)

- Live in-match feed replacing Admin live console
- HTML website scraping as primary source
- Sync of cards / detailed stats / real scorers
- Fixtures betting UX (`p10-fixtures-ux`) — tracked separately; see [phase-10-fixtures-ux](phase-10-fixtures-ux.md)
- WC visual styling (`p10-wc-styling`)
- Message broker

### Files touched (Task 1)

| Area | Paths |
| --- | --- |
| Provider + sync | `Core/Services/MatchSync/*`, `Core/Options/MatchResultSyncOptions.cs` |
| DTOs | `Core/DTOs/Matches/MatchResultSyncDTO.cs`, `MatchDTO.ExternalMatchId` |
| Data | `Match.ExternalMatchId`, `ApplicationDbContext`, migration `AddMatchExternalMatchId` |
| API | `MatchController` sync actions; `Program.cs` DI + HttpClient |
| Config | `appsettings.json` → `MatchResultSync` |
| Client | `match-api.service.ts`, `api.models.ts` |
| Tests | `MatchResultSyncServiceTests`, `FifaCalendarMatchResultProviderTests`, `MatchControllerTests` sync actions |

### Phase 10 roadmap mapping

| Task id | Status | Notes |
| --- | --- | --- |
| `p10-match-sync` | **Done** | Sync + mapping + FIFA provider + tests; gate closed |
| RabbitMQ decision | **Done** | No broker in v1 |
| `p10-fixtures-ux` | **Done in code** (gate pending) | [phase-10-fixtures-ux](phase-10-fixtures-ux.md) |
| `p10-wc-styling` | Planned | Not started |

### Related

- [Phase 10 planned backlog](phase-10-planned.md)
- [Changes Review](../changes-review.md)
- [API Status](../api-status.md) — Match endpoints
- [WC 2026 live data](wc2026-live-data.md) — offline/manual path still valid
- [Phase 7](phase-7-bugfixes.md) — cancelled broad live external API
- [Phase 4 betting](phase-4-betting.md) — resolve rules reused
