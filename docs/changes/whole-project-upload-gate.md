# WorldCup System — Whole-Project Upload Gate

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 11 gate](phase-11-tests-gate.md) · [Phase 12 gate](phase-12-tests-gate.md)

## Whole-project upload readiness (Phases 9–13 docs + 10 leftovers + 11–12 code)

Opened **21 Jul 2026**. **Status (27 Jul 2026): packaging closed on origin.** Commits: `ac0608e` (Phases 11–12 + upload gate) · `5fdd1c4` (Phase 13, pushed — Phase 14 Task 1 **Done**). This page is **historical** — do not re-run the packaging checklist. Deploy work: [phase-14-planned](phase-14-planned.md) (Tasks 1–6 Done — [phase-14-migrate-smoke](phase-14-migrate-smoke.md); next Task 7 gate).

> **Success (docs gate):** Phase detail pages for **10–13** map to [roadmap](../roadmap.md). Product gates closed. **Do not** treat “omit migrations / MatchSync” as still open — those landed in git. For deploy work use [phase-14-planned](phase-14-planned.md).

### Review gate

High/critical items that must be clear **before** treating the upload as complete (commit/push). These are **upload packaging** risks — phase Bugbot highs were already cleared in Tasks 8 / 4.

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Omit EF migrations from commit | **Critical** | **Cleared** | Landed with Phases 11–12 / 13 commits |
| Omit Phase 11 MatchSync providers / tests | **Critical** | **Cleared** | Landed in `ac0608e` |
| Omit Phase 12 simulator scripts | **High** | **Cleared** | Landed in `ac0608e` |
| Dual docs drift (VitePress `docs/` vs git `WorldCup-System/docs/`) | **High** | **Cleared for packaging pass** | Keep dual-docs policy; Phase 14 continues the mirror |
| Accidental WC 2026 wipe via importer in ops notes | **High** | **Documented** | Prefer `add_sf2_and_final.py`; importer wipe path warned — [phase-9-ops-closeout](phase-9-ops-closeout.md) |
| Unfixed Phase 11 / 12 Bugbot highs | **High** | **Cleared** | [phase-11-tests-gate](phase-11-tests-gate.md) · [phase-12-tests-gate](phase-12-tests-gate.md) |
| Suite / smoke regressions before push | **High** | **Cleared at last gate** | Re-run only if code changes; Phase 13 suite **438** |
| Phase 13 not on origin | **High** | **Cleared** | Pushed `5fdd1c4` to `origin/master` (27 Jul 2026) — [phase-14-planned](phase-14-planned.md) Task 1 |
| Phase 13 implementation incomplete | Info | **Cleared** | Phase 13 Done |

**Gate verdict (docs):** packaging **complete** on origin. Next: [Phase 14 Deploy Readiness](phase-14-planned.md) Task 7 (gate) — Task 6 migrate/smoke Done ([phase-14-migrate-smoke](phase-14-migrate-smoke.md)).

### Working-tree inventory (historical — `git status` 21 Jul 2026)

**Superseded.** Snapshot below was taken **before** packaging commits. As of **27 Jul 2026**, `origin/master` includes `ac0608e` + Phase 13 `5fdd1c4`. Do not treat “ahead by 19” or “Phase 13 plan only” as current.

Git repo: `WorldCup-System/` · branch `master` **was** ahead of `origin/master` by 19 · **~59 modified** · **~44 untracked** (21 Jul snapshot).

