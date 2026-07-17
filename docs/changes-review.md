# WorldCup System — Changes Review

## Docs navigation

[Dashboard](index.md) | [System Overview](overview.md) | [Completion Roadmap](roadmap.md) | [Dev Workflow](workflow.md) | [API Status](api-status.md) | [Data Model](data-model.md) | [Changes Review](changes-review.md)

---
## Changes Review

Latest: **Phase 10 Task 2 complete (17 Jul 2026)** — fixtures Action default, chips, Open/Live/Finished sections, jump-to-bet; Bugbot cleared; Jasmine **12** + API **268** green. Task 1 remains closed (`dcb322b`). Detail: [phase-10-fixtures-ux](changes/phase-10-fixtures-ux.md) · [phase-10-match-sync](changes/phase-10-match-sync.md) · [phase-10-planned](changes/phase-10-planned.md) · [phase-9-ops-closeout](changes/phase-9-ops-closeout.md) · [phase-8-dashboards](changes/phase-8-dashboards.md) · [phase-7-bugfixes](changes/phase-7-bugfixes.md) · [wc2026-live-data](changes/wc2026-live-data.md).

> **Success:** **Phase 10 Task 2 gate closed.** Fixtures UX shipped; no high/critical Bugbot findings; Jasmine **12** + `dotnet test` **268** passed. Checklist: [Phase 10](roadmap.md#phase-10) · detail: [phase-10-fixtures-ux](changes/phase-10-fixtures-ux.md).

> **Success:** **Phase 10 Task 1 gate closed.** Sync API + RabbitMQ decision done; Bugbot highs fixed; **268** tests green. Detail: [phase-10-match-sync](changes/phase-10-match-sync.md).

> **Info:** **Phase 9 in progress.** Remaining: record Final after 19 Jul FT. Checklist: [Phase 9](roadmap.md#phase-9).

> **Success:** **Phase 8 implemented (13–16 Jul 2026).** User dashboard, login return URLs, bet resolve hardening (3/0 + bulk), live snapshot API, admin hub / live console, UpdateTeam wiring. Checklist: [Phase 8](roadmap.md#phase-8).

> **Success:** **Phase 7 knockout + live data.** `MatchStage` / feeders, `KnockoutService`, bracket SPA, H2H standings, offline WC 2026 scripts. External football API cancelled for live — Phase 10 adds **post-match** FIFA calendar sync only.

### Summary stats

- **Phase 10 Task 2 (gate closed):** `fixture-sections.ts` + 12 Jasmine specs, fixtures component — Action default, chips, sections, jump CTA (Live/Finished → Action), live `canBet` clear
- **Phase 10 Task 1 committed** (`dcb322b`): MatchSync services, `ExternalMatchId` migration, Admin sync endpoints, client API, tests
- **Phase 10 checklist:** Tasks 1–2 done (8/9 items); Task 3 styling still open
- **Tests:** Jasmine **12** / 12; API `dotnet test` **268** / 268 (17 Jul 2026)

### Phase 10 Roadmap Mapping

| Task | Status | Evidence |
| --- | --- | --- |
| `p10-match-sync` — Post-match FIFA sync + resolve | **Done** | `FifaCalendarMatchResultProvider`, `MatchResultSyncService`, `ExternalMatchId`, Admin sync endpoints, 19 new tests |
| Explicit MatchStatus + home/away orientation | **Done** | Provider requires status; sync orients/swaps sides |
| RabbitMQ decision — v1 without broker | **Done** | Documented; in-process Admin/API only |
| Client sync surface | **Done** | `match-api.service.ts` + sync DTOs |
| `p10-fixtures-ux` — Bettable matches first | **Done** | Action default; chips; Open/Live/Finished; jump fixed; Jasmine **12**; [detail](changes/phase-10-fixtures-ux.md) |
| `p10-wc-styling` — WC visual polish | Planned | Not started |

### Phase 8 Roadmap Mapping

| Task | Status | Evidence |
| --- | --- | --- |
| `p8-dev-roles` — Dev Admin + User | Done | Committed `Program.cs` DevAdmin / DevUser (`Admin123!` / `User123!`) |
| `p8-login-return` — Deep-link restore | Done | Guards pass `returnUrl`; login `sanitizeReturnUrl` + `navigateByUrl`; defaults `/admin` / `/dashboard` |
| `p8-user-dashboard` — User hub | Done | `/dashboard` — `getMySummary` + active bets; shell nav for non-admins |
| `p8-bet-auto-resolve` — Resolve hardening | Done | `ResolveBetsResultDTO` +3/0 breakdown; DeleteGoal re-score; TryAuto + world-cup bulk |
| `p8-admin-leaderboard` — Resolve UI | Done | Schedule breakdown + hub `ResolveBetsForWorldCup` |
| `p8-live-snapshot` — Live feed API | Done | `GET Match/GetLiveSnapshot`; fixtures 30s poll |
| `p8-live-action-api` — Client write APIs | Done | goal / card / team-stats API services |
| `p8-admin-live-console` — Live console | Done | `/admin/live/{matchId}` |
| `p8-admin-team-import` — Manual teams | Done | Admin Teams create + `updateTeam` |
| `p8-admin-hub` — Ops hub | Done | `/admin` → ops cards (+ generate bracket) |

### Phase 7 Roadmap Mapping (same uncommitted tree)

| Task | Status | Evidence in this diff |
| --- | --- | --- |
| `p7-knockout` — Bracket + feeders | Done | `KnockoutService`, `KnockoutController`, `MatchStage`, feeder columns, `/bracket`, hub GenerateBracket |
| `p7-fixtures-seed` — WC 2026 offline data | Done | `scripts/seed-wc2026-groups.sql`, `import_wc2026_finished_matches.py` (through both SFs), `RoundOf32` |
| Standings H2H tiebreakers | Done | `StandingsService.ApplyHeadToHeadTiebreakers` |
| `p7-live-refresh` / resolve after goals | Done | Fixtures snapshot poll; `GoalService.TryResolveAndAdvance` |
| `p7-external-api` | Cancelled | Manual scripts + Admin live console; Phase 10 post-match FIFA sync is a narrower revisit |

### Review Gate — Status

> **Success:** **Phase 10 Task 2 gate closed** (17 Jul 2026). Jasmine **12** + API **268** passed. **Phase 10 Task 1 gate closed**. **Phase 8 / knockout gate closed** (16 Jul 2026).

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Task 2 Bugbot review | High (gate) | **Cleared** | No high/critical findings |
| Live snapshot stale `canBet` | High | **Fixed** | `canBet` forced false when live status ≠ `Scheduled` |
| Jump scroll after filter change | Medium | **Fixed** | Live/Finished → Action; `setTimeout` before `#fixture-{id}` scroll |
| Unknown status → Open bucket | Medium | Accepted v1 | Documented in [phase-10-fixtures-ux](changes/phase-10-fixtures-ux.md) |
| Edited applied migration `IdentityLongKeys` | Critical | Cleared | Not in current diff; knockout schema uses forward `AddMatchStage` |
| Parallel possession updates race (live console) | High | Fixed | Sequential `updateTeamStats` + possession sum check |
| Sync missing MatchStatus / home-away orientation | High | Fixed | Explicit status + side orientation in MatchSync |
| MatchResultSync unit tests | High | Fixed | 19 new tests; suite 268 green |
| Re-sync placeholder scorers | Medium | Accepted | Score-only model; no churn when counts match |
| Fixtures auto-resolve / TBD update / stage lock | Medium | Fixed | Snapshot on load; nullable UpdateMatch; block stage change after events |
| `AddMatchStage` + Feeder* + `ExternalMatchId` migrate | Medium | Ops | Migrate on API start |
| Silent swallow of incomplete-stats / advance errors | Medium | Accepted | Optional Serilog / admin surface later |

### Risk Summary

| Risk | Severity | Details | Action |
| --- | --- | --- | --- |
| Task 2 review/test gate | High | Closed | Jasmine **12** + API **268** |
| Migration rewrite (IdentityLongKeys) | Critical | Removed from working tree | Cleared |
| Sync orientation / MatchStatus | High | Fixed in Task 1 | Cleared |
| Sync deletes Admin goals on score change | Medium | Placeholder scorers when counts differ | Accepted score-only |
| Tests executed | Done | Jasmine fixtures + API suite | **12** + **268** passed (17 Jul 2026) |
| FIFA calendar ToS / season ids | Medium | Public JSON; config-driven | Monitor; paid provider fallback |
| Final not yet finished in DB | Low | Kickoff 19 Jul | Ops after FT |

### Per-File Summary

#### Fixtures UX (NEW helpers + component) — Action / chips / sections


**[p10-fixtures-ux]**

`fixture-sections.ts` (+ Jasmine spec): group, filter, next bettable. Fixtures component: default Action, toolbar chips, Open → Live → Finished, collapsible finished, jump CTA, live poll clears `canBet`. Detail: [phase-10-fixtures-ux](changes/phase-10-fixtures-ux.md).

#### MatchSync (NEW) — FIFA provider + apply + resolve


**[p10-match-sync]**

`FifaCalendarMatchResultProvider`, `MatchResultSyncService`, options, DTOs; Admin SetExternal / SyncResult / SyncFinishedResults; client models + match-api methods. Detail: [phase-10-match-sync](changes/phase-10-match-sync.md).

#### Match entity / migration — ExternalMatchId


**[p10-match-sync]**

Nullable unique `ExternalMatchId`; migration `20260717120000_AddMatchExternalMatchId`.

#### BetService / BetController / BetDTO


**[p8-bet-auto-resolve · p8-admin-leaderboard]**

Resolve returns per-user +3/0 breakdown; world-cup bulk resolve; TryAuto helper for finished matches. Sync reuses `ResolveBetsForMatch`.

#### MatchService / MatchController — stage + GetLiveSnapshot + sync


**[p8-live-snapshot · p7-knockout · p10-match-sync]**

Stage on create/update; nullable teams + feeders in DTOs; batch live snapshot; Admin post-match sync actions.

#### Knockout (NEW) + GoalService advance


**[p7-knockout]**

Generate/get bracket; advance winners into feeder slots; AddGoal/DeleteGoal call `TryResolveAndAdvance`. Sync also calls `TryAdvanceFromMatch`.

#### StandingsService — H2H


**[p7-knockout]**

FIFA-style head-to-head among tied clusters after points / GD / GF.

#### SPA — dashboard / admin hub / live / bracket (NEW)


**[p8-user-dashboard · p8-admin-hub · p8-admin-live-console · p7-knockout]**

Untracked feature folders + write/knockout API services; routes in `app.routes.ts`.

#### Guards · login · shell · admin-teams


**[p8-login-return · p8-admin-team-import]**

`returnUrl` restore; role landing; UpdateTeam create/edit UI; Bracket nav link.

#### Fixtures + admin schedule · Program.cs


**[p8-live-snapshot · p8-admin-leaderboard · p7-knockout · p10-match-sync · p10-fixtures-ux]**

30s snapshot poll (+ Task 2 `canBet` clear); resolve breakdown; live console links; Knockout DI + Feeder* startup repair SQL; MatchResultSync HttpClient registration; Task 2 sectioned fixtures UI.

#### Scripts + tests + docs


**[coverage · ops]**

WC 2026 group/results import scripts; unit tests for resolve, snapshot, knockout, H2H standings (245 passed pre–Task 1 sync tests). Planning docs migrated HTML → Markdown (VitePress). Task 2 Jasmine `fixture-sections.spec.ts`.

### Prior Phases — Committed

| Phase | Status | Detail page |
| --- | --- | --- |
| Phase 1 — Foundation | Committed | [Phase 1 detail](changes/phase-1-identity.md) |
| Phase 2 — Tournament setup | Committed | [Phase 2 detail](changes/phase-2-tournament-setup.md) |
| Phase 3 — Match lifecycle | Committed | [Phase 3 detail](changes/phase-3-match-lifecycle.md) |
| Phase 4 — Betting | Committed | [Phase 4 detail](changes/phase-4-betting.md) |
| Phase 5 — Frontend SPA | Committed | [Phase 5 detail](changes/phase-5-frontend-spa.md) |
| Phase 6 — Quality & Operations | Committed | [Phase 6 detail](changes/phase-6-quality-ops.md) |
| Phase 7 — Bugfixes & Live Data | Done · uncommitted knockout/data | [Phase 7 detail](changes/phase-7-bugfixes.md) |
| Phase 8 — User & Admin Dashboards | Implemented · uncommitted | [Phase 8 detail](changes/phase-8-dashboards.md) |
| Phase 9 — Ops & Tournament Close-Out | In progress | [Phase 9 detail](changes/phase-9-ops-closeout.md) |
| Phase 10 — Match Sync, Fixtures UX & Styling | In progress (Tasks 1–2 done; Task 3 open) | [sync](changes/phase-10-match-sync.md) · [fixtures UX](changes/phase-10-fixtures-ux.md) · [plan](changes/phase-10-planned.md) |

### Suggested Next Steps

#### Phase 10 Task 3 (when asked)

WC visual styling — black/white/gold atmosphere across SPA.

#### Record Final after FT (19 Jul)

Admin live console on Match 116, sync API after ExternalMatchId mapped, or extend `import_wc2026_finished_matches.py`.
