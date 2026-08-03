# WorldCup System — Phase 15 Task 5: Gate

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 15 plan](phase-15-planned.md) · [Task 4 data hygiene](phase-15-data-hygiene.md) · [Task 3 migrate/smoke](phase-15-migrate-smoke.md) · [Task 2 deploy](phase-15-deploy.md) · [Task 1 SPA apiUrl](phase-15-spa-url.md)

## Phase 15 Task 5 — Gate (`p15-gate`)

Opened **3 Aug 2026**. Closed **3 Aug 2026**. Checklist: [Roadmap Phase 15](../roadmap.md#phase-15) — Task 5 **[x]**; Phase 15 **[Done]**.

> **Success:** **Gate closed.** Bugbot: no high/critical; one medium README stale `REPLACE_ME` claim fixed. Dual docs mirrored; secrets placeholders only; Python wipe-guard **2** OK; `dotnet test` N/A (no C# / SPA logic). Phase 15 Production Ship & Ops Hygiene is **Done**.

<a id="review-gate"></a>

## Review gate

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Tasks 1–4 deliverables present | High | **Cleared** | Detail pages + code/scripts as scoped |
| No secrets in git diff | Critical | **Cleared** | SPA `apiUrl` public only; no JWT/DB passwords in Phase 15 tree |
| Dual docs VitePress ↔ git mirror | High | **Cleared** | Both trees updated through Done |
| High/critical Bugbot (SPA env + Python/SQL scripts) | High | **Cleared** | No high/critical findings |
| `dotnet test` (API / SPA logic) | High | **N/A (documented)** | No C# API changes; SPA `apiUrl` string only |
| Python wipe-guard test | Medium (ops) | **Cleared** | `test_import_wc2026_wipe_guard.py` — **2** OK |
| Roadmap / index / changes-review mark Phase 15 Done | High | **Cleared** | Task 5 `[x]`; Phase 15 **[Done]**; plan 15/15 |
| Ship SPA with localhost `apiUrl` / weak Compose secrets | Medium (ops) | Documented | Local rehearsal — swap public host later; `PROD_*` stay host-only |

**Gate verdict:** **Closed** — Phase 15 **Done**.

### Review findings

| Source | Finding | Severity | Status |
| --- | --- | --- | --- |
| Bugbot | Workspace README still claimed git default `REPLACE_ME` after Task 1 set `localhost:5055` | Medium | **Fixed** — root + nested README point at Phase 15 rehearsal URL + `phase-15-spa-url` |
| Markdown docs subagent | Dual-docs gate page + changes-review / index / roadmap / planned / VitePress | High (docs) | **Cleared** |

### Secrets check

| Check | Expected | Status |
| --- | --- | --- |
| No real passwords / JWT secrets / connection strings in Phase 15 tracked diffs | Public `apiUrl` only; scripts keep existing local Dev `WC_DB` default (not new secrets) | **Cleared** |
| `.env` with real values not staged | `.env` gitignored; session `PROD_*` never committed | **Cleared** |
| Production Compose still uses `PROD_*` required vars | Unchanged from Phase 14 | **Cleared** |
| SPA env has no JWT/DB secrets | Origin string only | **Cleared** — [phase-15-spa-url](phase-15-spa-url.md) |

### Tests decision

| Suite | Warranted? | Decision |
| --- | --- | --- |
| `dotnet test` (`WorldCup-System.Tests`) | **No** for this phase tree | **Skipped** — no C# API changes; SPA change is `apiUrl` config only |
| Python wipe-guard | **Yes** (script logic) | **Passed** — Ran 2 tests OK (3 Aug 2026) |

```powershell
cd WorldCup-System\scripts
python test_import_wc2026_wipe_guard.py
```

**Result (3 Aug 2026):** Passed — Failed: **0**, Passed: **2**, Skipped: **0**.

**Baseline reminder:** Phase 14 consented suite **462** remains the last API gate result. Phase 15 does not invalidate it by C# churn.

---

## Scope of this gate

Formal close across all Phase 15 tasks — not a new product feature.

| Task | Id | Detail | Impl status |
| --- | --- | --- | --- |
| 1 | `p15-spa-url` | [phase-15-spa-url](phase-15-spa-url.md) | **Done** — local rehearsal `http://localhost:5055` |
| 2 | `p15-deploy` | [phase-15-deploy](phase-15-deploy.md) | **Done** — local Production Compose; `/health` 200 |
| 3 | `p15-migrate-smoke` | [phase-15-migrate-smoke](phase-15-migrate-smoke.md) | **Done** — MigrateAsync + smoke green; no wipe |
| 4 | `p15-data-hygiene` | [phase-15-data-hygiene](phase-15-data-hygiene.md) | **Done** — wipe confirm flag + multi-cup seed |
| 5 | `p15-gate` | This page | **Done** (3 Aug 2026) |

---

## Git status (Phase 15 working tree — Task 5 snapshot)

Repo: nested `WorldCup-System/` (branch `master`). Captured for gate docs (**3 Aug 2026**). Uncommitted / untracked Phase 15 paths include:

```
 M README.md
 M docs/.vitepress/config.mts
 M docs/changes-review.md
 M docs/changes/phase-9-ops-closeout.md
 M docs/changes/wc2026-live-data.md
 M docs/index.md
 M docs/roadmap.md
 M scripts/import_wc2026_finished_matches.py
 M scripts/seed-wc2026-groups.sql
 M scripts/simulate_betting_demo.py
 M scripts/simulate_wc2030.py
 M worldcup-client/README.md
 M worldcup-client/src/environments/environment.ts
?? docs/changes/phase-15-data-hygiene.md
?? docs/changes/phase-15-deploy.md
?? docs/changes/phase-15-migrate-smoke.md
?? docs/changes/phase-15-planned.md
?? docs/changes/phase-15-spa-url.md
?? docs/changes/phase-15-gate.md
?? scripts/test_import_wc2026_wipe_guard.py
```

Also (workspace root, outside nested git): `README.md` Production SPA section updated for Phase 15 rehearsal URL.

Key runtime / ops files (no C# API):

| Change | Path | Task |
| --- | --- | --- |
| Modified | `worldcup-client/src/environments/environment.ts` (`apiUrl` only) | 1 |
| Modified | `worldcup-client/README.md` | 1 |
| Modified | `scripts/import_wc2026_finished_matches.py` (wipe confirm flag) | 4 |
| Modified | `scripts/seed-wc2026-groups.sql` (year/cup-scoped) | 4 |
| Modified | `scripts/simulate_wc2030.py`, `simulate_betting_demo.py` (2030 wipe banners) | 4 |
| **New** | `scripts/test_import_wc2026_wipe_guard.py` | 4 / 5 |
| **New** | `docs/changes/phase-15-*.md` (spa/deploy/migrate/hygiene/planned/gate) | 1–5 |

Outside nested git: workspace-root `docs/` (VitePress) — must stay mirrored for dual-docs.

---

## Diff snippets (representative)

### SPA production `apiUrl` (Task 1)

```typescript
export const environment = {
  production: true,
  apiUrl: 'http://localhost:5055',
};
```

### Wipe confirm guard (Task 4)

```python
WIPE_CONFIRM_FLAG = "--i-understand-this-wipes-wc2026"

def require_wipe_confirmation(args: argparse.Namespace) -> None:
    if args.confirm_wipe:
        return
    raise SystemExit(
        "Refusing to wipe WC 2026 matches without confirmation.\n"
        f"  Prefer upsert:  python add_sf2_and_final.py\n"
        f"  Full wipe:      python import_wc2026_finished_matches.py {WIPE_CONFIRM_FLAG}\n"
        "See docs/changes/phase-15-data-hygiene.md"
    )
```

### Multi-cup group seed (Task 4)

```sql
SELECT "Id" INTO wc_id FROM "WorldCups"
WHERE EXTRACT(YEAR FROM "Year" AT TIME ZONE 'UTC') = 2026
ORDER BY "Id"
LIMIT 1;
```

---

## Agent do / do not

| Do | Do not |
| --- | --- |
| Keep dual docs mirrored through gate close | Re-open Phase 15 after Closed without a new roadmap phase |
| Prefer Python wipe-guard test for script changes | Run `dotnet test` / `dotnet build` without consent |
| Note localhost `apiUrl` is local rehearsal only | Commit real `.env` or production passwords |
| Ask before commit / push | Treat Tasks 1–3 local rehearsal as public production ship |

---

## Related

- [Roadmap Phase 15](../roadmap.md#phase-15)
- [phase-15-planned](phase-15-planned.md) — Task 5 section
- [Changes Review](../changes-review.md)
- [phase-15-spa-url](phase-15-spa-url.md) · [phase-15-deploy](phase-15-deploy.md) · [phase-15-migrate-smoke](phase-15-migrate-smoke.md) · [phase-15-data-hygiene](phase-15-data-hygiene.md)
- [Phase 14 gate (closed)](phase-14-gate.md)
