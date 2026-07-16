# WorldCup System — Changes Review

## Docs navigation

[Dashboard](index.md) | [System Overview](overview.md) | [Completion Roadmap](roadmap.md) | [Dev Workflow](workflow.md) | [API Status](api-status.md) | [Data Model](data-model.md) | [Changes Review](changes-review.md)

---
## Changes Review

Latest: **Phase 10 Task 1 — Match sync (17 Jul 2026)** — FIFA calendar post-match sync API in code; fixtures UX / styling still planned. Detail: [phase-10-match-sync](changes/phase-10-match-sync.md) · [phase-10-planned](changes/phase-10-planned.md) · [phase-9-ops-closeout](changes/phase-9-ops-closeout.md) · [phase-8-dashboards](changes/phase-8-dashboards.md) · [phase-7-bugfixes](changes/phase-7-bugfixes.md) · [wc2026-live-data](changes/wc2026-live-data.md).

> **Info:** **Phase 10 in progress.** Task 1 sync API + RabbitMQ decision done; Tasks 2–3 (fixtures UX, WC styling) not started. Checklist: [Phase 10](roadmap.md#phase-10) · detail: [phase-10-match-sync](changes/phase-10-match-sync.md).

> **Info:** **Phase 9 in progress.** Remaining: record Final after 19 Jul FT; commit Phase 7–8 when asked. Checklist: [Phase 9](roadmap.md#phase-9).

> **Success:** **Phase 8 implemented (13–16 Jul 2026).** User dashboard, login return URLs, bet resolve hardening (3/0 + bulk), live snapshot API, admin hub / live console, UpdateTeam wiring. Checklist: [Phase 8](roadmap.md#phase-8).

> **Success:** **Phase 7 knockout + live data in same tree.** `MatchStage` / feeders, `KnockoutService`, bracket SPA, H2H standings, offline WC 2026 scripts (groups + finished through both SFs). External football API cancelled for live — Phase 10 adds **post-match** FIFA calendar sync only.

> **Success:** **Review + test gate passed for Phase 8 / knockout (16 Jul 2026).** Bugbot high (possession race) fixed; medium TBD/stage/auto-resolve fixed. `dotnet test`: **245 passed**. Phase 10 Task 1 review gate is **still open**.

### Summary stats (working tree)

- **64 tracked changes** (modified/deleted) + **~50+ untracked** paths (includes Phase 10 MatchSync + ExternalMatchId migration)
- **Tracked diff:** +3048 / −6233 (large deletion = old HTML docs → Markdown)
- **Phase 10 Task 1 new surface:** `Core/Services/MatchSync/*`, `MatchResultSyncDTO`, `AddMatchExternalMatchId`, MatchController sync actions, client sync API models
- **Phase 8 done · Phase 7 done (live API cancelled) · Phase 10 Task 1 API done:** 10 / 10 · 8 / 8 · sync 2 / 4 Phase 10 checklist items

**Git repo (`WorldCup-System/`) — uncommitted on `master` (ahead of remote):**

**Themes:** Phase 7–8 product work + Phase 9 ops + Phase 10 Task 1 post-match sync.

### Phase 10 Roadmap Mapping

| Task | Status | Evidence |
| --- | --- | --- |
| `p10-match-sync` — Post-match FIFA sync + resolve | Done (API) | `FifaCalendarMatchResultProvider`, `MatchResultSyncService`, `ExternalMatchId`, Admin `SyncResult` / `SyncFinishedResults` / `SetExternalMatchId` |
| RabbitMQ decision — v1 without broker | Done | Documented; in-process Admin/API only — no RabbitMQ wired |
| `p10-fixtures-ux` — Bettable matches first | Planned | Not started |
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

> **Success:** **Phase 8 / knockout gate closed.** High/critical review findings fixed; `dotnet test` **245 passed** (16 Jul 2026). Medium debt items below may ship with acceptance notes.

> **Warning:** **Phase 10 Task 1 gate open.** Clear high/critical sync risks before Task 1 test pass / phase advance.

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Edited applied migration `IdentityLongKeys` | Critical | Cleared | Not in current diff; knockout schema uses forward `AddMatchStage` |
| Parallel possession updates race (live console) | High | Fixed | Sequential `updateTeamStats` + possession sum check |
| `dotnet test` Phase 8 + knockout | High | Passed | 245 passed / 0 failed |
| Home/Away = TeamOne/TeamTwo orientation (sync) | High | Open | Verify FIFA Home maps to TeamOne before production sync |
| Re-sync wipes real scorers for placeholders | High | Open | Accept score-only ops model or harden apply path |
| MatchResultSync unit tests missing | High | Open | Add service/provider tests before closing Task 1 gate |
| Large uncommitted Phase 7–10 surface | High | Ops | Commit when asked |
| Fixtures auto-resolve / TBD update / stage lock | Medium | Fixed | Snapshot on load; nullable UpdateMatch; block stage change after events |
| `AddMatchStage` + Feeder* + `ExternalMatchId` migrate | Medium | Ops | Migrate on next API start |
| Silent swallow of incomplete-stats / advance errors | Medium | Accepted | Optional Serilog / admin surface later |

### Risk Summary

| Risk | Severity | Details | Action |
| --- | --- | --- | --- |
| Migration rewrite (IdentityLongKeys) | Critical | Removed from working tree | Cleared |
| Sync home/away orientation | High | Assumed TeamOne=Home | Confirm mappings; gate before test close |
| Sync deletes Admin goals on score change | High | Placeholder “Tournament Scorer” goals | Document / harden |
| No MatchSync service tests | High | Controller mocks only | Write tests |
| Uncommitted feature set | High | Dashboards + knockout + sync + WC scripts + MD docs | Commit when asked |
| Tests executed (P8/knockout) | Done | Phase 8 + knockout suite | 245 passed (16 Jul 2026) |
| FIFA calendar ToS / season ids | Medium | Public JSON; config-driven | Monitor; paid provider fallback |
| GetLiveSnapshot loads broad entity sets | Medium | Players/countries/teams for event enrichment | OK for demo scale |
| Nullable team FKs + TBD slots | Medium | Goals/cards blocked until both teams set | Guarded in Card/Goal paths |
| Final not yet finished in DB | Low | Kickoff 19 Jul — schedule via helper script | Ops after FT |

### Per-File Summary

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


**[p8-live-snapshot · p8-admin-leaderboard · p7-knockout · p10-match-sync]**

30s snapshot poll; resolve breakdown; live console links; Knockout DI + Feeder* startup repair SQL; MatchResultSync HttpClient registration.

#### Scripts + tests + docs


**[coverage · ops]**

WC 2026 group/results import scripts; unit tests for resolve, snapshot, knockout, H2H standings (245 passed pre–Task 1 sync tests). Planning docs migrated HTML → Markdown (VitePress).

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
| Phase 10 — Match Sync, Fixtures UX & Styling | In progress (Task 1 API) | [Phase 10 sync](changes/phase-10-match-sync.md) · [plan](changes/phase-10-planned.md) |

### Suggested Next Steps

#### Close Phase 10 Task 1 review/test gate

Address high sync risks (orientation, placeholder goals, unit tests), then run `dotnet test`.

#### Phase 10 Tasks 2–3 (when asked)

Fixtures betting priority UX; WC visual styling.

#### Commit when ready

Stage Phase 7–10 paths when you ask for a commit.

#### Record Final after FT (19 Jul)

Admin live console on Match 116, sync API after ExternalMatchId mapped, or extend `import_wc2026_finished_matches.py`.
