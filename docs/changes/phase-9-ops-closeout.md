# WorldCup System — Phase 9 Ops Hardening & Tournament Close-Out

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md)

## Phase 9 — Ops Hardening & Tournament Close-Out **[Done]**

Opened **17 Jul 2026** from the former “What’s left” ops list after Phases 1–8 product work. Closed **21 Jul 2026** when Final FT was recorded. [← Back to Changes Review](../changes-review.md) · [Roadmap Phase 9](../roadmap.md#phase-9)

> **Success:** **Phase 9 Done.** Final FT recorded — Argentina **0–1** Spain a.e.t. (Ferran Torres 106', MetLife); MatchId **116**; bets resolved **2/2**. Scripts updated for idempotent close-out. No C# / migration changes. Commits Phase 7–8: `dcb322b` + `7886b40`. ThirdPlace skipped.

### Review gate

High/critical risks that must be clear **before** treating close-out as verified (ops scripts — no `dotnet test` surface in this close-out).

| Item | Severity | Status | Notes |
| --- | --- | --- | --- |
| Wrong Final scoreline / unexpected goals | High | **Cleared** | Local DB: Argentina 0–1 Spain; GoalId **298** corrected Morata → Ferran Torres @ 106' |
| `import_wc2026_finished_matches.py` wipe of WC 2026 | High | **Documented** | Wipes Match/Goal/Bet for **2026 only** then reloads; prefer `add_sf2_and_final.py` for Final-only upsert |
| Import Final goal minute / scorer wrong | High | **Cleared** | Wipe path special-cases Final → Ferran Torres @ 106' (not +15' round-robin) |
| Wrong bet-resolve URL in script print | High | **Cleared** | Message uses `POST Bet/ResolveBetsForMatch/{matchId}` |
| Bets left open after FT | High | **Cleared** | MatchId **116** bets resolved **2/2** (Admin resolve / SyncResult) |
| Script writes C# / migrations | Critical | **N/A** | Close-out is Python + local DB only — no API schema change |
| Open high/critical Bugbot on scripts | High (gate) | **Cleared** | Two highs fixed 21 Jul; jersey-99 filter accepted (Tournament Scorer #99) |

**Gate verdict:** no open high/critical blockers for Phase 9 close-out. Ops caution: do not re-run the wipe importer unless a full 2026 reload is intended — Phase 15 requires `--i-understand-this-wipes-wc2026` ([phase-15-data-hygiene](phase-15-data-hygiene.md)).

### Tests

| Suite | Result |
| --- | --- |
| `scripts/test_add_sf2_and_final.py` | **9** OK |
| `dotnet test` WorldCup-System.Tests | **382** passed (no C# changes this close-out) |

### Phase 9 Roadmap Mapping

| Task | Status | Notes |
| --- | --- | --- |
| Schedule Final (Argentina vs Spain, 19 Jul) | **Done** | `add_sf2_and_final.py` — MatchId **116** (Argentina vs Spain, MetLife) |
| Record Final result after FT | **Done** | Argentina **0–1** Spain a.e.t. (Ferran Torres 106'); `apply_final_ft` + import `MATCHES` Final row |
| Optional ThirdPlace match | **Skipped** | Not needed for this tournament path |
| Apply `AddMatchStage` + Feeder* repair | **Done** | Migration recorded; Feeder* columns present; repair runs on API start |
| Commit Phase 7–8 working tree | **Done** | `dcb322b` (sync/knockout) + `7886b40` (SPA/scripts/standings) |
| Knockout re-advance UX | **Done** | Goal API warning + Schedule **Advance winner** |

### Git scope (Final FT close-out)

Git repo root: `WorldCup-System/`.

```text
M  scripts/add_sf2_and_final.py
M  scripts/import_wc2026_finished_matches.py
```

Diffstat (uncommitted close-out): `+466 / −96` across those two files. No C# / migration / SPA changes in this close-out.

### Final result (recorded)

| Field | Value |
| --- | --- |
| MatchId | **116** |
| Score | Argentina **0–1** Spain (a.e.t.) |
| Scorer | Ferran Torres **106'** |
| Venue | MetLife Stadium |
| Champions | Spain |
| Bets | Resolved **2/2** |
| Local DB note | GoalId **298** was Morata; corrected to Ferran Torres @ 106' |

### Script changes — full detail

#### `scripts/add_sf2_and_final.py` (preferred for Final close-out)

Idempotent SF2 + Final path; **does not wipe** Match/Bet data.

- Docstring: Final close-out Argentina 0–1 Spain a.e.t., Ferran Torres 106'
- Constants: `FINAL_SCORER_NAME = "Ferran Torres"`, `FINAL_GOAL_MINUTE = 106`
- Helpers: `load_team_forwards`, `pick_scorer`, `find_player_id`, `count_goals`, `ensure_team_stats`
- **`apply_final_ft`**:
  - `0–0` → insert Spain goal (Torres if in squad, else Forward fallback)
  - `0–1` → if scorer already Torres, refresh `TimeScored` to 106'; else UPDATE PlayerId/TimeScored (corrects wrong name)
  - Any other scoreline → `SystemExit` (manual fix)
- Teams scoped to WC 2026 via `Group.WorldCupId`
- Prefers seeded squad Forwards over `"Tournament Scorer"` / jersey 99
- After FT print: resolve bets via `POST Bet/ResolveBetsForMatch/{matchId}` (or SyncResult)

#### `scripts/import_wc2026_finished_matches.py` (wipe/reload path)

Full finished schedule through Final; **wipes WC 2026 matches then reloads**.

- `FINAL = 5` stage constant
- `MATCHES` Final row:

```python
(utc("2026-07-19T19:00:00Z"), FINAL, "New York New Jersey", "Argentina", "Spain", 0, 1),
```

- Insert loop special-cases Final Argentina–Spain → Ferran Torres @ 106' (named player when seeded)
- `wipe_world_cup_matches` — multi-cup safe (2026 team scope only; clears feeders, BetResult, Bet, Goal, Card, TeamStats, Match)
- Squad-forward scorer preference + placeholder only for empty squads
- Warning in module docstring about wipe vs upsert path

### Knockout re-advance UX (earlier in phase)

- `GoalService.AddGoal` / `DeleteGoal` return `string?` advance-conflict warning (goal still saved)
- `GoalController` Ok body includes `Warning: …` when advance fails
- Live console shows a warning banner for those responses
- Admin Schedule: **Advance winner** on finished knockout matches → `Knockout/AdvanceFromMatch/{id}`

### Ops procedure (Final FT)

1. Prefer: `python scripts/add_sf2_and_final.py` (idempotent `apply_final_ft`)
2. If bets still open: Admin `POST Bet/ResolveBetsForMatch/{matchId}` for MatchId **116**, or SyncResult
3. Avoid: `import_wc2026_finished_matches.py --i-understand-this-wipes-wc2026` unless a full 2026 wipe/reload is intended (Final scorer is still Torres @ 106' after Bugbot fix, but wipe destroys bets). See [phase-15-data-hygiene](phase-15-data-hygiene.md).

### Explicit non-goals (followed)

- No C# / EF migration in this close-out
- No ThirdPlace fixture
- No Phase 10+ sync/timeline work (already separate phases)

### Related

- [WC 2026 live data](wc2026-live-data.md)
- [Phase 7 bugfixes](phase-7-bugfixes.md)
- [Phase 8 dashboards](phase-8-dashboards.md)
- [Changes Review](../changes-review.md)
