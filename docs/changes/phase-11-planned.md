# WorldCup System — Phase 11 Planned Work

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md)

## Phase 11 — Real Goalscorers from FIFA Timeline

**Status: Done** — Tasks 1–8 complete (`p11-external-stage` … `p11-tests-gate`; gate closed; Bugbot highs fixed; suite **382**). Checklist: [Roadmap Phase 11](../roadmap.md#phase-11) · [Task 8 gate](phase-11-tests-gate.md) · [Task 1](phase-11-external-stage.md) · [Task 2](phase-11-timeline-provider.md) · [Task 3](phase-11-player-resolve.md) · [Task 4](phase-11-scorer-apply.md) · [Task 5](phase-11-sync-wire.md) · [Task 6](phase-11-backfill.md) · [Task 7](phase-11-recent-events-honesty.md).

Opened **20 Jul 2026** after fixtures Recent events showed incorrect player names. Phase 10 Task 1 sync is **score-only** and attributes goals to squad forwards in round-robin order (or `"Tournament Scorer"`). FIFA’s public **timeline** API already exposes real Goal! events (minute + player). This phase imports those identities into our `Goal` rows so Recent events / match timelines are truthful.

### Problem

| Symptom | Cause |
| --- | --- |
| Recent events show wrong scorers | `GetLiveSnapshot` reads real `Goal.PlayerId` rows that were never historically accurate |
| Import / sync invents names | `MatchResultSyncService` + offline scripts pick forwards by index; FIFA calendar returns scores only |
| Admin live console can fix one match | Not scalable for 100+ finished fixtures |

### Recommended approach

Extend the existing **Admin-triggered, in-process** sync path (no RabbitMQ). After FT score apply (or when scores already match), fetch:

```text
GET https://api.fifa.com/api/v3/timelines/{idCompetition}/{idSeason}/{idStage}/{idMatch}?language=en
```

Filter events where `TypeLocalized` is `Goal!` (and Own Goal / Penalty Goal as needed). Map each event to a local `Player` and rewrite `Goal` rows with correct `PlayerId` + minute (`TimeScored`). Keep bet resolve / knockout advance unchanged (1X2 does not depend on scorer identity).

**Proven sample (Mexico 2–0 South Africa, IdMatch `400021443`):** timeline returns Quiñones 9' and Jiménez 67' — not placeholder forwards.

### Goals (summary)

| # | Task id | Theme | Outcome | Status |
| --- | --- | --- | --- | --- |
| 1 | `p11-external-stage` | Stage id for timeline URLs | Persist FIFA `IdStage` on `Match.ExternalStageId` | **Done** |
| 2 | `p11-timeline-provider` | Timeline HTTP provider | Fetch/parse goal events (minute, team, player name/id, own-goal) | **Done** |
| 3 | `p11-player-resolve` | Player identity | Map FIFA player → local `Player` (match existing; create only when needed) | **Done** |
| 4 | `p11-scorer-apply` | Apply real scorers | Idempotent Goal rewrite when timeline events align with FT score | **Done** |
| 5 | `p11-sync-wire` | Wire into sync APIs | `SyncResult` / `SyncFinishedResults` call timeline after calendar FT | **Done** |
| 6 | `p11-backfill` | Backfill finished matches | Admin path to re-attribute scorers without changing FT when counts already match | **Done** |
| 7 | `p11-recent-events-honesty` | UI honesty | Hide or label placeholder / `"Tournament Scorer"` names in Recent events | **Done** |
| 8 | `p11-tests-gate` | Tests + review gate | Unit tests for provider/resolver/apply; Bugbot + `dotnet test` green | **Done** |

Implement **one task at a time**. Close each task’s review/test gate before starting the next (same loop as Phase 10).

---

### 1 — External stage id (`p11-external-stage`) — **Done**

#### Why

Timeline URLs require `IdStage` in addition to `IdMatch`. Calendar responses already include `IdStage`; we previously stored only `Match.ExternalMatchId`.

#### Decision

Persist **`Match.ExternalStageId`**. Calendar sync auto-fills the column when FIFA returns `IdStage`. Detail: [phase-11-external-stage](phase-11-external-stage.md).

#### Deliverables

- [x] Decide: column `Match.ExternalStageId` (not calendar-only resolve)
- [x] EF migration + IF NOT EXISTS repair on API start
- [x] Extend `SetExternalMatchId` to accept optional `ExternalStageId`
- [x] Expose on Match DTOs + client model
- [x] Document config: reuse `MatchResultSync:IdCompetition` / `IdSeason`
- [x] Calendar parse `IdStage` + SyncResult auto-persist
- [x] Unit tests for SetExternal stage semantics + SyncResult stage persist (gate)
- [x] Bugbot highs cleared + `dotnet test` green (gate) — suite **278** at Task 1; phase close **382**

#### Acceptance

Sync code can build a timeline URL for every mapped finished match without hard-coding stage ids (once stage is set or filled from calendar).

---

### 2 — FIFA timeline provider (`p11-timeline-provider`) — **Done**

#### Why

Calendar provider is score-only by design. Timeline is a separate FIFA v3 endpoint with event payloads.

#### Deliverables

- [x] `IExternalMatchEventsProvider` + `FifaTimelineEventsProvider`
- [x] HttpClient registration / `MatchResultSync:TimelineBaseUrl`
- [x] Parse `Event[]`: `Goal!`; also `Own Goal` / `Penalty Goal` labels (Mexico sample had only `Goal!`)
- [x] Normalize minute from `MatchMinute` (`9'` → 9, `90'+2'` → 92)
- [x] DTO: match id, team id, player id/name, minute, `IsHomeSide`, `IsOwnGoal`, `IsPenalty`
- [x] Fail hard in provider (throw); Task 5 catches as scorer warning so calendar FT still succeeds — [detail](phase-11-timeline-provider.md)

#### Acceptance

Unit test with recorded JSON fixture: Mexico–South Africa yields two Goal! events with Quiñones / Jiménez and minutes 9 / 67.

---

### 3 — Player identity resolution (`p11-player-resolve`) — **Done**

#### Why

Timeline gives FIFA `IdPlayer` + description text (`"Julian QUINONES (Mexico) scores!!"`). Our DB has seeded squad names that may not match spelling/case.

#### Decisions

| Decision | Choice |
| --- | --- |
| Persist `Player.ExternalPlayerId`? | **Yes** (unique filtered index) |
| Auto-create when missing? | **Yes** — named Forward, jersey 1–98; never Tournament Scorer when display name exists |

Detail: [phase-11-player-resolve](phase-11-player-resolve.md) (refined from git diff 20 Jul 2026).

#### Deliverables

- [x] Resolver service: inputs team id + FIFA player id + display name → `Player` (`ITimelinePlayerResolver` / `TimelinePlayerResolver`)
- [x] Match strategy: ExternalPlayerId → exact name → normalized name → create named player
- [x] Persist `Player.ExternalPlayerId` — migration `20260720140000_AddPlayerExternalPlayerId` + Program.cs `IF NOT EXISTS`
- [x] DI: `AddScoped<ITimelinePlayerResolver, TimelinePlayerResolver>`; DTO + client `externalPlayerId`
- [x] **Do not** credit `"Tournament Scorer"` when a real name is available (eligible squad excludes placeholder by name)
- [x] Cross-team ExternalPlayerId / conflicting id on name match → throw
- [x] Own-goal: caller passes conceding team id (documented for Task 4 / `GoalService` rules)
- [x] No silent round-robin fallback when timeline provided a name; ambiguous → throw
- [x] Unit tests + Bugbot highs cleared + `dotnet test` green (gate) — suite **342**

#### Acceptance

Given a timeline goal for a known squad forward, resolver returns that `Player.Id`. Ambiguous matches throw (not randomly assigned). Id-only miss without display name throws (cannot create).

---

### 4 — Real-scorer apply (`p11-scorer-apply`) — **Done**

#### Why

Today, when goal counts already match FT, sync is idempotent and **leaves wrong PlayerIds in place**. We need a path that updates attribution without churning scores or double-resolving bets incorrectly.

#### Decision

| Decision | Choice |
| --- | --- |
| Count mismatch | **Fail** with `InvalidOperationException` — leave Goal rows unchanged (calendar FT authoritative). Task 5 catches as scorer warning. |
| Apply style | **In-place** update of `PlayerId` / `TimeScored` / `IsOwnGoal` (no delete/recreate) |

Detail: [phase-11-scorer-apply](phase-11-scorer-apply.md).

#### Deliverables

- [x] Apply algorithm when timeline goal list is present:
  1. Orient home/away to TeamOne/TeamTwo via `homeMapsToTeamOne`
  2. Compare timeline goal counts to existing Goal counts
  3. If counts disagree → fail with clear error (documented)
  4. If counts agree → update PlayerId + TimeScored + IsOwnGoal in place
- [x] Preserve `IsOwnGoal` from timeline; resolve player on conceding team
- [x] Do **not** change TeamStats points / bet outcomes when only scorers change
- [x] Do **not** re-run knockout advance solely because scorers changed
- [x] `ITimelineScorerApplyService` + DI + unit tests

#### Acceptance

Re-apply of a finished match with correct score but wrong scorers updates player names; second apply is idempotent (same PlayerIds / minutes).

---

### 5 — Wire into sync APIs (`p11-sync-wire`) — **Done**

#### Why

Operators already use `SyncResult` / `SyncFinishedResults`. Scorers should ride that path, not a one-off script-only fix.

#### Deliverables

- [x] After calendar FT apply (or when score already matched), call timeline provider + scorer apply
- [x] Sync result DTO / message includes scorer outcome: applied / skipped / warning
- [x] Batch sync collects per-match scorer errors without aborting the whole World Cup run
- [x] Rate-limit / delay consideration for batch (FIFA public API) — `BatchDelayMilliseconds` (default 250)
- [x] Client: surface new sync message fields if DTOs change (`api.models.ts`)
- [x] Bugbot highs cleared + `dotnet test` green (gate) — suite **361**; wiring **8**

#### Acceptance

Admin `SyncResult` on a mapped finished match yields Recent events with real names after fixtures poll (when stage id present and timeline succeeds).

Detail: [phase-11-sync-wire](phase-11-sync-wire.md).

---

### 6 — Backfill finished matches (`p11-backfill`) — **Done**

#### Why

Most WC 2026 fixtures already have FT scores and placeholder Goal rows. Operators need a dedicated “fix scorers only” action without pretending the score changed.

#### Deliverables

- [x] Admin endpoint e.g. `POST Match/SyncScorers/{matchId}` and/or `POST Match/SyncScorersForWorldCup?worldCupId=`
- [x] Requires `ExternalMatchId` (+ stage resolution from Task 1)
- [x] Skips matches that are not finished / have no timeline goals
- [x] Optional: one-shot offline script that calls the same service for local DB repair
- [x] Document ops runbook: map External ids → backfill → verify Recent events
- [x] Bugbot highs cleared + `dotnet test` green (gate) — backfill **14**; suite **375**

#### Acceptance

One Admin (or script) run updates scorers for all mapped finished matches in the tournament without changing scores or re-awarding bet points.

Detail: [phase-11-backfill](phase-11-backfill.md).

---

### 7 — Recent events honesty (`p11-recent-events-honesty`) — **Done**

#### Why

Until backfill completes (and for any remaining placeholders), the UI should not present invented names as fact.

#### Deliverables

- [x] Snapshot / fixtures Recent events: omit player name when it is `"Tournament Scorer"`, **or** show team + event type only
- [x] Optional: omit events whose player is a known placeholder number (`99`) — **Rejected**: name-only (jersey 99 may be real; matches Task 3)
- [x] Match detail / admin live timeline: same rule for consistency
- [x] Prefer fixing data (Tasks 4–6) over permanent UI hiding — honesty is a safety net
- [x] Bugbot highs cleared + `dotnet test` green (gate) — honesty **3**; suite **380**

#### Acceptance

Fixtures Recent events never display the literal string `Tournament Scorer` as if it were a real player.

Detail: [phase-11-recent-events-honesty](phase-11-recent-events-honesty.md).

---

### 8 — Tests & review gate (`p11-tests-gate`) — **Done**

Formal phase close after Tasks 1–7. Detail: [phase-11-tests-gate](phase-11-tests-gate.md).

#### Deliverables

- [x] Unit tests present in tree: timeline JSON parse; minute normalization; player resolve; scorer apply idempotency; SyncResult wiring; backfill skip paths; honesty
- [x] Bugbot + Markdown docs review per workspace phase gate
- [x] Fix all **high/critical** findings — cancel rethrow + jersey-99 name-only; **+2** regressions
- [x] `dotnet test` on `WorldCup-System.Tests` passes — suite **382**
- [x] Flip this plan’s Status column, [roadmap](../roadmap.md#phase-11) Task 8, and [changes-review](../changes-review.md) to **Done**

#### Acceptance

Phase 11 checklist on the roadmap marked done — tests passed and review blockers cleared (21 Jul 2026).

---

### Phase 11 Roadmap Mapping

| Task id | Status | Notes |
| --- | --- | --- |
| `p11-external-stage` | **Done** | Column + migration + SetExternal + calendar IdStage + sync auto-fill; [detail](phase-11-external-stage.md) |
| `p11-timeline-provider` | **Done** | `FifaTimelineEventsProvider`; gate closed (310 tests); [detail](phase-11-timeline-provider.md) |
| `p11-player-resolve` | **Done** | `TimelinePlayerResolver` + `ExternalPlayerId`; gate closed (**342**); [detail](phase-11-player-resolve.md) |
| `p11-scorer-apply` | **Done** | `TimelineScorerApplyService`; gate closed (**353**); [detail](phase-11-scorer-apply.md) |
| `p11-sync-wire` | **Done** | SyncResult / SyncFinishedResults wire; suite **361**; [detail](phase-11-sync-wire.md) |
| `p11-backfill` | **Done** | SyncScorers / SyncScorersForWorldCup + script; suite **375**; [detail](phase-11-backfill.md) |
| `p11-recent-events-honesty` | **Done** | Omit placeholder names; gate closed (**380**); [detail](phase-11-recent-events-honesty.md) |
| `p11-tests-gate` | **Done** | Formal close; Bugbot highs fixed; suite **382**; [detail](phase-11-tests-gate.md) |

### Explicit non-goals (until revisited)

- Live in-match polling / replacing Admin live console during play
- Full cards / substitutions / VAR event import (timeline has them; **goals first**)
- Paid third-party sports APIs as primary source
- RabbitMQ / MassTransit
- HTML scraping of fifa.com match pages
- Historically perfect assists or xG
- Changing betting rules to depend on scorers

### Relationship to earlier phases

| Phase | Relationship |
| --- | --- |
| Phase 7 | Cancelled broad live external API — Phase 11 remains **post-match** only |
| Phase 8 | Recent events UI already consumes snapshot; becomes accurate once Goals are fixed |
| Phase 10 Task 1 | Calendar FT sync stays; Phase 11 **adds** timeline scorers on top |
| Offline scripts | `import_wc2026_finished_matches.py` may keep score import; scorers should prefer backfill/sync over round-robin long-term |

### Related

- [Roadmap Phase 11](../roadmap.md#phase-11)
- [Phase 10 match sync](phase-10-match-sync.md) — score-only baseline
- [Phase 10 planned](phase-10-planned.md)
- [WC 2026 live data](wc2026-live-data.md) — documents placeholder scorer limitation
- [Phase 8 dashboards](phase-8-dashboards.md) — `GetLiveSnapshot` / Recent events
