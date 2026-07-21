# WorldCup System — Phase 12 Planned Work

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md)

## Phase 12 — 2030 World Cup Simulation

**Status: Done** — Tasks 1–4 complete **21 Jul 2026** (draw + group sim + bracket + smoke/review gate closed). Checklist: [Roadmap Phase 12](../roadmap.md#phase-12) · [Task 1](phase-12-draw.md) · [Task 2](phase-12-group-sim.md) · [Task 3](phase-12-bracket.md) · [Task 4 gate](phase-12-tests-gate.md).

Offline Python-driven simulation of a FIFA World Cup 2030 tournament for demos and betting practice alongside WC 2026.

### Locked decisions

| Choice | Decision |
| --- | --- |
| Format | 48 teams, 12 groups A–L (same as 2026 / expected 2030) |
| Teams | **Hosts locked** — Spain, Portugal, Morocco, Argentina, Uruguay, Paraguay — plus **42 random** from existing `Countries` |
| Depth | **Hybrid** — fully simulate group stage; generate knockout tree from standings; knockout fixtures scheduled **without scores** (Admin / optional later pass) |
| Delivery | Offline **Python** under `WorldCup-System/scripts/` (same DB path as WC 2026). No C# `GenerateBracket` extension in v1 (that API is still 8-group / R16-only) |
| Multi-cup | **One Team per country per World Cup** — global unique on `Team.CountryId` dropped (Task 1); WC 2026 rows stay put |

### Architecture

```text
ensure WorldCup 2030
  → lock 6 hosts + random 42
    → random draw into groups A–L
    → schedule 72 group matches
    → simulate group scores + goals
    → top 2 per group + 8 best thirds
    → build R32 → R16 → QF → SF → Final (feeders)
    → knockout TBD in SPA Admin
```

Reuse patterns from `scripts/import_wc2026_finished_matches.py`: `WC_DB`, country name resolution, `MatchStage` ints (`Group=0` … `RoundOf32=6`), synthetic goals via squad/Tournament Scorer, stadium aliases.

**Multi-cup:** create a dedicated `WorldCups` row (year ~2030-06-13). Never `ORDER BY Id LIMIT 1`.

### Goals (summary)

| # | Task id | Theme | Outcome | Status |
| --- | --- | --- | --- | --- |
| 1 | docs | Docs | Roadmap Phase 12 + this planned doc | **Done** |
| 2 | `p12-draw` | Draw | `simulate_wc2030.py` — ensure cup, hosts+random 42, groups A–L | **Done** — [detail](phase-12-draw.md) |
| 3 | `p12-group-sim` | Group sim | Schedule 72 group matches; simulate scores/goals/stats | **Done** — [detail](phase-12-group-sim.md) |
| 4 | `p12-bracket` | Qualify + bracket | Best thirds + R32→Final with feeder FKs | **Done** — [detail](phase-12-bracket.md) |
| 5 | `p12-tests-gate` | Smoke + tests | Runbook + review gate (unittest **23**; smoke **103**; Bugbot highs fixed) | **Done** — [detail](phase-12-tests-gate.md) |

---

### Core simulator (when implementing)

Orchestrator: `WorldCup-System/scripts/simulate_wc2030.py`

```text
python simulate_wc2030.py [--seed N] [--wipe-matches] [--skip-knockout]
```

Steps in one run:

1. **Ensure cup** — insert/find `WorldCups` with Year = 2030-06-13 (or agreed kickoff).
2. **Team pool** — resolve hosts by `Countries.Name`; sample 42 others excluding hosts; create/update `Team` rows for this cup.
3. **Draw** — shuffle into groups A–L (4 each); optionally keep each host in a different group when possible (nice-to-have).
4. **Group fixtures** — 6 matches × 12 groups = **72** (`MatchStage.Group`), staggered kickoffs from ~2030-06-13; reuse Spanish/Portuguese/Moroccan stadiums where present.
5. **Simulate results** — Poisson-ish low scores (λ≈1.2–1.5); insert `Goal` + dummy `TeamStats` like the 2026 importer.
6. **Qualification** — Pts → GD → GF → name; advance **24** winners/runners-up + **8 best thirds**.
7. **Bracket** — **16 R32 + 8 R16 + 4 QF + 2 SF + Final** (skip ThirdPlace). Wire `FeederMatchOneId` / `FeederMatchTwoId`. R32 pairings from a fixed, documented template (seedable).
8. **Idempotency** — `--wipe-matches` deletes only **this** WorldCup’s matches/goals/stats/bets; redraw when `--seed` changes.

Optional modules if the file grows: `wc2030_standings.py`, `wc2030_bracket.py`.

### Supporting data

- v1: lightweight placeholder players / Tournament Scorer so goals insert cleanly.
- Optional later: `seed_wc2030_players.py` + CSV.

### Client / API

- **No new endpoints** for v1. SPA World Cup picker selects 2030 after seed.

### Tests & gate (when implementing)

- Python unit tests for draw size, best-thirds, feeder wiring (pytest).
- Phase gate: Bugbot + markdown changes doc, then tests green.
- Do not run `dotnet build` unless consented.

### Explicit non-goals (v1)

- Extending `KnockoutService.GenerateBracket` for 12-group / R32 / best thirds.
- Confederation quota draws.
- Full knockout score auto-sim (optional later `--sim-knockout`).
- FIFA calendar / external sync for 2030 (fictional tournament).
- Centenary South America venue schedule fidelity.

### Risks

- Country name mismatches vs `Countries` — reuse/extend 2026 alias map.
- Multi-cup DB — all queries filter by `WorldCupId`.
- Random draws need `--seed` for reproducible demos.

### Runbook (after implementation)

1. Ensure Postgres `TestDatabase` is up and WC 2026 data (if any) is untouched.
2. `cd WorldCup-System/scripts`
3. `python simulate_wc2030.py --seed 2030 --wipe-matches`
4. Start API + SPA; select World Cup **2030** in the client.
5. Group standings and finished group fixtures should be populated; knockout bracket open for Admin scoring.

---

### Phase 12 Roadmap Mapping

| Roadmap item | This doc |
| --- | --- |
| Docs / planned backlog | This file |
| Draw + groups | [phase-12-draw](phase-12-draw.md) (Task 1 **Done**) |
| Group simulation | [phase-12-group-sim](phase-12-group-sim.md) (Task 2 **Done**) |
| Qualify + bracket | [phase-12-bracket](phase-12-bracket.md) (Task 3 **Done**) |
| Smoke + review gate | [phase-12-tests-gate](phase-12-tests-gate.md) (Task 4 **Done**; Bugbot highs cleared) |

### Related

- [Roadmap Phase 12](../roadmap.md#phase-12)
- [Task 1 — Draw](phase-12-draw.md)
- [Task 2 — Group sim](phase-12-group-sim.md)
- [Task 3 — Qualify + bracket](phase-12-bracket.md)
- [Task 4 — Smoke + review gate](phase-12-tests-gate.md)
- [WC 2026 live data / scripts](wc2026-live-data.md)
- [Phase 11](phase-11-planned.md) — continues in parallel (MatchSync); Phase 12 Tasks 1–4 are Python-only for 2030 fixtures/bracket (Task 1 also had multi-cup C#)
