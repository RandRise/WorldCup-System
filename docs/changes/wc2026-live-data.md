# WC 2026 live data & schedule import — Change Log

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md)

## WC 2026 live data & schedule import

15–17 Jul 2026 — Match schema repair, 48-team group draw, finished results through both semi-finals. [← Changes Review](../changes-review.md) · [Roadmap Phase 7](../roadmap.md#phase-7)

### Problems fixed

| Symptom | Cause | Fix |
| --- | --- | --- |
| `Npgsql.PostgresException` — `FeederMatchOneId` does not exist on login/refresh | `AddMatchStage` was marked applied while Feeder* columns / nullable team FKs were missing | Idempotent ALTER in DB + startup repair in `Program.cs` after `MigrateAsync` |
| Add match → `Group stage matches must be between teams in the same group` | Teams sat in placeholder `QF1`–`QF4` groups, not draw groups A–L | `scripts/seed-wc2026-groups.sql` — create A–L, assign 48 nations to the official draw |
| Empty / non-tournament schedule for live WC 2026 | Demo CSV seed removed; no offline results load | `scripts/import_wc2026_finished_matches.py` — group + knockout finished fixtures + goals |

### Data imported (as of 17 Jul 2026)

Scripts define:

- **Groups A–L** — 4 teams each (official FIFA draw) via `seed-wc2026-groups.sql`
- **72** finished group-stage matches
- **16** Round of 32 · **8** Round of 16 · **4** quarter-finals · **2** semi-finals
  - SF1: France 0–2 Spain (Dallas, 14 Jul)
  - SF2: England 1–2 Argentina (Atlanta, 15 Jul) — now in `import_wc2026_finished_matches.py`
- **Not finished in import:** Final Argentina vs Spain (19 Jul, MetLife) — **scheduled** via `add_sf2_and_final.py` (MatchId 116, no score until FT)
- **Not used:** Third-place playoff (`MatchStage.ThirdPlace` exists but skipped for this path)
- Scores materialised as `Goal` rows on placeholder `Tournament Scorer` players so standings/fixtures show FT results

Re-run the import (or Admin live console) on your local DB if the DB was last loaded before SF2 was added to the script.

### Code / schema

- `MatchStage.RoundOf32 = 6` (+ admin stage option, bracket sort order)
- `Program.cs` — IF NOT EXISTS repair for Feeder* / nullable team columns
- Migration history still records `20260713120000_AddMatchStage`; repair covers stuck local DBs

### How to re-run (dev)

```
# Groups + teams (idempotent SQL)
psql -h localhost -U postgres -d TestDatabase -f WorldCup-System/scripts/seed-wc2026-groups.sql

# Finished matches (clears Match/TeamStats/Goal/Bet rows, then reloads through both SFs)
python WorldCup-System/scripts/import_wc2026_finished_matches.py
# Optional: set WC_DB="host=... dbname=... user=... password=..."

# Idempotent: ensure SF2 + Final fixture (Final without score, for betting)
python WorldCup-System/scripts/add_sf2_and_final.py
```

Sources for results: FIFA schedule/results pages and published knockout scores (Yahoo Sports). Group standings were verified against published tables after import. Further results: extend the import script or use the Admin live console (Phase 8).

### Uncommitted status

Scripts and `RoundOf32` / Feeder schema remain in the shared Phase 7+8 working tree (not committed). See [Changes Review](../changes-review.md) · [Phase 7 detail](phase-7-bugfixes.md).

### Out of scope

- Live external football API poller — **cancelled** (manual import / Admin live console)
- Player-accurate goalscorers / cards (placeholder scorers only)
- Penalty shoot-out winners beyond FT/ET scoreline
