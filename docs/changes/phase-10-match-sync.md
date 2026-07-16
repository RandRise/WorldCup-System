# WorldCup System — Phase 10 Task 1: Post-Match Result Sync

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 10 plan](phase-10-planned.md)

## Phase 10 Task 1 — Post-match sync API

Opened **17 Jul 2026** as implementation of [phase-10-planned](phase-10-planned.md) item 1. Checklist: [Roadmap Phase 10](../roadmap.md#phase-10).

> **Success:** **Sync API portion done in code (17 Jul 2026).** FIFA calendar JSON provider, `ExternalMatchId` mapping, Admin sync endpoints, idempotent score apply, bet resolve reuse. **No RabbitMQ in v1** (decision documented + implemented as in-process). Fixtures UX and WC styling are **not** part of this task.

### Review gate

> **Warning:** **Gate open — fix high/critical items before `dotnet test` / phase advance.**

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Home/Away assumed = TeamOne/TeamTwo | High | Open | Confirm FIFA Home maps to our TeamOne for every mapped match; wrong orientation applies inverted scores and wrong 1X2 resolve |
| Re-sync replaces real Goal rows with placeholder scorers | High | Open | Accept for score-only betting, or preserve real scorers when counts already match / Admin-entered; document ops expectation |
| No dedicated MatchResultSync unit tests | High | Open | Add provider + sync service tests (idempotency, unfinished external, missing ExternalMatchId, unique conflict) before closing gate |
| `AddMatchExternalMatchId` migration not yet applied on all envs | Medium | Ops | Run migrate on next API start; unique filtered index on `ExternalMatchId` |
| FIFA public calendar ToS / rate limits / season ids | Medium | Accepted for v1 | Config in `MatchResultSync`; revisit paid provider if blocked |
| Large uncommitted tree (P7–P10) | High | Ops | Commit when asked — not a code defect |

### Goals (this task)

| Outcome | Status |
| --- | --- |
| Structured post-match FT fetch (prefer JSON over HTML scrape) | Done — FIFA calendar |
| Map FIFA `IdMatch` ↔ our `Match.Id` | Done — `ExternalMatchId` |
| Admin sync one match / batch by WorldCup | Done — `SyncResult` / `SyncFinishedResults` |
| Idempotent score apply (no double goals / double points on re-run) | Done — delete+recreate only when counts differ |
| Resolve bets via existing pipeline | Done — `IBetService.ResolveBetsForMatch` |
| Knockout advance after sync | Done — `IKnockoutService.TryAdvanceFromMatch` |
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

Request shape: `?idCompetition=&idSeason=&idMatch=&language=&count=1`. Finished when FIFA `MatchStatus == 0` (Played). Scores from `HomeTeamScore` / `AwayTeamScore` (fallback `Home.Score` / `Away.Score`).

**Score-only:** no cards, team stats (beyond ensuring `TeamStats` rows), or real scorer identities from FIFA.

### ExternalMatchId mapping

- Column: `Match.ExternalMatchId` — `varchar(64)`, nullable
- Unique filtered index: `IX_Match_ExternalMatchId` where not null
- Migration: `20260717120000_AddMatchExternalMatchId`
- EF config in `ApplicationDbContext`
- Exposed on list/detail DTOs and client `Match.externalMatchId`
- Admin `POST Match/SetExternalMatchId` — rejects blank ids and duplicate mappings

### Admin API surface

| Endpoint | Auth | Behavior |
| --- | --- | --- |
| `POST /api/Match/SetExternalMatchId` | Admin | Body: `{ matchId, externalMatchId }` |
| `POST /api/Match/SyncResult/{matchId}` | Admin | Fetch FIFA FT → apply score → resolve bets → try knockout advance |
| `POST /api/Match/SyncFinishedResults?worldCupId=` | Admin | All WC fixtures with a non-empty `ExternalMatchId`; per-match errors collected, not abort-all |

**Client:** `match-api.service.ts` — `setExternalMatchId`, `syncResult`, `syncFinishedResults` + models in `api.models.ts`.

### Pipeline (implemented)

```text
Admin SyncResult / SyncFinishedResults
    → require ExternalMatchId + both TeamOneId/TeamTwoId
    → FifaCalendarMatchResultProvider.FetchResultAsync(IdMatch)
    → if not finished: Applied=false (no DB write)
    → ApplyScoreIdempotentAsync (TeamOne=Home, TeamTwo=Away)
    → if kickoff+MatchDurationMinutes ≤ UtcNow: BetService.ResolveBetsForMatch
    → KnockoutService.TryAdvanceFromMatch (warning on conflict)
```

### Idempotent score apply

1. Ensure `TeamStats` for both teams (default possession 50/50 if missing).
2. Count existing goals per side.
3. If counts already equal external FT → `ScoreChanged=false` (no Goal churn).
4. Else delete existing goals for those stats, create N placeholder goals per side.
5. Placeholder player: name `"Tournament Scorer"`, number `99`, position Forward (created once per team).

Re-resolve uses existing bet rules (3/0). Resolve is skipped until scheduled FT window has passed.

### RabbitMQ — decision (done)

**v1: no RabbitMQ / MassTransit.** Sync runs in-process on Admin HTTP calls. Revisit only if continuous multi-match crawl, durable cross-restart retries, or multiple consumers are required. See [phase-10-planned](phase-10-planned.md#rabbitmq--use-or-not).

### Explicit non-goals (this task)

- Live in-match feed replacing Admin live console
- HTML website scraping as primary source
- Sync of cards / detailed stats / real scorers
- Fixtures betting UX (`p10-fixtures-ux`)
- WC visual styling (`p10-wc-styling`)
- Message broker

### Files touched (Task 1 focus)

| Area | Paths |
| --- | --- |
| Provider + sync | `Core/Services/MatchSync/*`, `Core/Options/MatchResultSyncOptions.cs` |
| DTOs | `Core/DTOs/Matches/MatchResultSyncDTO.cs`, `MatchDTO.ExternalMatchId` |
| Data | `Match.ExternalMatchId`, `ApplicationDbContext`, migration `AddMatchExternalMatchId` |
| API | `MatchController` sync actions; `Program.cs` DI + HttpClient |
| Config | `appsettings.json` → `MatchResultSync` |
| Packages | `Core.csproj` — `Microsoft.Extensions.Http`, `Microsoft.Extensions.Options` |
| Client | `match-api.service.ts`, `api.models.ts` sync types + `externalMatchId` |
| Tests | `MatchControllerTests` mock `IMatchResultSyncService` only — **service tests still open** |

### Phase 10 roadmap mapping (Task 1)

| Task id | Status | Notes |
| --- | --- | --- |
| `p10-match-sync` | Done (API) | Sync + mapping + FIFA provider in code; review/test gate still open |
| RabbitMQ decision | Done | No broker in v1 |
| `p10-fixtures-ux` | Planned | Not started |
| `p10-wc-styling` | Planned | Not started |

### Related

- [Phase 10 planned backlog](phase-10-planned.md)
- [Changes Review](../changes-review.md)
- [API Status](../api-status.md) — Match endpoints
- [WC 2026 live data](wc2026-live-data.md) — offline/manual path still valid
- [Phase 7](phase-7-bugfixes.md) — cancelled broad live external API
- [Phase 4 betting](phase-4-betting.md) — resolve rules reused
