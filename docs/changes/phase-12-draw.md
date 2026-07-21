# WorldCup System — Phase 12 Task 1: Cup Ensure + Hosts/Random Draw

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 12 plan](phase-12-planned.md)

## Phase 12 Task 1 — Draw (`p12-draw`)

Opened **20 Jul 2026**. Checklist: [Roadmap Phase 12](../roadmap.md#phase-12).

> **Success:** **Task 1 Done (closed via Task 4, 21 Jul 2026).** `simulate_wc2030.py` ensures World Cup 2030, locks 6 hosts + 42 random countries, draws groups A–L (hosts in distinct groups). Multi-cup unlock: `Team.CountryId` no longer globally unique — one Team per country **per World Cup**. WC 2026 groups untouched. Formal gate: [phase-12-tests-gate](phase-12-tests-gate.md) (unittest **23** / smoke **103**). Group fixtures: [Task 2](phase-12-group-sim.md).

### Review gate

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Parallel 2030 vs unique `Team.CountryId` | High | **Cleared** | Migration `AllowMultipleTeamsPerCountry` + Program.cs index repair |
| Steal 2026 Team.GroupId | High | **Cleared** | New Team rows for 2030; 2026 Spain etc. remain |
| Draw size / host separation | High (acceptance) | **Cleared** | `test_simulate_wc2030_draw.py` — draw cases in suite **23** |
| TeamService same-cup duplicate | High | **Cleared** | Uniqueness scoped via `Group.WorldCupId` |
| Bugbot + TeamService / smoke | High (gate) | **Cleared** | Closed in [Task 4](phase-12-tests-gate.md) (`EnsureSameWorldCup` + smoke) |
| Group fixtures / scores | Info | **Done** | [Task 2](phase-12-group-sim.md) |

### Locked decisions

| Choice | Decision |
| --- | --- |
| Hosts | Spain, Portugal, Morocco, Argentina, Uruguay, Paraguay |
| Others | 42 random from `Countries` excluding hosts (`--seed`) |
| Multi-cup | Drop global unique on `Team.CountryId`; enforce one team per country **per World Cup** in `TeamService` |
| Task scope | Cup + groups + team draw only — no matches |

### Deliverables

| Item | Detail | Status |
| --- | --- | --- |
| Script | `scripts/simulate_wc2030.py` — `--seed`, `--dry-run`, `--reset-draw` | Done |
| Helpers | `build_team_pool`, `draw_groups` (seedable) | Done |
| Tests | `scripts/test_simulate_wc2030_draw.py` (unittest, 6 cases) | Done |
| Migration | `20260720160000_AllowMultipleTeamsPerCountry` | Done |
| Startup repair | `DROP` unique + `CREATE` non-unique `IX_Team_CountryId` in `Program.cs` | Done |
| Entity / EF | `Country.Teams` (WithMany); index non-unique | Done |
| TeamService | Per–World Cup country uniqueness | Done |
| TeamService tests | Same-cup reject; other-cup allow | Done |

### Acceptance

- WorldCups row with year **2030**; groups **A–L** for that id only.
- 48 Team rows in those groups (6 hosts + 42 others); each host in a **different** group.
- Existing WC 2026 Team rows / group membership unchanged (verified: Spain in WC1 Group H and WC2030 Group A as separate Team ids).
- `python test_simulate_wc2030_draw.py` → 6 OK.

### Runbook

```text
cd WorldCup-System/scripts
python test_simulate_wc2030_draw.py
python simulate_wc2030.py --seed 2030
python simulate_wc2030.py --seed 2030 --dry-run
python simulate_wc2030.py --seed 2030 --reset-draw   # only if 2030 teams have no deps
```

Requires Postgres `WC_DB` (same default as WC 2026 scripts). Apply migration or restart API so `IX_Team_CountryId` is non-unique before first multi-cup insert.

### Git file list (Task 1)

| Status | Path |
| --- | --- |
| **New** | `scripts/simulate_wc2030.py` |
| **New** | `scripts/test_simulate_wc2030_draw.py` |
| **New** | `Data/Migrations/20260720160000_AllowMultipleTeamsPerCountry.cs` |
| Modified | `Data/Entities/Country.cs` — `Teams` collection |
| Modified | `Data/Context/ApplicationDbContext.cs` — WithMany; non-unique CountryId |
| Modified | `Data/Migrations/ApplicationDbContextModelSnapshot.cs` |
| Modified | `Core/Services/Teams/TeamService.cs` — per–World Cup uniqueness |
| Modified | `WorldCup-System.Tests/Services/Teams/TeamServiceTests.cs` |
| Modified | `WorldCup-System/Program.cs` — index repair |
| Docs | `docs/changes/phase-12-draw.md`, planned, roadmap, changes-review, index, data-model |

### Related

- [Phase 12 plan](phase-12-planned.md)
- [Task 2 — Group sim](phase-12-group-sim.md)
- [Roadmap Phase 12](../roadmap.md#phase-12)
- [WC 2026 scripts](wc2026-live-data.md)
