# WorldCup System — Phase 10 Task 4: Project Cleanup

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

[← Changes Review](../changes-review.md) · [Phase 10 plan](phase-10-planned.md)

## Phase 10 Task 4 — Project cleanup (duplicates & unused) **[Done]**

Implemented **20 Jul 2026** as [phase-10-planned](phase-10-planned.md) item 4 (`p10-cleanup`). Checklist: [Roadmap Phase 10](../roadmap.md#phase-10).

### Review gate

High/critical risks that must be clear **before** Task 4 regression testing (`dotnet test` + Jasmine). Docs-only / filesystem cleanup — no API or SPA source edits in this task.

| Item | Severity | Status | Notes |
| --- | --- | --- | --- |
| Accidental delete of canonical SPA | Critical | **Cleared** | Only workspace-root `worldcup-client/` removed; `WorldCup-System/worldcup-client/` intact |
| Accidental delete of git-tracked docs | High | **Cleared** | Dual docs kept (VitePress + git mirror); policy below |
| Broken README / workflow client path | High | **Cleared** | README + overview point at `WorldCup-System/worldcup-client` |
| Open high/critical Bugbot findings | High (gate) | **None** | Cleanup is path inventory + deletes; no new product code |

**Gate verdict:** no open high/critical blockers — safe to run regression tests from the canonical client path.

### Git scope (20 Jul 2026)

Git repo root: `WorldCup-System/` only.

| Scope | What changed | Visible in `git status`? |
| --- | --- | --- |
| **Outside git** | Deleted workspace-root `worldcup-client/` (~265 MB) + `TestDatabase_backup_20260629_043805.dump` | No — never tracked |
| **Inside git (empty dirs)** | Removed empty `admin/seed/` + `admin/reference/` | No — empty dirs are not tracked |
| **Inside git (docs)** | Task 4 detail + roadmap / index / changes-review / planned / VitePress sidebar | Yes — see file list below |

Unrelated untracked in same tree (not Task 4): `scripts/data/wc2026_players.csv`, `scripts/seed_wc2026_players.py`.

#### Git file list (docs Task 4 documentation)

```text
M  docs/.vitepress/config.mts
M  docs/changes-review.md
M  docs/changes/phase-10-planned.md
M  docs/changes/wc2026-live-data.md
M  docs/index.md
M  docs/overview.md
M  docs/roadmap.md
M  docs/workflow.md
?? docs/changes/phase-10-cleanup.md
```

### Inventory decisions (20 Jul 2026)

| Path | Verdict | Evidence |
| --- | --- | --- |
| `WorldCup-System/worldcup-client/` | **KEEP** | Canonical SPA — black/gold theme, `fixture-sections`, dashboards/live/hub; **99** files in git |
| Workspace-root `worldcup-client/` | **DELETE** | Stale green-theme clone; missing Phase 10 Task 2–3; **outside git** (~265 MB w/ `node_modules`) |
| Workspace `docs/` | **KEEP** | VitePress source of truth (`npm run docs:dev`) |
| `WorldCup-System/docs/` | **KEEP** (mirror) | Byte-identical content; **only git-tracked** docs copy; skill prompts mirror updates here |
| Root `node_modules/` | **KEEP** | VitePress deps for `worldcup-system-docs` |
| `mcp-server/` | **KEEP** | Cursor PostgreSQL MCP tooling |
| `TestDatabase_backup_20260629_043805.dump` | **DELETE** | Orphan ~55 KB dump; no README/script refs |
| Empty `admin/seed/` + `admin/reference/` dirs | **DELETE** | Cancelled Phase 5 leftovers; 0 files |
| `WorldCup-System/scripts/` | **KEEP** | WC 2026 offline ops (Phase 9) |
| MatchSync / FIFA provider | **KEEP** | Phase 10 Task 1 — not cancelled live API |

### Dual docs policy (accepted)

Git repo lives under `WorldCup-System/` only. VitePress runs from the workspace root against `docs/`. Until the git root expands to the workspace, **both** trees stay:

1. Edit workspace `docs/` for VitePress / Cursor skills
2. Mirror the same Markdown into `WorldCup-System/docs/` so history stays in git

Do **not** delete either tree without relocating VitePress or expanding the git root.

### Explicit non-goals (followed)

- No API refactors without unused evidence
- No git history rewrite / force-push
- Phase 9 Final FT tooling retained
- Collapsing dual docs without expanding git root / relocating VitePress

### Removals performed

| Path | Action | Date | Git-tracked? |
| --- | --- | --- | --- |
| `worldcup-client/` (workspace root) | Deleted entire stale SPA | 20 Jul 2026 | No |
| `TestDatabase_backup_20260629_043805.dump` | Deleted orphan dump | 20 Jul 2026 | No |
| `WorldCup-System/worldcup-client/.../admin/seed/` | Removed empty dir | 20 Jul 2026 | No (empty) |
| `WorldCup-System/worldcup-client/.../admin/reference/` | Removed empty dir | 20 Jul 2026 | No (empty) |

### Doc / README updates

| File | Change |
| --- | --- |
| Workspace `README.md` | Layout + `cd` path → `WorldCup-System/worldcup-client` (outside git) |
| `docs/workflow.md` | Removed stale WeatherForecast auth sentence |
| `docs/overview.md` | Canonical client path; cleared duplicate risk row |
| `docs/roadmap.md` | Task 4 checked `[x]` |
| `docs/index.md` / `docs/changes-review.md` | Task 4 done status + mapping |
| `docs/changes/phase-10-planned.md` | Task 4 outcome; status Done |
| `docs/.vitepress/config.mts` | Sidebar link: Phase 10 — Cleanup (Task 4) |

### Roadmap mapping

| Roadmap item | Status | Evidence |
| --- | --- | --- |
| `p10-cleanup` — Project cleanup scan | **Done** | Removals + dual-docs policy above; [roadmap § Phase 10](../roadmap.md#phase-10) |

### Verify (review → test gate closed 20 Jul 2026)

- [x] `dotnet test` on `WorldCup-System.Tests` — **271** passed (0 failed; +3 MatchSync scorer tests in working tree)
- [x] Client Jasmine from `WorldCup-System/worldcup-client` — **25** / 25 passed
- [x] README / overview path matches only remaining SPA
- [x] Workspace-root `worldcup-client/` absent; dump absent

### Related

- [Phase 10 planned backlog](phase-10-planned.md)
- [Roadmap Phase 10](../roadmap.md#phase-10)
- [Changes Review](../changes-review.md)
- [Phase 5 SPA](phase-5-frontend-spa.md) — original root-client debt
