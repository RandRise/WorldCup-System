# WorldCup System — Phase 12 Task 2: Group Simulation

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 12 plan](phase-12-planned.md) · [Task 1 draw](phase-12-draw.md)

## Phase 12 Task 2 — Group simulation (`p12-group-sim`)

Opened **20 Jul 2026**. Checklist: [Roadmap Phase 12](../roadmap.md#phase-12).

> **Success:** **Task 2 implemented (20 Jul 2026).** After the draw, `simulate_wc2030.py` schedules **72** group matches (12×6), samples Poisson λ≈1.35 scores (cap 7), inserts `Goal` + `TeamStats` (possession/shots/SOT/points), and ensures Tournament Scorer placeholders for empty 2030 squads. `--wipe-matches` is scoped to WC 2030 only. Unittest **14** (6 draw + 8 group-sim). Smoke: seed 2030 → 72 matches, 193 goals; WC 2026 untouched. No C# / migration changes in Task 2. Bugbot highs fixed (2026 importer multi-cup scope + Forward position by name).

### Review gate

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Wipe scoped to 2030 teams only | High (acceptance) | **Cleared** | `wipe_world_cup_matches` filters by WC team ids; smoke: 2026 untouched |
| Redraw/resim blocked without wipe | High | **Cleared** | Exit if 2030 matches exist unless `--wipe-matches` |
| Schedule shape 72 / 3 games each | High (acceptance) | **Cleared** | `GroupSimHelperTests` — 72 fixtures, 48 teams × 3 |
| Poisson bounds (cap 7) | High (acceptance) | **Cleared** | `test_sample_poisson_and_score_bounds` |
| 2026 importer ambiguous Team by country | High | **Fixed** | `import_wc2026_finished_matches.py` filters teams by WC 2026 `WorldCupId` |
| 2026 importer global DELETE Match | High | **Fixed** | Scoped wipe via same WC-team match id pattern (2030 fixtures preserved) |
| Hardcoded Forward position id=4 | Medium | **Fixed** | `resolve_forward_position_id` by `PlayerPositions.Name` |
| Non-overlapping matchdays | Medium | **Cleared** | `test_kickoff_for_matchday_slot_ordering` |
| Python unittest suite | High (gate) | **Cleared** | `python test_simulate_wc2030_draw.py` → **14** OK |
| Bugbot Task 2 | High (gate) | **Cleared** | Highs/medium above fixed; formal phase close still Task 4 |
| Knockout / best thirds | Info | **Done** | [Task 3](phase-12-bracket.md) |

### Locked decisions

| Choice | Decision |
| --- | --- |
| Fixtures | 12 groups × 6 round-robin = **72** `MatchStage.Group` (0) |
| Scores | Independent Poisson λ=`1.35` per side; hard cap **7** |
| Stats | Dummy `TeamStats`: possession (sums to 100), shots, shots on target ≥ goals, points 3/1/0 |
| Scorers | Prefer Forwards → any non-placeholder squad → Tournament Scorer (#99) for empty squads |
| Venues | Cycle existing `Stadium` rows (NA 2026 venues when ES/PT/MA absent) |
| Kickoffs | From **2030-06-13** 15:00 UTC; 3 matchdays, non-overlapping (8 days × 3 kickoffs/day per MD) |
| Wipe | `--wipe-matches` deletes bets/goals/cards/stats/matches for **this** WorldCup’s teams only |
| Scope | Python only — **no** C# or EF migrations in Task 2 |

### Deliverables

| Item | Detail | Status |
| --- | --- | --- |
| Schedule helpers | `group_matchday_pairings`, `kickoff_for_matchday_slot`, `build_group_stage_schedule` | Done |
| Score / stats helpers | `sample_poisson`, `sample_match_score`, `match_result_points`, `build_dummy_stats` | Done |
| DB sim | `simulate_group_stage`, `wipe_world_cup_matches`, `ensure_placeholder_players`, scorers | Done |
| CLI | `--wipe-matches`, `--skip-group-sim` (+ existing `--seed` / `--dry-run` / `--reset-draw`) | Done |
| Guard | Block redraw/resim when 2030 matches exist unless wipe | Done |
| Tests | `test_simulate_wc2030_draw.py` — 14 unittest cases | Done |
| C# / migrations | None for Task 2 | N/A |

### Behavior

#### Schedule

- Round-robin pairings for 4 teams across 3 matchdays (2 games per matchday per group).
- Per matchday, all 12 groups play in order (24 fixtures); slot index drives kickoff staggering.
- Home/away randomly flipped (~50%) with the shared `--seed` RNG.
- Stadium index cycles `0..N-1` over all stadiums in DB.

#### Simulation insert (per fixture)

1. Insert `Match` (`Stage=0`, no feeders).
2. Insert two `TeamStats` rows (home/away).
3. Insert `Goal` rows timed from kickoff (home ~10'+12k, away ~15'+12k); `IsOwnGoal=0`.
4. Scorer pick: cycle Forwards / squad / Tournament Scorer for that team.

#### Idempotency

| Flag | Effect |
| --- | --- |
| `--wipe-matches` | Wipe prior 2030 matches (+ goals/stats/bets/cards; clear feeder FKs pointing at them) |
| (default) | If any 2030 match exists → `SystemExit` (no redraw/resim) |
| `--skip-group-sim` | Draw/ensure teams only; skip schedule + scores |
| `--dry-run` | Print plan; roll back DB writes |
| `--seed N` | Reproducible draw **and** scores |
| `--reset-draw` | Rebuild 2030 team assignments (requires no match deps — wipe first if fixtures exist) |

### Acceptance

- `python test_simulate_wc2030_draw.py` → **14** OK (6 draw + 8 group-sim).
- Smoke: `python simulate_wc2030.py --seed 2030 --wipe-matches` → **72** matches, **193** goals (seed-dependent goal count); WC 2026 match/goal counts unchanged.
- Groups A–L still 4 teams each; standings derivable from `TeamStats.Points` + goals.

### Runbook

```text
cd WorldCup-System/scripts
python test_simulate_wc2030_draw.py
python simulate_wc2030.py --seed 2030 --wipe-matches
python simulate_wc2030.py --seed 2030 --dry-run
python simulate_wc2030.py --seed 2030 --skip-group-sim   # draw only
```

Requires Postgres `WC_DB` (same default as WC 2026 scripts) and at least one `Stadium` row. Task 1 multi-cup index repair must already be applied.

SPA: select World Cup **2030** — group fixtures show finished scores; knockout via [Task 3](phase-12-bracket.md).

### Git file list (Task 2)

| Status | Path |
| --- | --- |
| **Modified** | `scripts/simulate_wc2030.py` — group schedule + sim + wipe + CLI; Forward by name |
| **Modified** | `scripts/test_simulate_wc2030_draw.py` — +8 group-sim cases (14 total) |
| **Modified** | `scripts/import_wc2026_finished_matches.py` — multi-cup: WC 2026 team filter + scoped wipe |
| **Modified** | `scripts/add_sf2_and_final.py` — resolve England/Argentina/Spain via WC 2026 |
| Docs | `docs/changes/phase-12-group-sim.md`, planned, roadmap, changes-review, index |

Untracked/modified in the wider working tree may include Phase 11 MatchSync files — **not** part of Task 2.

### Related

- [Phase 12 plan](phase-12-planned.md)
- [Task 1 — Draw](phase-12-draw.md)
- [Roadmap Phase 12](../roadmap.md#phase-12)
- [WC 2026 scripts](wc2026-live-data.md)