| Bucket | Roadmap | What landed in upload |
| --- | --- | --- |
| Phase 9 ops | [phase-9](../roadmap.md#phase-9) | `scripts/add_sf2_and_final.py`, `import_wc2026_finished_matches.py`, [phase-9-ops-closeout](phase-9-ops-closeout.md) |
| Phase 10 leftovers | [phase-10](../roadmap.md#phase-10) | Docs polish (`phase-10-cleanup`, planned, match-sync notes); SPA already mostly committed Tasks 1–3 |
| Phase 11 timeline / scorers | [phase-11](../roadmap.md#phase-11) | MatchSync timeline + resolve + apply + SyncScorers + honesty; 3 migrations; Admin + client; suite growth → **382** |
| Phase 12 WC 2030 | [phase-12](../roadmap.md#phase-12) | `simulate_wc2030.py` + tests; multi-cup `Team.CountryId`; TeamService `EnsureSameWorldCup`; SPA WC picker persistence |
| Phase 13 (was plan-only on 21 Jul) | [phase-13](../roadmap.md#phase-13) | Later: full company model/API/SPA — **Done** on origin as `5fdd1c4` |
| Extra scripts (ops) | — | Optional: `seed_wc2026_players.py`, `sync_scorers_backfill.ps1`, `reassign_placeholder_goals.py`, `simulate_betting_demo.py`, `scripts/data/wc2026_players.csv` |

### Roadmap mapping (upload scope — closed)

| Phase | Status | Detail pages |
| --- | --- | --- |
| 9 | **Done** | [phase-9-ops-closeout](phase-9-ops-closeout.md) |
| 10 | **Done** (Tasks 1–4) | [cleanup](phase-10-cleanup.md) · [match-sync](phase-10-match-sync.md) · [fixtures](phase-10-fixtures-ux.md) · [styling](phase-10-styling.md) · [planned](phase-10-planned.md) |
| 11 | **Done** (Tasks 1–8) | [tests-gate](phase-11-tests-gate.md) · [planned](phase-11-planned.md) · Tasks 1–7 detail pages |
| 12 | **Done** (Tasks 1–4) | [tests-gate](phase-12-tests-gate.md) · [planned](phase-12-planned.md) · [draw](phase-12-draw.md) · [group-sim](phase-12-group-sim.md) · [bracket](phase-12-bracket.md) |
| 13 | **Done** | [phase-13-tests-gate](phase-13-tests-gate.md) · [phase-13-planned](phase-13-planned.md) |

### Must-include file checklist (critical paths)

#### Phase 11 — MatchSync + migrations

- [ ] `Core/Services/MatchSync/FifaTimelineEventsProvider.cs` (+ interfaces/DTOs)
- [ ] `Core/Services/MatchSync/TimelinePlayerResolver.cs` (+ result/method types)
- [ ] `Core/Services/MatchSync/TimelineScorerApplyService.cs` (+ result)
- [ ] `Core/Services/MatchSync/MatchResultSyncService.cs` (SyncResult wire + SyncScorers)
- [ ] `Data/Migrations/20260720120000_AddMatchExternalStageId.cs` (+ Designer)
- [ ] `Data/Migrations/20260720140000_AddPlayerExternalPlayerId.cs` (+ Designer)
- [ ] `WorldCup-System.Tests/Services/MatchSync/*Timeline*` + expanded `MatchResultSyncServiceTests`
- [ ] `worldcup-client/.../placeholder-scorer.ts` + sync API models

#### Phase 12 — multi-cup + simulator

- [ ] `Data/Migrations/20260720160000_AllowMultipleTeamsPerCountry.cs`
- [ ] `Core/Services/Teams/TeamService.cs` (per-WC unique + `EnsureSameWorldCup`)
- [ ] `scripts/simulate_wc2030.py` + `scripts/test_simulate_wc2030_draw.py`
- [ ] `worldcup-client/.../worldcup-context.service.ts` (localStorage WC selection)

#### Docs (both trees)

- [ ] `docs/changes-review.md` + this page
- [ ] All `docs/changes/phase-11-*.md` / `phase-12-*.md` / `phase-10-cleanup.md` / `phase-13-planned.md`
- [ ] Mirror under workspace `docs/` for VitePress

### Accepted medium / non-blocking (do not block upload)

| Item | Notes |
| --- | --- |
| Phase 12 H2H vs Python sort | Accepted — Pts→GD→GF→name in simulator |
| FIFA public API ToS / rate limit | Config + `BatchDelayMilliseconds`; monitor |
| Auto-create named Forwards on resolve | Accepted v1 |
| Google Fonts CDN | Fallbacks present |
| Helper scripts CSV / betting demo | Include if useful for ops; not required for phase Done |

### Suggested commit packaging (when user asks to commit)

Historical — packaging commits already landed. For new Phase 14 work, commit only when the user asks; keep secrets out of git.

Do **not** force-push.

### Related

- [Phase 14 planned — Deploy Readiness](phase-14-planned.md)
- [Changes Review](../changes-review.md)
- [Phase 11 tests gate](phase-11-tests-gate.md)
- [Phase 12 tests gate](phase-12-tests-gate.md)
- [Phase 10 cleanup](phase-10-cleanup.md)
- [Phase 13 planned](phase-13-planned.md)
- [Roadmap](../roadmap.md)
