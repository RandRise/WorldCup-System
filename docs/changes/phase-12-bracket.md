# WorldCup System — Phase 12 Task 3: Qualify + Bracket

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 12 plan](phase-12-planned.md) · [Task 2 group sim](phase-12-group-sim.md)

## Phase 12 Task 3 — Qualify + bracket (`p12-bracket`)

Opened **20 Jul 2026**. Checklist: [Roadmap Phase 12](../roadmap.md#phase-12).

> **Success:** **Task 3 implemented (20 Jul 2026).** After group sim, `simulate_wc2030.py` ranks standings (Pts → GD → GF → name), advances top 2 per group + 8 best thirds, builds **16 R32 + 8 R16 + 4 QF + 2 SF + Final** (31 matches, no ThirdPlace). R32 teams filled; later rounds TBD with `FeederMatchOneId` / `FeederMatchTwoId`. FIFA Art. 12.6 skeleton + deterministic third-slot matching (allowed pools; Annexe C guarantee covered by unittest). Wipe walks feeder descendants so TBD rows clear. Unittest **22** (6 draw + 8 group-sim + 8 bracket). Smoke seed 2030 → **103** matches (72+31); WC 2026 untouched. No C# / migration. Formal phase gate closed in [Task 4](phase-12-tests-gate.md).

### Review gate

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| 32 qualifiers (24 + 8 best thirds) | High (acceptance) | **Cleared** | `qualify_knockout_teams` + tests |
| Standings order Pts→GD→GF→name | High (acceptance) | **Cleared** | `test_rank_standings_pts_gd_gf_name` |
| R32 16 unique teams; feeder tree 31 | High (acceptance) | **Cleared** | pairings + feeder shape tests; smoke 103 |
| Third-slot assignment always possible | High | **Cleared** | backtracking over FIFA pools; all C(12,8)=495 OK |
| Wipe includes TBD feeder matches | High | **Cleared** | `load_world_cup_match_ids` walks feeder descendants |
| Knockout scores / ThirdPlace | Info | By design | Hybrid: Admin scores KO; no ThirdPlace |
| Python unittest suite | High (gate) | **Cleared** | `python test_simulate_wc2030_draw.py` → **22** OK |
| Bugbot Task 3 | High (gate) | **Cleared** | Formal close in [Task 4](phase-12-tests-gate.md) |

### Locked decisions

| Choice | Decision |
| --- | --- |
| Tiebreakers | Pts → GD → GF → country name (no H2H / fair play in v1) |
| Qualifiers | Top 2 × 12 + 8 best thirds = **32** |
| R32 skeleton | FIFA WC26 Art. 12.6 fixed places (M73–M88) |
| Third placement | Deterministic backtracking on Art. 12.6 allowed pools (not a pasted Annexe C table) |
| Later rounds | FIFA Art. 12.7–12.11 feeders; **skip ThirdPlace** |
| Scores | R32 teams set; R16→Final `TeamOneId`/`TeamTwoId` null until Admin advance |
| Kickoffs | From **2030-07-07** 15:00 UTC; 4 slots/day |
| CLI | `--skip-knockout`; bracket-only via `--skip-group-sim` when 72 group matches already exist |
| Scope | Python only — **no** C# `GenerateBracket` extension |

### Deliverables

| Item | Detail | Status |
| --- | --- | --- |
| Standings helpers | `TeamStanding`, `rank_standings`, `rank_group_standings`, `select_best_thirds` | Done |
| Qualification | `qualify_knockout_teams`, `assign_third_groups_to_slots`, `build_r32_team_pairings` | Done |
| DB bracket | `load_group_standings`, `build_knockout_bracket`, feeder insert | Done |
| Wipe / counts | `load_world_cup_match_ids` (feeder walk); stage counts | Done |
| CLI | `--skip-knockout`; bracket-only path | Done |
| Tests | +8 bracket cases → **22** total | Done |
| C# / migrations | None for Task 3 | N/A |

### Behavior

#### Qualification

1. Load 72 group matches → Pts from `TeamStats`, GF/GA from goals (own goals credit opponent).
2. Rank each group; take 1st + 2nd.
3. Rank all 12 thirds; take top 8.

#### Bracket insert

1. Resolve R32 pairings from FIFA skeleton + third-slot assignment.
2. Insert 16 `MatchStage.RoundOf32` with both teams set.
3. Insert 8 R16 + 4 QF + 2 SF + Final with null teams and feeder FKs (winners advance via existing `KnockoutService`).

#### Idempotency

| Flag | Effect |
| --- | --- |
| `--wipe-matches` | Wipe all 2030 matches including feeder TBD rows |
| (default, matches exist) | Exit unless `--skip-group-sim` for bracket-only |
| `--skip-group-sim` + 72 group / 0 KO | Build bracket only (no redraw) |
| `--skip-knockout` | Draw/group only (Task 1–2 behavior) |
| `--seed N` | Reproducible draw + group scores (bracket deterministic from standings) |

### Acceptance

- `python test_simulate_wc2030_draw.py` → **22** OK.
- Smoke: `python simulate_wc2030.py --seed 2030 --wipe-matches` → **72** group + **31** knockout = **103**; R32 = 16; WC 2026 unchanged.
- SPA: select World Cup **2030** — `/bracket` shows R32 filled; later rounds TBD for Admin.

### Runbook

```text
cd WorldCup-System/scripts
python test_simulate_wc2030_draw.py
python simulate_wc2030.py --seed 2030 --wipe-matches
python simulate_wc2030.py --seed 2030 --skip-knockout          # group only
python simulate_wc2030.py --seed 2030 --skip-group-sim         # bracket only if 72 group exist
```

### Git file list (Task 3)

| Status | Path |
| --- | --- |
| **Modified** | `scripts/simulate_wc2030.py` — standings, best thirds, R32→Final, wipe feeder walk, CLI |
| **Modified** | `scripts/test_simulate_wc2030_draw.py` — +8 bracket cases (22 total) |
| Docs | `docs/changes/phase-12-bracket.md`, planned, roadmap, changes-review, index, vitepress |

### Related

- [Phase 12 plan](phase-12-planned.md)
- [Task 1 — Draw](phase-12-draw.md)
- [Task 2 — Group sim](phase-12-group-sim.md)
- [Roadmap Phase 12](../roadmap.md#phase-12)
- [WC 2026 scripts](wc2026-live-data.md)
