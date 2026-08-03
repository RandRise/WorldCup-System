# WorldCup System — Phase 15 Task 4: WC 2026 Data Hygiene

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 15 plan](phase-15-planned.md) · [Phase 9 ops](phase-9-ops-closeout.md) · [WC 2026 live data](wc2026-live-data.md)

## Phase 15 Task 4 — WC 2026 data hygiene (`p15-data-hygiene`)

Opened **3 Aug 2026**. Checklist: [Roadmap Phase 15](../roadmap.md#phase-15) — Task 4 **[x]** Done. Next: Task 5 gate — [phase-15-planned](phase-15-planned.md#task-5--gate-p15-gate).

> **Success:** Operators can tell wipe vs upsert without reading source. WC 2026 full wipe requires `--i-understand-this-wipes-wc2026`. Prefer `add_sf2_and_final.py` for Final corrections. 2030 wipe flags documented as 2030-only. Group seed is year-scoped / multi-cup safe. No wipe executed this task.

<a id="review-gate"></a>

## Review gate

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Bare `import_wc2026_finished_matches.py` wipes silently | **Critical** | **Cleared** | Confirm flag required; refuse otherwise |
| Operator cannot tell wipe vs upsert | **High** | **Cleared** | Inventory table + script banners |
| 2030 `--wipe-matches` confused with WC 2026 | **High** | **Cleared** | Help text + runtime WARNING (2030-only) |
| `seed-wc2026-groups.sql` moves 2030 teams by CountryId | **High** | **Cleared** | Year-scoped WC resolve + cup-scoped Team UPDATE |
| Dual docs VitePress ↔ git mirror | **High** | **Cleared this Markdown pass** | `docs/` and `WorldCup-System/docs/` |
| Unintended wipe during hygiene work | **Critical** | **Cleared** | No wipe/import run against any DB |

**Gate verdict:** Task 4 **high/critical** items **Cleared**. Formal Phase 15 close remains Task 5 (`p15-gate`).

---

## Locked approach

| Choice | Decision |
| --- | --- |
| Preferred Final path | `add_sf2_and_final.py` (idempotent upsert; no wipe) |
| Full 2026 reload | Only with `--i-understand-this-wipes-wc2026` |
| 2030 resim | `--wipe-matches` already opt-in; document 2030 scope loudly |
| Prod | Never run wipe importers against production tournament data |

---

<a id="inventory"></a>

## Script inventory (wipe vs safe)

| Script | Scope | Mode | Guard |
| --- | --- | --- | --- |
| `add_sf2_and_final.py` | WC 2026 SF2 + Final | **Upsert** (safe) | None needed — prefer this |
| `import_wc2026_finished_matches.py` | WC 2026 all finished matches | **Wipe + reload** | Requires `--i-understand-this-wipes-wc2026` |
| `seed-wc2026-groups.sql` | WC 2026 groups A–L | Idempotent assign | Year `2026` resolve; Team UPDATE cup-scoped |
| `seed_wc2026_players.py` | WC 2026 squads | Upsert players | Does not wipe matches/bets |
| `reassign_placeholder_goals.py` | Goals PlayerId only | Non-destructive | No score/bet wipe |
| `sync_scorers_backfill.ps1` | Mapped matches | Scorers-only API | No FT / bet resolve |
| `simulate_wc2030.py --wipe-matches` | **2030 only** | Wipe + resim | Opt-in flag; prints 2030-only WARNING |
| `simulate_betting_demo.py --wipe-bets` | **2030 only** | Wipe bets | Opt-in flag; help text says 2030 |

### Prefer for corrections

```powershell
# Nested git: WorldCup-System/scripts
python add_sf2_and_final.py
```

### Full wipe (dev only — destroys MatchIds + bets)

```powershell
python import_wc2026_finished_matches.py --i-understand-this-wipes-wc2026
```

Bare run refuses:

```text
Refusing to wipe WC 2026 matches without confirmation.
```

---

## Files changed

| Path | Change |
| --- | --- |
| `scripts/import_wc2026_finished_matches.py` | Confirm flag + refuse without it |
| `scripts/test_import_wc2026_wipe_guard.py` | **New** — 2 unittest cases |
| `scripts/seed-wc2026-groups.sql` | Year-2026 resolve; multi-cup Team UPDATE |
| `scripts/simulate_wc2030.py` | Docstring / help / wipe WARNING (2030-only) |
| `scripts/simulate_betting_demo.py` | `--wipe-bets` help clarifies 2030-only |
| `docs/changes/phase-15-data-hygiene.md` | This page (dual) |
| Docs (dual) | Roadmap / index / changes-review / planned / wc2026-live-data / VitePress |

Roadmap mapping: [Phase 15](../roadmap.md#phase-15) — `p15-data-hygiene` **Done** `[x]`.

---

## Verify

| Check | Pass when | Evidence |
| --- | --- | --- |
| Docs/scripts mark wipe vs upsert | Inventory readable without source dive | [Inventory](#inventory) |
| Bare importer refuses | `SystemExit` with prefer-upsert message | `python test_import_wc2026_wipe_guard.py` → **2** OK |
| Confirm flag accepted by argparse | `confirm_wipe=True` | Same unittest |
| No unintended wipe | Hygiene work did not run wipe against DB | This page — none run |
| WC 2026 Final intact | No wipe executed | Prefer upsert path unchanged |

```powershell
cd WorldCup-System\scripts
python test_import_wc2026_wipe_guard.py
```

---

## Related

- [Phase 15 planned Task 4](phase-15-planned.md#task-4--wc-2026-data-hygiene-p15-data-hygiene)
- [Phase 9 ops close-out](phase-9-ops-closeout.md)
- [WC 2026 live data](wc2026-live-data.md)
- [Phase 14 migrate/smoke wipe warnings](phase-14-migrate-smoke.md)
- [Roadmap Phase 15](../roadmap.md#phase-15)
- [Changes Review](../changes-review.md)
