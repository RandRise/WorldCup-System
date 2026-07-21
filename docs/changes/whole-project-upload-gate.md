# WorldCup System — Whole-Project Upload Gate

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 11 gate](phase-11-tests-gate.md) · [Phase 12 gate](phase-12-tests-gate.md)

## Whole-project upload readiness (Phases 9–13 docs + 10 leftovers + 11–12 code)

Opened **21 Jul 2026**. Purpose: one place to judge whether the **uncommitted working tree** is safe to commit / push after Phases 9–12 closed their individual review gates.

> **Success (docs gate):** Phase detail pages for **10–12** are present and map to [roadmap](../roadmap.md). Product gates already closed: Phase 11 suite **382**; Phase 12 unittest **23** / smoke **103**. Remaining work for upload is **git packaging** (include migrations + dual docs), not reopening phase logic.

### Review gate

High/critical items that must be clear **before** treating the upload as complete (commit/push). These are **upload packaging** risks — phase Bugbot highs were already cleared in Tasks 8 / 4.

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Omit EF migrations from commit | **Critical** | **Open until commit** | Include all three: `AddMatchExternalStageId` (+ Designer), `AddPlayerExternalPlayerId` (+ Designer), `AllowMultipleTeamsPerCountry` + model snapshot |
| Omit Phase 11 MatchSync providers / tests | **Critical** | **Open until commit** | Include untracked `FifaTimeline*`, `TimelinePlayer*`, `TimelineScorer*`, and their test files |
| Omit Phase 12 simulator scripts | **High** | **Open until commit** | Include `simulate_wc2030.py`, `test_simulate_wc2030_draw.py` |
| Dual docs drift (VitePress `docs/` vs git `WorldCup-System/docs/`) | **High** | **Cleared for this pass** | Mirror Markdown both trees; keep [phase-10-cleanup](phase-10-cleanup.md) dual-docs policy |
| Accidental WC 2026 wipe via importer in ops notes | **High** | **Documented** | Prefer `add_sf2_and_final.py`; importer wipe path warned — [phase-9-ops-closeout](phase-9-ops-closeout.md) |
| Unfixed Phase 11 / 12 Bugbot highs | **High** | **Cleared** | [phase-11-tests-gate](phase-11-tests-gate.md) · [phase-12-tests-gate](phase-12-tests-gate.md) |
| Suite / smoke regressions before push | **High** | **Cleared at last gate** | API **382**; Python **23** / smoke **103** — re-run if code changes after this doc |
| Phase 13 implementation incomplete | Info | **N/A** | Docs-only planned backlog — safe to ship plan file without code |

**Gate verdict (docs):** ready to **package** Phases 9–12 + Phase 10 leftovers + Phase 13 plan into git. Do **not** start Phase 13 implementation until the upload commit lands (optional process preference).

### Working-tree inventory (`git status` 21 Jul 2026)

Git repo: `WorldCup-System/` · branch `master` **ahead of `origin/master` by 19** · **~59 modified** · **~44 untracked**.

| Bucket | Roadmap | What lands in upload |
| --- | --- | --- |
| Phase 9 ops | [phase-9](../roadmap.md#phase-9) | `scripts/add_sf2_and_final.py`, `import_wc2026_finished_matches.py`, [phase-9-ops-closeout](phase-9-ops-closeout.md) |
| Phase 10 leftovers | [phase-10](../roadmap.md#phase-10) | Docs polish (`phase-10-cleanup`, planned, match-sync notes); SPA already mostly committed Tasks 1–3 |
| Phase 11 timeline / scorers | [phase-11](../roadmap.md#phase-11) | MatchSync timeline + resolve + apply + SyncScorers + honesty; 3 migrations; Admin + client; suite growth → **382** |
| Phase 12 WC 2030 | [phase-12](../roadmap.md#phase-12) | `simulate_wc2030.py` + tests; multi-cup `Team.CountryId`; TeamService `EnsureSameWorldCup`; SPA WC picker persistence |
| Phase 13 plan only | [phase-13](../roadmap.md#phase-13) | [phase-13-planned](phase-13-planned.md) — **no** company code yet |
| Extra scripts (ops) | — | Optional: `seed_wc2026_players.py`, `sync_scorers_backfill.ps1`, `reassign_placeholder_goals.py`, `simulate_betting_demo.py`, `scripts/data/wc2026_players.csv` |

### Roadmap mapping (upload scope)

| Phase | Status | Detail pages |
| --- | --- | --- |
| 9 | **Done** | [phase-9-ops-closeout](phase-9-ops-closeout.md) |
| 10 | **Done** (Tasks 1–4) | [cleanup](phase-10-cleanup.md) · [match-sync](phase-10-match-sync.md) · [fixtures](phase-10-fixtures-ux.md) · [styling](phase-10-styling.md) · [planned](phase-10-planned.md) |
| 11 | **Done** (Tasks 1–8) | [tests-gate](phase-11-tests-gate.md) · [planned](phase-11-planned.md) · Tasks 1–7 detail pages |
| 12 | **Done** (Tasks 1–4) | [tests-gate](phase-12-tests-gate.md) · [planned](phase-12-planned.md) · [draw](phase-12-draw.md) · [group-sim](phase-12-group-sim.md) · [bracket](phase-12-bracket.md) |
| 13 | **Planned** | [phase-13-planned](phase-13-planned.md) |

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

1. Migrations + entities + snapshot first (schema).
2. Phase 11 MatchSync + tests + client honesty/sync.
3. Phase 12 scripts + TeamService + SPA WC context.
4. Docs (changes-review + phase pages + this gate).
5. Optional ops scripts / CSV in a follow-up if noise is a concern.

Do **not** force-push; branch is already **19** commits ahead of origin.

### Related

- [Changes Review](../changes-review.md)
- [Phase 11 tests gate](phase-11-tests-gate.md)
- [Phase 12 tests gate](phase-12-tests-gate.md)
- [Phase 10 cleanup](phase-10-cleanup.md)
- [Phase 13 planned](phase-13-planned.md)
- [Roadmap](../roadmap.md)
