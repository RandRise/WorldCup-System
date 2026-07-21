# WorldCup System — Changes Review

## Docs navigation

[Dashboard](index.md) | [System Overview](overview.md) | [Completion Roadmap](roadmap.md) | [Dev Workflow](workflow.md) | [API Status](api-status.md) | [Data Model](data-model.md) | [Changes Review](changes-review.md)

---
## Changes Review

Latest: **Whole-project upload gate (21 Jul 2026)** — Phases 9–12 Done in tree; ~59 modified + ~44 untracked pending commit/push; packaging checklist. Detail: [whole-project-upload-gate](changes/whole-project-upload-gate.md). Prior: **Phase 13 Planned** — company-scoped competitions (docs only). Detail: [phase-13-planned](changes/phase-13-planned.md). Prior: **Phase 9 Done — Final FT** — Argentina 0–1 Spain a.e.t.; MatchId **116**. Detail: [phase-9-ops-closeout](changes/phase-9-ops-closeout.md). Prior: **Phase 11 Done** (`p11-tests-gate`; suite **382**). Detail: [phase-11-tests-gate](changes/phase-11-tests-gate.md). Prior: **Phase 12 Done** (`p12-tests-gate`; unittest **23**; smoke **103**). Detail: [phase-12-tests-gate](changes/phase-12-tests-gate.md). Prior: Phase 11 Tasks 1–7 · Phase 12 Tasks 1–3 · Phase 10 Tasks 1–4 — see detail pages under `docs/changes/`.

> **Info:** **Whole-project upload gate.** Product review gates for Phases 9–12 are closed. Upload still **open until commit** for critical packaging (migrations + MatchSync + WC 2030 scripts). Checklist: [whole-project-upload-gate](changes/whole-project-upload-gate.md) · [Changes Review](changes-review.md).

