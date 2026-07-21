# WorldCup System — Phase 12 Task 4: Smoke + Review Gate

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 12 plan](phase-12-planned.md) · [Task 3 bracket](phase-12-bracket.md)

## Phase 12 Task 4 — Smoke + review gate (`p12-tests-gate`)

Opened **21 Jul 2026**. Checklist: [Roadmap Phase 12](../roadmap.md#phase-12).

> **Success:** **Task 4 gate closed (21 Jul 2026).** Formal close of Phase 12. Unittest `python test_simulate_wc2030_draw.py` → **23** OK. Smoke `python simulate_wc2030.py --seed 2030 --wipe-matches` → WorldCup 2030; Group 72 / goals 193; Knockout 31; total **103**; WC 2026 untouched. Bugbot highs fixed (cross-cup team move blocked; demo kickoffs so group = Finished / KO = Scheduled). Medium H2H tiebreak divergence accepted (v1 Pts→GD→GF→name). TeamService tests **14** passed. Phase 12 **Done**.

### Review gate

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Python unittest suite (draw + group + bracket + anchors) | High (gate) | **Cleared** | `python test_simulate_wc2030_draw.py` → **23** OK |
| Smoke seed 2030 full pipeline | High (gate) | **Cleared** | `--wipe-matches` → 72 group + 31 KO = **103**; goals=193 |
| Wipe scoped to WC 2030 only | High (acceptance) | **Cleared** | Smoke: WC 2026 untouched by design |
| Bracket shape (R32 filled; later TBD feeders) | High (acceptance) | **Cleared** | Tasks 1–3; smoke 31 KO |
| Cross-cup `AssignTeamToGroup` / `UpdateTeam` | High | **Cleared** | `EnsureSameWorldCup`; TeamService **14** |
| Future 2030 kickoffs break SPA standings | High | **Cleared** | `demo_kickoff_anchors()` — group past / KO future; cup Year stays 2030 |
| Bracket vs SPA H2H tiebreak | Medium | **Accepted debt** | v1 Pts→GD→GF→name (documented Task 3) |
| Markdown docs (plan + Tasks 1–4) | High (gate) | **Cleared** | This page + planned/roadmap/index/changes-review |
| Bugbot Phase 12 formal | High (gate) | **Cleared** | Highs fixed; medium accepted |

### Locked decisions

| Choice | Decision |
| --- | --- |
| Gate scope | Smoke + unittest + docs + Bugbot highs fixed |
| Unittest | `test_simulate_wc2030_draw.py` — **23** (6 draw + 8 group-sim + 8 bracket + 1 anchors) |
| Smoke seed | `--seed 2030 --wipe-matches` |
| Match.Date | Anchored vs UtcNow (`demo_kickoff_anchors`); `WorldCup.Year` = 2030-06-13 |
| Hybrid KO | Group Finished; R32 Scheduled/CanBet; R16→Final TBD |
| Multi-cup | Teams cannot move across World Cups via Admin assign/update |
| H2H | Not in Python v1 (accepted vs `StandingsService`) |

### Deliverables

| Item | Detail | Status |
| --- | --- | --- |
| Unittest green | **23** OK | Done |
| Smoke green | **103** matches | Done |
| Bugbot highs | Cross-cup + kickoff anchors | Done |
| TeamService tests | **14** passed (filter) | Done |
| Docs gate | planned + roadmap + changes-review + index + vitepress | Done |

### Acceptance

- `python test_simulate_wc2030_draw.py` → Ran **23** tests, OK.
- Smoke: `python simulate_wc2030.py --seed 2030 --wipe-matches` → **72** group + **31** KO = **103**; demo kickoffs logged; WC 2026 unchanged.
- SPA: World Cup **2030** — group standings populated (Finished); `/bracket` R32 filled / CanBet; later rounds TBD.
- Cross-cup team move throws; TeamService filter tests green.

### Runbook

```text
cd WorldCup-System/scripts

python test_simulate_wc2030_draw.py
# expect: Ran 23 tests ... OK

python simulate_wc2030.py --seed 2030 --wipe-matches
# expect: Demo kickoffs (group past / KO future); 72 + 31 = 103
```

### Bugbot findings (Task 4)

| Severity | Location | Finding | Resolution |
| --- | --- | --- | --- |
| High | `TeamService.cs` | Assign/Update could move a team into another World Cup’s group | `EnsureSameWorldCup` on assign + update |
| High | `simulate_wc2030.py` | Calendar 2030 kickoffs → standings skip / Scheduled+goals | `demo_kickoff_anchors()` |
| Medium | `simulate_wc2030.py` sort_key | No H2H vs `StandingsService` | Accepted — Task 3 locked Pts→GD→GF→name |

### Git file list (Task 4 deltas)

| Status | Path |
| --- | --- |
| Modified | `scripts/simulate_wc2030.py` — `demo_kickoff_anchors` |
| Modified | `scripts/test_simulate_wc2030_draw.py` — anchors test (**23**) |
| Modified | `Core/Services/Teams/TeamService.cs` — `EnsureSameWorldCup` |
| Modified | `WorldCup-System.Tests/Services/Teams/TeamServiceTests.cs` — cross-cup cases |
| Docs | `docs/changes/phase-12-tests-gate.md` (+ planned, roadmap, changes-review, index) |

### Related

- [Phase 12 plan](phase-12-planned.md)
- [Task 1 — Draw](phase-12-draw.md)
- [Task 2 — Group sim](phase-12-group-sim.md)
- [Task 3 — Qualify + bracket](phase-12-bracket.md)
- [Roadmap Phase 12](../roadmap.md#phase-12)
- [WC 2026 scripts](wc2026-live-data.md)