> **Info:** **Phase 13 Planned.** Soft multi-tenancy for workplace pools — shared tournament, isolated leaderboards; invite codes; no branding/subdomains in v1. Checklist: [Phase 13](roadmap.md#phase-13) · plan: [phase-13-planned](changes/phase-13-planned.md).

> **Success:** **Phase 9 Done — Final FT recorded.** Argentina **0–1** Spain a.e.t. (Ferran Torres 106', MetLife); MatchId **116**; bets resolved **2/2**; GoalId **298** corrected Morata → Torres. Scripts: idempotent `apply_final_ft` in `add_sf2_and_final.py`; Final row + `FINAL=5` in `import_wc2026_finished_matches.py` (wipe path — prefer upsert script). No C# / migration. Commits Phase 7–8: `dcb322b` + `7886b40`. ThirdPlace skipped. Checklist: [Phase 9](roadmap.md#phase-9) · detail: [phase-9-ops-closeout](changes/phase-9-ops-closeout.md).

> **Success:** **Phase 11 Done — Task 8 gate closed.** Bugbot highs fixed (`SyncFinishedResults` cancel rethrow; `GetSquadScorers` name-only); **+2** regressions; suite **382** passed. Checklist: [Phase 11](roadmap.md#phase-11) · detail: [phase-11-tests-gate](changes/phase-11-tests-gate.md).

> **Success:** **Phase 12 Done — Task 4 gate closed.** Unittest **23** OK; smoke **103**; Bugbot highs fixed (`EnsureSameWorldCup`; `demo_kickoff_anchors`); medium H2H accepted. Checklist: [Phase 12](roadmap.md#phase-12) · detail: [phase-12-tests-gate](changes/phase-12-tests-gate.md).

> **Success:** **Phase 11 Task 7 gate closed.** `MatchService.ToHonestPlayerName` nulls `"Tournament Scorer"` on `GetLiveSnapshot` + match-detail goal/card DTOs; client `placeholder-scorer.ts` + fixtures Recent events + admin live timeline sanitize (name-only; not jersey 99 alone). Bugbot found no bugs; honesty **3** + suite **380** passed. Checklist: [Phase 11](roadmap.md#phase-11) · detail: [phase-11-recent-events-honesty](changes/phase-11-recent-events-honesty.md).

> **Success:** **Phase 12 Task 3 implemented.** Best thirds + FIFA Art. 12 R32→Final (31 matches, no ThirdPlace); feeder FKs; wipe walks TBD rows; unittest **22**; smoke seed 2030 → **103** matches. Formal close: [Task 4](changes/phase-12-tests-gate.md). Checklist: [Phase 12](roadmap.md#phase-12) · detail: [phase-12-bracket](changes/phase-12-bracket.md).

> **Success:** **Phase 11 Task 6 gate closed.** Admin `POST Match/SyncScorers/{matchId}` + `POST Match/SyncScorersForWorldCup?worldCupId=`; client `syncScorers` / `syncScorersForWorldCup`; optional `scripts/sync_scorers_backfill.ps1`. Calendar finished+orient only — no FT rewrite, bets, or knockout; empty timeline → `Skipped`; batch hard fail → `Warning`; local Goal counts in DTO. Bugbot highs fixed (cancel rethrow; local scores); backfill **14** + suite **375** passed. Checklist: [Phase 11](roadmap.md#phase-11) · detail: [phase-11-backfill](changes/phase-11-backfill.md).

> **Success:** **Phase 11 Task 5 gate closed.** `SyncResult` / `SyncFinishedResults` call timeline + scorer apply after calendar FT (fail-soft); `ScorerStatus` / `ScorerMessage` / `ScorerGoalsUpdated` on DTO; batch scorer counts + `BatchDelayMilliseconds`; client models updated. Bugbot found no bugs; wiring **8** + suite **361** passed. Checklist: [Phase 11](roadmap.md#phase-11) · detail: [phase-11-sync-wire](changes/phase-11-sync-wire.md).

> **Success:** **Phase 12 Task 2 gate closed.** After draw: 72 group matches, Poisson λ≈1.35 (cap 7), Goal + TeamStats, Tournament Scorer placeholders; `--wipe-matches` 2030-only; unittest **14** OK; smoke seed 2030 → 72 matches / 193 goals; WC 2026 untouched. Bugbot highs fixed (2026 importer WC-scoped teams/wipe; Forward position by name). No C# in Task 2. Checklist: [Phase 12](roadmap.md#phase-12) · detail: [phase-12-group-sim](changes/phase-12-group-sim.md).

> **Success:** **Phase 12 Task 1 Done (closed via Task 4).** `simulate_wc2030.py` ensures WC 2030, 6 hosts + 42 random, groups A–L; multi-cup via non-unique `Team.CountryId`; draw helpers in unittest suite (**23** total with group/bracket/anchors). Checklist: [Phase 12](roadmap.md#phase-12) · detail: [phase-12-draw](changes/phase-12-draw.md).

> **Success:** **Phase 11 Task 4 gate closed.** `ITimelineScorerApplyService` / `TimelineScorerApplyService`; in-place Goal `PlayerId` + `TimeScored` + `IsOwnGoal` when per-side counts align; count mismatch throws (goals unchanged); own goal → conceding team; timeline Event order preserved (same-minute); no bet/knockout; DI in `Program.cs`. Bugbot high fixed; apply **10** + suite **353** passed. Wired by Task 5. Checklist: [Phase 11](roadmap.md#phase-11) · detail: [phase-11-scorer-apply](changes/phase-11-scorer-apply.md).

> **Success:** **Phase 11 Task 3 gate closed.** `ITimelinePlayerResolver` / `TimelinePlayerResolver`; `Player.ExternalPlayerId`; exact → normalized → create; Bugbot highs fixed (jersey-99 filter); resolver **32** + suite **342** passed. Wired by Task 4 apply / Task 5 SyncResult. Checklist: [Phase 11](roadmap.md#phase-11) · detail: [phase-11-player-resolve](changes/phase-11-player-resolve.md).

> **Info:** **Phase 12 Done.** Tasks 1–4 complete; gate closed. Plan: [phase-12-planned](changes/phase-12-planned.md) · [tests gate](changes/phase-12-tests-gate.md).

> **Success:** **Phase 11 Task 2 gate closed.** `IExternalMatchEventsProvider` + `FifaTimelineEventsProvider`; `TimelineBaseUrl`; Goal!/OG/Penalty parse; skip unchanged-score Goal! lines; Mexico–SA fixture. Wired by Task 5. Bugbot highs fixed; timeline **32** + suite **310** passed. Checklist: [Phase 11](roadmap.md#phase-11) · detail: [phase-11-timeline-provider](changes/phase-11-timeline-provider.md).

> **Success:** **Phase 11 Task 1 gate closed.** `Match.ExternalStageId` column, migration `AddMatchExternalStageId`, SetExternal optional stage, calendar `IdStage` parse + SyncResult auto-persist, DTOs + client. No high/critical Bugbot findings; medium batch DTO refresh fixed; `dotnet test` **278** passed. Checklist: [Phase 11](roadmap.md#phase-11) · detail: [phase-11-external-stage](changes/phase-11-external-stage.md).

> **Info:** **Phase 11 Done.** Tasks 1–8 complete; gate closed (suite **382**). Plan: [phase-11-planned](changes/phase-11-planned.md) · [tests gate](changes/phase-11-tests-gate.md).

> **Success:** **Phase 10 Task 4 complete.** Removed stale root `worldcup-client` and orphan DB dump; empty cancelled admin dirs; README canonical SPA path; dual docs retained (VitePress + git mirror). Checklist: [Phase 10](roadmap.md#phase-10) · detail: [phase-10-cleanup](changes/phase-10-cleanup.md).

> **Success:** **Phase 10 Task 3 gate closed.** WC styling shipped; no high/critical Bugbot findings; medium fixes applied; Jasmine **25** + `dotnet test` **268** passed. Checklist: [Phase 10](roadmap.md#phase-10) · detail: [phase-10-styling](changes/phase-10-styling.md).

> **Success:** **Phase 10 Task 2 gate closed.** Fixtures UX shipped; no high/critical Bugbot findings; Jasmine **12** + `dotnet test` **268** passed. Checklist: [Phase 10](roadmap.md#phase-10) · detail: [phase-10-fixtures-ux](changes/phase-10-fixtures-ux.md).

> **Success:** **Phase 10 Task 1 gate closed.** Sync API + RabbitMQ decision done; Bugbot highs fixed; **268** tests green. Detail: [phase-10-match-sync](changes/phase-10-match-sync.md).

> **Success:** **Phase 9 Done.** Ops close-out complete — Final FT + script tooling + Phase 7–8 commits. Checklist: [Phase 9](roadmap.md#phase-9) · detail: [phase-9-ops-closeout](changes/phase-9-ops-closeout.md).

> **Success:** **Phase 8 implemented (13–16 Jul 2026).** User dashboard, login return URLs, bet resolve hardening (3/0 + bulk), live snapshot API, admin hub / live console, UpdateTeam wiring. Checklist: [Phase 8](roadmap.md#phase-8).

> **Success:** **Phase 7 knockout + live data.** `MatchStage` / feeders, `KnockoutService`, bracket SPA, H2H standings, offline WC 2026 scripts. External football API cancelled for live — Phase 10 adds **post-match** FIFA calendar sync only.

### Summary stats

- **Upload inventory (uncommitted):** ~**59** modified + ~**44** untracked; branch **ahead 19** of `origin/master`; packaging gate — [whole-project-upload-gate](changes/whole-project-upload-gate.md)
- **Phase 9 (Done):** Final FT Argentina **0–1** Spain a.e.t. (Ferran Torres 106'); MatchId **116**; bets **2/2**; `add_sf2_and_final.py` `apply_final_ft` (+466/−96 across 2 scripts); no C#; commits `dcb322b` + `7886b40`; ThirdPlace skipped — [phase-9-ops-closeout](changes/phase-9-ops-closeout.md)
- **Phase 11 Task 8 (gate closed):** formal phase close; Bugbot highs fixed (cancel rethrow + jersey-99 name-only); **+2** regressions; suite **382** — [phase-11-tests-gate](changes/phase-11-tests-gate.md)
- **Phase 11 Task 7 (gate closed):** omit `"Tournament Scorer"` on snapshot + match-detail DTOs (`ToHonestPlayerName`); client `placeholder-scorer.ts`; fixtures + admin live sanitize; name-only; Bugbot clean; honesty **3**; suite **380** — [phase-11-recent-events-honesty](changes/phase-11-recent-events-honesty.md)
- **Phase 12 Task 4 (gate closed):** unittest **23**; smoke **103**; Bugbot highs fixed (cross-cup + demo kickoffs); medium H2H accepted — [phase-12-tests-gate](changes/phase-12-tests-gate.md)
- **Phase 12 Task 3 (impl):** best thirds + R32→Final (31 KO); FIFA Art. 12 skeleton; unittest **22**; smoke **103** — [phase-12-bracket](changes/phase-12-bracket.md)
- **Phase 11 Task 6 (gate closed):** `SyncScorers` / `SyncScorersForWorldCup`; no FT/bets/knockout; empty timeline → `Skipped`; batch hard fail → `Warning`; client + `scripts/sync_scorers_backfill.ps1`; Bugbot highs fixed; backfill **14**; suite **375** — [phase-11-backfill](changes/phase-11-backfill.md)
- **Phase 11 Task 5 (gate closed):** SyncResult / SyncFinishedResults timeline + scorer apply (fail-soft); `ScorerStatus` DTO; batch delay; Bugbot clean; wiring **8**; suite **361** — [phase-11-sync-wire](changes/phase-11-sync-wire.md)
- **Phase 12 Task 2 (gate closed):** 72 group matches + Poisson scores + Goal/TeamStats; `--wipe-matches` 2030-only; Bugbot highs fixed (2026 importer scope); unittest (now **22** total); smoke 72/193 — [phase-12-group-sim](changes/phase-12-group-sim.md)
- **Phase 12 Task 1 (Done; gate closed via Task 4):** cup + hosts/random draw + multi-cup `Team.CountryId` — [phase-12-draw](changes/phase-12-draw.md)
- **Phase 11 Task 4 (gate closed):** `TimelineScorerApplyService` + `ITimelineScorerApplyService` + `TimelineScorerApplyResult`; in-place Goal rewrite; count mismatch throws; same-minute order fix; DI; **10** apply tests; suite **353** — [phase-11-scorer-apply](changes/phase-11-scorer-apply.md)
- **Phase 11 Task 3 (gate closed):** `TimelinePlayerResolver` + `ExternalPlayerId`; 32 resolver tests; suite **342** — [phase-11-player-resolve](changes/phase-11-player-resolve.md)
- **Phase 11 Task 2 (gate closed):** `FifaTimelineEventsProvider` + `IExternalMatchEventsProvider`; DTOs; `TimelineBaseUrl`; 32 provider tests; suite **310** — [phase-11-timeline-provider](changes/phase-11-timeline-provider.md)
- **Phase 11 Task 1 (gate closed):** `ExternalStageId` varchar(64) nullable; migration `20260720120000_AddMatchExternalStageId`; Program.cs IF NOT EXISTS; SetExternal optional stage; calendar `IdStage`; SyncResult auto-persist; MatchDTO + `api.models.ts` — [phase-11-external-stage](changes/phase-11-external-stage.md)
- **Phase 11 checklist:** Tasks 1–8 complete (suite **382**)
- **Phase 10 Task 4 (done):** deleted workspace-root `worldcup-client` + `TestDatabase_backup_*.dump` (**outside git**); empty `admin/seed` + `admin/reference`; README + dual docs policy; docs/VitePress updates **in git**
- **Phase 10 Task 3 (gate closed):** black/gold theme, night-pitch atmosphere, Bebas Neue + Manrope, brand-first home; medium Bugbot fixes (pending pill, amber warnings, reduced-motion, admin finished)
- **Phase 10 Task 2 (gate closed):** `fixture-sections.ts` + 12 Jasmine specs, fixtures component — Action default, chips, sections, jump CTA
- **Phase 10 Task 1 committed** (`dcb322b`): MatchSync services, `ExternalMatchId` migration, Admin sync endpoints, client API, tests
- **Phase 10 checklist:** Tasks 1–4 complete
- **Tests:** Jasmine **25** / 25; API `dotnet test` **382** / 382 (Phase 11 Done)

### Phase 9 Roadmap Mapping

| Task | Status | Evidence |
| --- | --- | --- |
| Schedule Final (Argentina vs Spain, 19 Jul) | **Done** | MatchId **116**; MetLife; `add_sf2_and_final.py` |
| Record Final result after FT | **Done** | 0–1 a.e.t. Ferran Torres 106'; bets **2/2**; [detail](changes/phase-9-ops-closeout.md) |
| Optional ThirdPlace match | **Skipped** | Not used for this tournament path |
| Apply `AddMatchStage` + Feeder* repair | **Done** | `MigrateAsync` + IF NOT EXISTS in `Program.cs` |
| Commit Phase 7–8 working tree | **Done** | `dcb322b` + `7886b40` |
| Knockout re-advance UX | **Done** | Goal warning + Schedule Advance winner |

### Phase 13 Roadmap Mapping

| Roadmap item | Status | Detail |
| --- | --- | --- |
| Docs / planned backlog | **Done** | [phase-13-planned](changes/phase-13-planned.md) |
| `p13-company-model` — Company + User.CompanyId | Planned | same |
| `p13-company-api` — create / join / admin | Planned | same |
| `p13-leaderboard-scope` — company filter | Planned | same |
| `p13-spa` — join + company board | Planned | same |
| `p13-tests-gate` — tests + review | Planned | same |

### Phase 12 Roadmap Mapping

| Task | Status | Evidence |
| --- | --- | --- |
| Docs / planned backlog | **Done** | [phase-12-planned](changes/phase-12-planned.md) |
| `p12-draw` — cup, hosts+random, groups A–L | **Done** | Script + migration + TeamService per-WC unique; [detail](changes/phase-12-draw.md) |
| `p12-group-sim` — 72 group matches + scores | **Done** (gate closed) | Schedule + Poisson + wipe; Bugbot highs fixed; unittest **14**; smoke 72/193; [detail](changes/phase-12-group-sim.md) |
| `p12-bracket` — best thirds + R32→Final | **Done** | FIFA Art. 12 skeleton; 31 KO; unittest **22**; smoke 103; [detail](changes/phase-12-bracket.md) |
| `p12-tests-gate` — smoke + review | **Done** (gate closed) | Unittest **23**; smoke **103**; Bugbot highs fixed; [detail](changes/phase-12-tests-gate.md) |

### Phase 11 Roadmap Mapping

| Task | Status | Evidence |
| --- | --- | --- |
| `p11-external-stage` — IdStage for timeline URLs | **Done** | Column + migration + SetExternal + calendar parse + SyncResult persist; **278** tests; [detail](changes/phase-11-external-stage.md) |
| `p11-timeline-provider` — FIFA timeline Goal! parse | **Done** | Provider + DTOs + `TimelineBaseUrl` + DI + unit tests; [detail](changes/phase-11-timeline-provider.md) |
| `p11-player-resolve` — FIFA player → local Player | **Done** | `TimelinePlayerResolver` + `ExternalPlayerId`; **32** resolver + suite **342**; [detail](changes/phase-11-player-resolve.md) |
| `p11-scorer-apply` — Idempotent Goal rewrite | **Done** | `TimelineScorerApplyService`; count mismatch throws; same-minute order; **10** + suite **353**; [detail](changes/phase-11-scorer-apply.md) |
| `p11-sync-wire` — SyncResult / SyncFinishedResults | **Done** | Fail-soft timeline + apply; Bugbot clean; wiring **8**; suite **361**; [detail](changes/phase-11-sync-wire.md) |
| `p11-backfill` — Scorers-only Admin batch | **Done** | `SyncScorers` / `SyncScorersForWorldCup` + script; suite **375**; [detail](changes/phase-11-backfill.md) |
| `p11-recent-events-honesty` — Hide placeholders | **Done** | `ToHonestPlayerName` + client sanitizer; suite **380**; [detail](changes/phase-11-recent-events-honesty.md) |
| `p11-tests-gate` — Review + `dotnet test` | **Done** | Bugbot highs fixed; suite **382**; [detail](changes/phase-11-tests-gate.md) |

### Phase 10 Roadmap Mapping

| Task | Status | Evidence |
| --- | --- | --- |
| `p10-match-sync` — Post-match FIFA sync + resolve | **Done** | `FifaCalendarMatchResultProvider`, `MatchResultSyncService`, `ExternalMatchId`, Admin sync endpoints, 19 new tests |
| Explicit MatchStatus + home/away orientation | **Done** | Provider requires status; sync orients/swaps sides |
| RabbitMQ decision — v1 without broker | **Done** | Documented; in-process Admin/API only |
| Client sync surface | **Done** | `match-api.service.ts` + sync DTOs |
| `p10-fixtures-ux` — Bettable matches first | **Done** | Action default; chips; Open/Live/Finished; jump fixed; Jasmine **12**; [detail](changes/phase-10-fixtures-ux.md) |
| `p10-wc-styling` — WC visual polish | **Done** | Theme tokens, atmosphere, typography; Jasmine **25** + API **268**; [detail](changes/phase-10-styling.md) |
| `p10-cleanup` — Duplicates & unused | **Done** | Root client + dump removed; dual docs kept; [detail](changes/phase-10-cleanup.md) |

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

> **Success:** **Product gates cleared (Phases 9–12).** Phase 11 Task 8 suite **382**; Phase 12 Task 4 unittest **23** / smoke **103**; Phase 10 Tasks 1–4 closed. **Upload packaging** still open until commit — see [whole-project-upload-gate](changes/whole-project-upload-gate.md).

#### Upload packaging (must clear before / during commit)

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Omit EF migrations (`ExternalStageId`, `ExternalPlayerId`, `AllowMultipleTeamsPerCountry`) | Critical | **Open until commit** | Include migration + Designer files + snapshot |
| Omit Phase 11 MatchSync providers / unit tests | Critical | **Open until commit** | Include untracked MatchSync + Timeline* tests |
| Omit Phase 12 `simulate_wc2030.py` / unittest | High | **Open until commit** | Include scripts + TeamService multi-cup |
| Dual docs VitePress vs git mirror drift | High | **Cleared this pass** | Mirror Markdown both trees |
| Accidental WC 2026 wipe via importer | High | **Documented** | Prefer `add_sf2_and_final.py` |
| Unfixed Phase 11/12 Bugbot highs | High | **Cleared** | See Task 8 / Task 4 gates |

High/critical items that were fixed **before** declaring phase Done (historical):

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Phase 9 — wrong Final scoreline / scorer | High | **Cleared** | DB 0–1; GoalId **298** → Ferran Torres 106' |
| Phase 9 — bets open after FT | High | **Cleared** | MatchId **116** bets **2/2** resolved |
| Phase 9 — accidental wipe via importer | High | **Documented** | Prefer `add_sf2_and_final.py`; importer warns in docstring |
| Phase 9 — C# / migration drift | Critical | **N/A** | No C# in close-out |
| Phase 11 Task 8 Bugbot highs/criticals | High (gate) | **Cleared** | Cancel rethrow + jersey-99 name-only; [tests-gate](changes/phase-11-tests-gate.md) |
| Phase 11 Task 8 — `dotnet test` green | High (gate) | **Cleared** | Suite **382** |
| Phase 11 Task 8 — roadmap / plan Status → Done | High (acceptance) | **Cleared** | Phase 11 Done |
| Phase 11 Task 7 — never show literal `Tournament Scorer` | High (acceptance) | **Cleared by design** | API nulls name; fixtures skip empty `playerName` |
| Phase 11 Task 7 — match detail / admin live same rule | High (acceptance) | **Cleared by design** | `BuildTeamStatsDto` + `allEvents()` |
| Phase 11 Task 7 — real names still shown | High | **Cleared by design** | Name-only omit of placeholder string |
| Phase 11 Task 7 Bugbot highs/criticals | High (gate) | **Cleared** | Bugbot found no bugs |
| Phase 11 Task 7 — honesty unit suite | High (gate) | **Cleared** | Honesty **3** + suite **380** |
| Phase 11 Task 6 — no FT rewrite / bets / knockout on SyncScorers | High | **Cleared** | ScoreChanged=false; BetsResolved=0; no ApplyScoreIdempotent |
| Phase 11 Task 6 — Applied only for Applied/AlreadyMatched | High | **Cleared** | Skipped/Warning → Applied=false |
| Phase 11 Task 6 — empty timeline → Skipped | High | **Cleared** | `skipWhenNoTimelineGoals` |
| Phase 11 Task 6 — batch hard fail → Warning | High | **Cleared** | Unlike SyncFinishedResults (null ScorerStatus) |
| Phase 11 Task 6 — cancel not swallowed | High | **Cleared** | Rethrow in core + batch catch |
| Phase 11 Task 6 — BatchDelayMilliseconds on WC backfill | High | **Cleared** | Same option as FT batch |
| Phase 11 Task 6 Bugbot highs/criticals | High (gate) | **Cleared** | Cancel swallow + local scores |
| Phase 11 Task 6 — backfill unit suite | High (gate) | **Cleared** | **14** cases; suite **375** |
| Phase 11 Task 5 Bugbot highs/criticals | High (gate) | **Cleared** | No bugs found |
| Phase 11 Task 5 — wiring unit suite | High (gate) | **Cleared** | **8** wiring cases; suite **361** |
| Phase 11 Task 5 — FT still succeeds when timeline throws | High | **Cleared** | Fail-soft → `ScorerStatus=Warning` |
| Phase 11 Task 5 — missing stage does not abort FT | High | **Cleared** | `Skipped`; no timeline HTTP |
| Phase 11 Task 5 — batch continues on scorer warning | High | **Cleared** | Per-match scorer status; hard FT errors still isolated |
| Phase 11 Task 5 — orientation to apply | High | **Cleared** | `homeMapsToTeamOne` from calendar orient |
| Phase 11 Task 4 Bugbot highs/criticals | High (gate) | **Cleared** | Same-minute fix; suite **353** |
| Phase 11 Task 4 — apply unit suite | High (gate) | **Cleared** | **10** apply + suite **353** |
| Phase 11 Task 4 — count mismatch mutates goals | High | **Cleared by design** | Throws; no Goal writes / SaveAsync |
| Phase 11 Task 4 — scorer-only re-resolve bets / knockout | High | **Cleared by design** | No bet/knockout deps; test asserts |
| Phase 11 Task 4 — own-goal wrong team | High | **Cleared** | Conceding team id to resolver |
| Task 3 Bugbot highs/criticals | High (gate) | **Cleared** | Jersey-99 filter fixed; suite **342** |
| Task 3 — resolver unit suite | High (gate) | **Cleared** | 32 resolver cases |
| Task 3 — ambiguous name / cross-team id | High | **Cleared** | Throws `InvalidOperationException` |
| Task 3 — Tournament Scorer when real name exists | High | **Cleared** | Eligible squad excludes placeholder by name |
| Task 2 Bugbot highs/criticals | High (gate) | **Cleared** | Timeline provider gate closed; suite **310** |
| Task 2 — provider unit suite | High (gate) | **Cleared** | Mexico–SA + 32 provider tests |
| Task 1 — `ExternalStageId` unit tests | High | **Cleared** | SetExternal + SyncResult stage cases; suite **278** |
| Task 1 Bugbot highs/criticals | High (gate) | **Cleared** | No high/critical; medium batch DTO refresh fixed |
| Phase 10 Task 4 — wrong SPA deleted | Critical | **Cleared** | Root stale client only; `WorldCup-System/worldcup-client` kept |
| Phase 10 Task 4 — git docs tree deleted | High | **Cleared** | Dual docs retained (VitePress + git mirror) |
| Phase 10 Task 4 — broken client path in docs/README | High | **Cleared** | Canonical path documented |
| Phase 10 Task 4 Bugbot highs/criticals | High (gate) | **None** | Filesystem/docs cleanup only |
| Phase 10 Task 3 Bugbot review | High (gate) | **Cleared** | Mediums fixed; Jasmine **25** + API **268** |
| Phase 10 Task 2 Bugbot review | High (gate) | **Cleared** | No high/critical findings |
| Live snapshot stale `canBet` | High | **Fixed** | `canBet` forced false when live status ≠ `Scheduled` |
| Sync missing MatchStatus / home-away orientation | High | **Fixed** | Explicit status + side orientation in MatchSync |
| MatchResultSync unit tests | High | **Fixed** | 19 new tests; suite 268 green (pre–Task 1 stage) |
| Parallel possession updates race (live console) | High | **Fixed** | Sequential `updateTeamStats` + possession sum check |
| Edited applied migration `IdentityLongKeys` | Critical | **Cleared** | Forward `AddMatchStage` only |

Accepted / medium (non-blocking):

| Item | Severity | Status | Notes |
| --- | --- | --- | --- |
| Task 7 — filter jersey 99 only for display | Medium | **Rejected** | Name-only; jersey 99 may be real (Task 3) |
| Admin live picker still excludes jersey 99 | Low | Accepted | Picker UX; display honesty remains name-only |
| Apply not wired to SyncResult | Info | **Cleared** | Task 5 wired fail-soft |
| BatchDelayMilliseconds default 250 | Medium | Accepted v1 | Tests force `0`; FIFA public API courtesy |
| Sync message appends scorer note always on FT | Low | Accepted | `Scorers: {scorerMessage}` even when Skipped |
| Auto-create named Forwards | Medium | Accepted v1 | Prefer named player over Tournament Scorer |
| Name truncate to 64 chars | Medium | Accepted | Matches `CK_Player_Name_Length` |
| `AddPlayerExternalPlayerId` migrate | Medium | Ops | Migrate + IF NOT EXISTS on API start |
| Timeline HTTP / empty payload | Medium | Documented | Provider throws; Task 5 catches as scorer warning |
| Own Goal / Penalty label variants | Medium | Accepted v1 | Common `TypeLocalized` strings; Mexico sample was `Goal!` only |
| Side inference via score delta | Medium | Accepted v1 | No FIFA HomeOrAway on event; ambiguous OG defaults away |
| Zip by minute order only | Medium | Accepted v1 | Same-minute goals ordered by ExternalPlayerId / name |
| `AddMatchExternalStageId` migrate | Medium | Ops | Migrate + IF NOT EXISTS on API start |
| Squad round-robin scorers | Medium | Accepted | Better than Tournament Scorer; real names = Phase 11 Tasks 3–4 |
| Google Fonts CDN / CSP / offline | Medium | Open | Fallbacks in `--font-*`; self-host later if needed |
| `color-mix` / `backdrop-filter` | Medium | Accepted v1 | Readable without blur on older engines |
| Gold-on-dark contrast | Medium | Mitigated | Dark text on gold CTAs; bright gold accents |
| Jump scroll after filter change | Medium | **Fixed** | Live/Finished → Action; `setTimeout` before scroll |
| Unknown status → Open bucket | Medium | Accepted v1 | [phase-10-fixtures-ux](changes/phase-10-fixtures-ux.md) |
| Re-sync placeholder scorers | Medium | Accepted | Score-only model until timeline apply |
| Fixtures auto-resolve / TBD update / stage lock | Medium | **Fixed** | Snapshot on load; nullable UpdateMatch |
| `AddMatchStage` + Feeder* + `ExternalMatchId` migrate | Medium | Ops | Migrate on API start |
| Silent swallow of incomplete-stats / advance errors | Medium | Accepted | Optional Serilog later |

### Risk Summary

| Risk | Severity | Details | Action |
| --- | --- | --- | --- |
| Upload omits migrations / MatchSync | Critical | Working tree has untracked schema + providers | **Open until commit** — [upload gate](changes/whole-project-upload-gate.md) |
| Dual docs out of sync on push | High | VitePress `docs/` vs git `WorldCup-System/docs/` | Mirror both; dual-docs policy |
| Phase 11 Task 7 shows `Tournament Scorer` as real | High | Snapshot / timelines must omit placeholder name | **Cleared by design** — [honesty](changes/phase-11-recent-events-honesty.md) |
| Phase 11 Task 7 Bugbot / unit suite | High | Gate closed | Bugbot clean; honesty **3**; suite **380** |
| Phase 11 Task 6 accidentally changes FT / bets | High | Scorers-only path must skip ApplyScoreIdempotent | **Cleared** — [backfill](changes/phase-11-backfill.md) |
| Phase 11 Task 6 empty timeline Warning noise | High | Backfill should Skip, not count-mismatch | **Cleared** — `skipWhenNoTimelineGoals` |
| Phase 11 Task 6 Bugbot / unit suite | High | Gate closed | **Cleared** — suite **375** |
| Phase 12 Task 2 wipe scope | High | Must not delete WC 2026 matches | Cleared — team-scoped wipe; smoke OK |
| Phase 12 Task 2 redraw without wipe | Medium | Orphan fixtures / team reassignment | Cleared — exit unless `--wipe-matches` |
| Phase 12 Task 4 / formal Bugbot | High (gate) | Unittest **23**; smoke **103**; highs fixed | **Cleared** — [tests-gate](changes/phase-12-tests-gate.md) |
| Phase 11 Task 5 Bugbot / review gate | High | Cleared | No bugs; suite **361** |
| Phase 11 Task 5 wiring unit tests | High | Cleared | **8** wiring cases; suite **361** |
| Phase 11 Task 4 Bugbot / review gate | High | Cleared | Suite **353** |
| Phase 11 Task 4 apply unit tests | High | Cleared | Suite **353** |
| Phase 11 Task 4 not wired into SyncResult | Info | **Cleared** | Task 5 |
| Auto-create players on miss | Medium | New Forward rows during apply/resolve | Accepted; monitor jersey exhaustion |
| Task 3 Bugbot / review gate | High | Cleared | Suite **342** |
| Task 2 Bugbot / review gate | High | Cleared | Suite **310** |
| Task 2 not wired into SyncResult | Info | **Cleared** | Task 5 |
| Task 1 ExternalStageId tests | High | Cleared | Suite **278** |
| Task 1 Bugbot / review gate | High | Cleared | No high/critical |
| Phase 10 Task 4 review/test gate | High | Highs cleared | Jasmine + `dotnet test` from canonical paths |
| Phase 10 Task 3 review/test gate | High | Closed | Jasmine **25** + API **268** |
| Phase 10 Task 2 review/test gate | High | Closed | Jasmine **12** + API **268** |
| Migration rewrite (IdentityLongKeys) | Critical | Removed from working tree | Cleared |
| Sync orientation / MatchStatus | High | Fixed in Phase 10 Task 1 | Cleared |
| Sync deletes Admin goals on score change | Medium | Squad/placeholder scorers when counts differ | Accepted until timeline apply |
| External Google Fonts | Medium | CDN in `index.html` | Fallbacks; monitor CSP |
| Outside-git deletes invisible in `git diff` | Low | Root client + dump never tracked | Documented in [phase-10-cleanup](changes/phase-10-cleanup.md) |
| FIFA calendar/timeline ToS / season ids | Medium | Public JSON; config-driven | Monitor; paid provider fallback |
| Final not yet finished in DB | Low | Kickoff 19 Jul | **Cleared** — Phase 9 Done; MatchId 116 FT recorded |
| Accidental WC 2026 wipe via importer | High | `import_wc2026_finished_matches.py` deletes then reloads | Documented — prefer `add_sf2_and_final.py` |
| Missing `ExternalStageId` blocks timeline URL | Medium | Timeline needs stage | Backfill via SetExternal or SyncResult |

### Per-File Summary

#### Whole-project upload gate


**[upload-packaging]**

New: `docs/changes/whole-project-upload-gate.md` — inventory of uncommitted Phases 9–12 + Phase 10 leftovers + Phase 13 plan; critical must-include migrations/MatchSync/scripts; dual-docs mirror checklist. Working tree: ~59 modified + ~44 untracked; branch ahead **19**. Product Bugbot highs cleared; packaging open until commit. Detail: [whole-project-upload-gate](changes/whole-project-upload-gate.md).

#### Multi-cup SPA context (Phase 12 companion)


**[p12-draw · spa]**

Modified: `worldcup-context.service.ts` — sort cups by year; persist `worldcup.selectedId` in localStorage; resolve stored / current / preferred 2026 then first. Selector + fixtures / standings / dashboard / bracket / leaderboard / admin hub·schedule·live reload against selected cup. Supports WC 2026 + WC 2030 side by side after seed.

#### Phase 9 — Final FT ops close-out


**[p9-final-ft · p9-ops]**

Modified (uncommitted): `scripts/add_sf2_and_final.py` — idempotent `apply_final_ft` (insert 0–1 or correct scorer to Ferran Torres @ 106'); WC 2026–scoped teams; squad Forward preference; no wipe. Modified: `scripts/import_wc2026_finished_matches.py` — `FINAL = 5`; Final `MATCHES` row Argentina 0–1 Spain; `wipe_world_cup_matches` (2026-only); prefer upsert script for named Torres. Local DB: MatchId **116**; GoalId **298** Morata → Torres; bets **2/2**. No C# / migration. Commits already Done: `dcb322b`, `7886b40`. **Gate cleared** — [phase-9-ops-closeout](changes/phase-9-ops-closeout.md).

#### Recent events honesty — Task 7


**[p11-recent-events-honesty]**

Modified (uncommitted): `Core/Services/Matches/MatchService.cs` — `ToHonestPlayerName` on `GetLiveSnapshot` recent events + `BuildTeamStatsDto` goal/card `PlayerName`; reuses `MatchResultSyncService.PlaceholderScorerName`. New (untracked): `worldcup-client/src/app/core/utils/placeholder-scorer.ts` — `PLACEHOLDER_SCORER_NAME` / `toHonestPlayerName`. Modified: `fixtures.component.ts` — map snapshot `recentEvents` through sanitizer; `admin-live-console.component.ts` — timeline labels omit placeholder; picker uses shared constant (+ jersey 99 for picker only). Name-only display rule (not jersey 99 alone). **Gate closed** — Bugbot clean; honesty **3**; suite **380**. Detail: [phase-11-recent-events-honesty](changes/phase-11-recent-events-honesty.md).

#### Scorers-only backfill — Task 6


**[p11-backfill]**

Modified (uncommitted): `IMatchResultSyncService` + `MatchResultSyncService` — `SyncScorers` / `SyncScorersForWorldCup`; calendar finished + `OrientScoresToLocalSides` + stage persist only; **no** `ApplyScoreIdempotent` / bets / knockout; `TryApplyTimelineScorersForBackfillAsync` (`skipWhenNoTimelineGoals: true` → empty Goal! list = `Skipped`); `Applied=true` when `ScorerStatus` is `Applied` or `AlreadyMatched`; `ScoreChanged=false`; `BetsResolved=0`; batch catch sets `ScorerStatus=Warning` (unlike FT batch); `BatchDelayMilliseconds` between matches. `MatchController` — Admin `POST SyncScorers/{matchId}`, `POST SyncScorersForWorldCup?worldCupId=`. Client — `match-api.service.ts` `syncScorers` / `syncScorersForWorldCup`. Untracked: `scripts/sync_scorers_backfill.ps1`. **Gate closed** — Bugbot highs fixed; backfill **14**; suite **375**. Detail: [phase-11-backfill](changes/phase-11-backfill.md).

#### WC 2030 smoke + review gate — Phase 12 Task 4


**[p12-tests-gate]**

Formal phase close for Tasks 1–3: unittest **23** OK; smoke `--seed 2030 --wipe-matches` → **103** matches; `demo_kickoff_anchors` (group Finished / KO Scheduled); `EnsureSameWorldCup` on Team assign/update; Bugbot highs cleared; medium H2H accepted. Detail: [phase-12-tests-gate](changes/phase-12-tests-gate.md).

#### WC 2030 group simulation — Phase 12 Task 2


**[p12-group-sim]**

Extended `scripts/simulate_wc2030.py`: `build_group_stage_schedule` (72 fixtures, 3 non-overlapping matchdays from 2030-06-13), Poisson λ=1.35 (cap 7), `simulate_group_stage` inserts Match + TeamStats + Goal, Tournament Scorer for empty squads, stadium cycle, `--wipe-matches` / `--skip-group-sim`, block redraw when 2030 matches exist. Tests: `test_simulate_wc2030_draw.py` **14** cases. No C# / migrations. Detail: [phase-12-group-sim](changes/phase-12-group-sim.md).

#### WC 2030 draw — Phase 12 Task 1


**[p12-draw]**

Cup ensure, hosts+random 42, groups A–L; multi-cup `Team.CountryId`. Detail: [phase-12-draw](changes/phase-12-draw.md).

#### Sync wire — Task 5


**[p11-sync-wire]**

Modified (uncommitted): `MatchResultSyncService` — inject `IExternalMatchEventsProvider` + `ITimelineScorerApplyService` + `IOptions<MatchResultSyncOptions>`; after FT + bets/advance call fail-soft `TryApplyTimelineScorersAsync`; merge scorer warning into `Warning`; batch `ScorersApplied` / `ScorerWarnings` + `BatchDelayMilliseconds` between matches. `MatchResultSyncDTO` — `SyncScorerStatuses`, `ScorerStatus` / `ScorerGoalsUpdated` / `ScorerMessage`, batch counts. `MatchResultSyncOptions` + `appsettings.json` — `BatchDelayMilliseconds` (default 250). Tests — constructor Moqs + **8** wiring cases (`BatchDelayMilliseconds = 0`). Client — `api.models.ts` `scorerStatus` / `scorerGoalsUpdated` / `scorerMessage` / `scorersApplied` / `scorerWarnings`. Calendar FT still succeeds when timeline/apply throws (`ScorerStatus=Warning`). Missing `ExternalStageId` → `Skipped`. **Gate closed** — Bugbot clean; suite **361**. Detail: [phase-11-sync-wire](changes/phase-11-sync-wire.md).

#### Real-scorer apply — Task 4


**[p11-scorer-apply]**

New: `ITimelineScorerApplyService`, `TimelineScorerApplyService`, `TimelineScorerApplyResult`, `TimelineScorerApplyServiceTests` (10 cases). Modified: `Program.cs` scoped DI. Orient timeline home/away via `homeMapsToTeamOne`; compare per-side counts; mismatch → throw (no Goal writes); align → in-place `PlayerId` / `TimeScored` / `IsOwnGoal`; own goal → resolve on conceding team; `SaveAsync` only when changed; no bet/knockout. Wired by Task 5 SyncResult. Detail: [phase-11-scorer-apply](changes/phase-11-scorer-apply.md).

#### Player identity resolver — Task 3


**[p11-player-resolve]**

New: `ITimelinePlayerResolver`, `TimelinePlayerResolver`, `TimelinePlayerResolveResult`, `TimelinePlayerMatchMethod`, migration `20260720140000_AddPlayerExternalPlayerId` (+ Designer). Modified: `Player.ExternalPlayerId`; `ApplicationDbContext` unique filtered index; model snapshot; `Program.cs` scoped DI + `IF NOT EXISTS` column/index; `PlayerDTO` + `PlayerService` map; client `api.models.ts` `externalPlayerId`. Resolve: ExternalPlayerId → exact name → normalized → create Forward (jersey 1–98). Ambiguous / cross-team id → throw. Used by Task 4 apply / Task 5 SyncResult. Detail: [phase-11-player-resolve](changes/phase-11-player-resolve.md).

#### FIFA timeline provider — Task 2


**[p11-timeline-provider]**

New: `FifaTimelineEventsProvider`, `IExternalMatchEventsProvider`, `ExternalMatchEvents`, `ExternalMatchGoalEvent`, `FifaTimelineEventsProviderTests`. Modified: `MatchResultSyncOptions.TimelineBaseUrl`, `appsettings.json`, `Program.cs` HttpClient registration. Parses Goal! / Own Goal / Penalty Goal; minute + display name helpers; throws on HTTP/empty/missing ids (Task 5 soft-fail). Detail: [phase-11-timeline-provider](changes/phase-11-timeline-provider.md).

#### External stage id — Task 1


**[p11-external-stage]**

`Match.ExternalStageId` (varchar 64 nullable); migration `20260720120000_AddMatchExternalStageId`; Program.cs IF NOT EXISTS; `SetExternalMatchIdDTO` optional stage; calendar `IdStage` → `ExternalMatchResult`; `PersistExternalStageIdIfNeededAsync` on SyncResult; MatchDTO + client `externalStageId`; options document timeline path reuse of IdCompetition/IdSeason. Detail: [phase-11-external-stage](changes/phase-11-external-stage.md).

#### MatchSync — squad scorer preference (same Task 1 tree)


**[p11-external-stage · related]**

When FT goal counts change, prefer seeded forwards / non-placeholder squad (cycle by number) before `"Tournament Scorer"`. Tests: placeholder-only, midfielder fallback, cycle. Not real FIFA timeline scorers.

#### Admin live console — hide placeholders


**[related]**

`playersForTeam` filters `Tournament Scorer` / number `99` from goal/card pickers.

#### Project cleanup — Phase 10 Task 4


**[p10-cleanup]**

Workspace deletes **outside git**: root `worldcup-client/`, `TestDatabase_backup_*.dump`. Empty untracked dirs removed under admin. Docs in git: `phase-10-cleanup.md` (new), roadmap/index/changes-review/planned, VitePress sidebar. Dual docs kept. Detail: [phase-10-cleanup](changes/phase-10-cleanup.md).

#### WC styling (theme + shell + home + accents) — Task 3


**[p10-wc-styling]**

Client theme: `styles.scss` black/gold tokens + night-pitch atmosphere + motions; `index.html` Bebas Neue / Manrope; shell glass topbar + shimmer brand; home brand-first hero (no cards); fixtures/leaderboard/auth/dashboard/bracket/bets gold + display typography. Detail: [phase-10-styling](changes/phase-10-styling.md).

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


**[p8-live-snapshot · p7-knockout · p10-match-sync · p11-external-stage]**

Stage on create/update; nullable teams + feeders in DTOs; batch live snapshot; Admin post-match sync actions; `ExternalStageId` on list/detail DTOs.

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


**[p8-live-snapshot · p8-admin-leaderboard · p7-knockout · p10-match-sync · p10-fixtures-ux · p11-external-stage · p11-player-resolve · p11-scorer-apply · p11-sync-wire]**

30s snapshot poll (+ fixtures Task 2 `canBet` clear); resolve breakdown; live console links; Knockout DI + Feeder* startup repair SQL; MatchResultSync calendar + timeline HttpClient registration; `ITimelinePlayerResolver` + `ITimelineScorerApplyService` DI; SyncResult fail-soft timeline apply (Task 5); ExternalStageId + ExternalPlayerId IF NOT EXISTS; fixtures sectioned UI.

#### Scripts + tests + docs


**[coverage · ops · p9-final-ft · p11 · p12 · upload]**

WC 2026 group/results import scripts (through Final; prefer `add_sf2_and_final.py` for Final-only); Phase 12 `simulate_wc2030.py` + `test_simulate_wc2030_draw.py` (**23**); optional `sync_scorers_backfill.ps1`, `seed_wc2026_players.py`, `scripts/data/wc2026_players.csv`. Unit tests: MatchSync timeline/resolve/apply/wire/backfill + TeamService multi-cup (suite **382**). Docs: Phase 9–13 change pages + [whole-project-upload-gate](changes/whole-project-upload-gate.md); VitePress sidebar updated.

### Prior Phases — Committed

| Phase | Status | Detail page |
| --- | --- | --- |
| Phase 1 — Foundation | Committed | [Phase 1 detail](changes/phase-1-identity.md) |
| Phase 2 — Tournament setup | Committed | [Phase 2 detail](changes/phase-2-tournament-setup.md) |
| Phase 3 — Match lifecycle | Committed | [Phase 3 detail](changes/phase-3-match-lifecycle.md) |
| Phase 4 — Betting | Committed | [Phase 4 detail](changes/phase-4-betting.md) |
| Phase 5 — Frontend SPA | Committed | [Phase 5 detail](changes/phase-5-frontend-spa.md) |
| Phase 6 — Quality & Operations | Committed | [Phase 6 detail](changes/phase-6-quality-ops.md) |
| Phase 7 — Bugfixes & Live Data | **Done** · committed (`dcb322b` / `7886b40`) | [Phase 7 detail](changes/phase-7-bugfixes.md) |
| Phase 8 — User & Admin Dashboards | **Done** · committed (`7886b40`) | [Phase 8 detail](changes/phase-8-dashboards.md) |
| Phase 9 — Ops & Tournament Close-Out | **Done** (Final FT 21 Jul) | [Phase 9 detail](changes/phase-9-ops-closeout.md) |
| Phase 10 — Match Sync, Fixtures UX, Styling & Cleanup | **Done** (Tasks 1–4; cleanup 20 Jul) | [sync](changes/phase-10-match-sync.md) · [fixtures UX](changes/phase-10-fixtures-ux.md) · [styling](changes/phase-10-styling.md) · [cleanup](changes/phase-10-cleanup.md) · [plan](changes/phase-10-planned.md) |
| Phase 11 — Real Goalscorers (FIFA Timeline) | **Done** | [Task 8 gate](changes/phase-11-tests-gate.md) · [Task 1](changes/phase-11-external-stage.md) · [Task 2](changes/phase-11-timeline-provider.md) · [Task 3](changes/phase-11-player-resolve.md) · [Task 4](changes/phase-11-scorer-apply.md) · [Task 5](changes/phase-11-sync-wire.md) · [Task 6](changes/phase-11-backfill.md) · [Task 7](changes/phase-11-recent-events-honesty.md) · [plan](changes/phase-11-planned.md) |
| Phase 12 — 2030 World Cup Simulation | **Done** | [plan](changes/phase-12-planned.md) · [draw](changes/phase-12-draw.md) · [group sim](changes/phase-12-group-sim.md) · [bracket](changes/phase-12-bracket.md) · [tests gate](changes/phase-12-tests-gate.md) |
| Phase 13 — Company-Scoped Competitions | **Planned** | [plan](changes/phase-13-planned.md) |

### Suggested Next Steps

#### Upload packaging

Commit/push the uncommitted Phases 9–12 tree using the must-include checklist — [whole-project-upload-gate](changes/whole-project-upload-gate.md). Include all three EF migrations + MatchSync providers + WC 2030 scripts + dual docs.

#### Phase 13 — Company-scoped competitions

After upload (or in parallel docs-only): `Company` + invite join + company-only leaderboard — [phase-13-planned](changes/phase-13-planned.md) · [Roadmap Phase 13](roadmap.md#phase-13).

#### Maintenance / polish

Phases **1–12** Done. Prefer ops hygiene (do not re-run the WC 2026 wipe importer unless a full reload is intended) — [Phase 9 detail](changes/phase-9-ops-closeout.md) · [roadmap](roadmap.md).
