# WorldCup System — Phase 15 Planned Work

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 14 gate (closed)](phase-14-gate.md)

## Phase 15 — Production Ship & Ops Hygiene

**Status: Done** — Tasks 1–5 complete; Task 5 gate **Closed** — [phase-15-gate](phase-15-gate.md). Product Phases **1–13** and Deploy Readiness (**14**) are Done. This phase **ships** that packaging and hardens day-2 data ops. Checklist: [Roadmap Phase 15](../roadmap.md#phase-15).

> **Success criteria for Phase 15 Done:** production SPA points at the real API; host runs Production Compose with real secrets (never committed); target DB migrated + smoke table green; WC 2026 wipe/re-import risk documented or guarded; dual docs updated; no high/critical review blockers; consented `dotnet test` green when code changed. **Met for local rehearsal** — gate **Closed**.

### Why

Phase 14 packaged deploy readiness. Remaining risk is **never actually deploying**, shipping SPA with `REPLACE_ME`, and **ops accidents** (wipe scripts against WC 2026).

### Agent rules (read before every task)

| Rule | Detail |
| --- | --- |
| One task at a time | Finish Task N checkboxes + dual-docs note before starting N+1 |
| Ask before push | Never `git push` unless the user explicitly asks |
| Ask before commit | Never `git commit` unless the user explicitly asks |
| Ask before `dotnet build` | User rule — do not build without consent |
| Ask before `dotnet test` | Prefer asking; required at Task 5 only if code changed (or user says “phase gate”) |
| No secrets in git | Never commit real passwords, JWT secrets, connection strings, or `.env` with values |
| Dual docs | Edit VitePress `docs/` **and** mirror `WorldCup-System/docs/` the same change |
| Git repo root | Commands run from `WorldCup-System/` (nested git), not workspace root |
| Do not reopen Phases 1–14 | No new product features; ship + ops hygiene only |

### Locked decisions (v1)

| Choice | Decision |
| --- | --- |
| Scope | **Ship + ops hygiene**, not new product features or cloud IaC |
| Hosting | Use Phase 14 Compose override + secrets inventory |
| SPA URL | Real origin before production `ng build` — no `REPLACE_ME` in shipped assets |
| WC 2026 | Prefer upsert / Final-only scripts; treat wipe paths as dangerous |
| Phase 14 git | Already on `origin/master` (`1358008`) — no re-push required for packaging |

### Goals (summary)

| # | Task id | Theme | Outcome | Status |
| --- | --- | --- | --- | --- |
| 1 | `p15-spa-url` | Client | Real production `apiUrl` (no `REPLACE_ME`) before `ng build` | **Done** — [detail](phase-15-spa-url.md) |
| 2 | `p15-deploy` | Docker | Production Compose up with real `PROD_*` env | **Done** — [detail](phase-15-deploy.md) |
| 3 | `p15-migrate-smoke` | Ops | Target migrate + smoke checklist green | **Done** — [detail](phase-15-migrate-smoke.md) |
| 4 | `p15-data-hygiene` | Data | WC 2026 wipe/re-import safeguards | **Done** — [detail](phase-15-data-hygiene.md) |
| 5 | `p15-gate` | Gate | Dual-docs close + review (+ tests if code changed) | **Done** — [detail](phase-15-gate.md) |

### Execution order (strict)

```text
Task 1 SPA apiUrl  →  Task 2 deploy  →  Task 3 migrate/smoke
    →  Task 4 data hygiene  →  Task 5 gate
```

Task 1 may run in parallel with host secret setup for Task 2 if the API origin is already known.

---

### Task 1 — SPA production `apiUrl` (`p15-spa-url`) **[Done]**

**Goal:** Production SPA must call the real API host — not `https://api.REPLACE_ME.example`.

> **Success:** Production `environment.ts` uses a ship-ready API origin; Dev localhost unchanged. **Done 3 Aug 2026** as **local ship rehearsal** (`http://localhost:5055`) — no public domain yet; swap when hosted. Detail: [phase-15-spa-url](phase-15-spa-url.md). See [phase-14-spa-prod](phase-14-spa-prod.md).

#### Do

| Step | Action | Status |
| --- | --- | --- |
| 1 | Set production `apiUrl` to the real API origin | **Done** — local rehearsal `http://localhost:5055` |
| 2 | Confirm Dev `environment` still points at local API | **Done** — unchanged |
| 3 | Document the chosen host on the Task 1 detail note | **Done** — [phase-15-spa-url](phase-15-spa-url.md) |

#### Don't

| Avoid | Why |
| --- | --- |
| Commit secrets in `environment.ts` | API URL is usually public; never put JWT/DB secrets in SPA |
| Ship with `REPLACE_ME` | Browsers will call a dead host |

#### Verify

| Check | Pass when | Evidence |
| --- | --- | --- |
| Production env has no `REPLACE_ME` | Grep clean | `environment.ts` → `http://localhost:5055` |
| Dev env still localhost / Dev API | Unchanged | `environment.development.ts` |

---

### Task 2 — Production deploy (`p15-deploy`) **[Done]**

**Goal:** Bring API + Postgres up with `docker-compose.prod.yml` and real `PROD_*` values.

> **Success:** Stack runs with `ASPNETCORE_ENVIRONMENT=Production`; DevAdmin/DevUser unset; CORS/JWT match the SPA origin. **Done 3 Aug 2026** as **local ship rehearsal** — `/health` **200** `Healthy`. Detail: [phase-15-deploy](phase-15-deploy.md). See [phase-14-compose](phase-14-compose.md) · [phase-14-secrets](phase-14-secrets.md).

#### Do

| Step | Action | Status |
| --- | --- | --- |
| 1 | Set required `PROD_*` (and related) env on the host — never commit them | **Done** — session-only random values |
| 2 | `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d` (or host-equivalent) | **Done** — `up -d --build`; api + db healthy |
| 3 | Confirm `/health` responds | **Done** — HTTP 200 `Healthy` |

#### Don't

| Avoid | Why |
| --- | --- |
| Reuse Dev `.env` keys alone | Prod override requires `PROD_*` (`${VAR:?…}`) |
| Commit `.env` with real values | Secrets leak |

#### Verify

| Check | Pass when | Evidence |
| --- | --- | --- |
| Health endpoint OK | HTTP 200 from API `/health` | `Healthy` on `:5055` |
| No Dev seed accounts expected | Production env clears DevAdmin/DevUser | Override clears seed env |

---

### Task 3 — Migrate + smoke on target (`p15-migrate-smoke`) **[Done]**

**Goal:** Apply EF migrations to the **target** DB and walk the Phase 14 smoke table.

> **Success:** Migrations applied; health, company create/join, scoped leaderboard, CORS, fixtures checks recorded. **Done 3 Aug 2026** as **local ship rehearsal** — Compose `MigrateAsync` + history verify (**28**); smoke table green. Detail: [phase-15-migrate-smoke](phase-15-migrate-smoke.md). See [phase-14-migrate-smoke](phase-14-migrate-smoke.md).

#### Do

| Step | Action | Status |
| --- | --- | --- |
| 1 | `dotnet ef database update` against the target connection (ask before if destructive) | **Done** — applied via startup `MigrateAsync`; host `ef` blocked by Docker Desktop publish; SQL verified |
| 2 | Run smoke rows: health, company, leaderboard scope, CORS, fixtures | **Done** — all 7 green |
| 3 | Note wipe warnings — do not wipe production tournament data | **Done** — no wipe scripts run |

#### Don't

| Avoid | Why |
| --- | --- |
| Wipe scripts on prod WC 2026 | Irreversible data loss |
| Skip company isolation smoke | Phase 13 is a deploy regression risk |

#### Verify

| Check | Pass when | Evidence |
| --- | --- | --- |
| Smoke table all green | Per [phase-14-migrate-smoke](phase-14-migrate-smoke.md) | [phase-15-migrate-smoke](phase-15-migrate-smoke.md#smoke-results) |

---

### Task 4 — WC 2026 data hygiene (`p15-data-hygiene`) **[Done]**

**Goal:** Reduce accidental wipe/re-import risk for finished WC 2026 data.

> **Success:** Operators know which scripts are upsert-safe vs wipe; guardrails in place. **Done 3 Aug 2026** — confirm flag on wipe importer; multi-cup group seed; inventory. Detail: [phase-15-data-hygiene](phase-15-data-hygiene.md). See [phase-9-ops-closeout](phase-9-ops-closeout.md).

#### Do

| Step | Action | Status |
| --- | --- | --- |
| 1 | Inventory dangerous wipe paths (`import_wc2026_finished_matches.py` wipe, etc.) | **Done** — [inventory](phase-15-data-hygiene.md#inventory) |
| 2 | Prefer Final-only / upsert scripts (`add_sf2_and_final.py`) for corrections | **Done** — documented + refuse message |
| 3 | Add or tighten operator warnings / confirm flags as needed | **Done** — `--i-understand-this-wipes-wc2026`; 2030 banners |

#### Don't

| Avoid | Why |
| --- | --- |
| Re-run full wipe imports “just to refresh” | Destroys MatchIds, goals, resolved bets |
| Treat 2030 sim wipe flags as WC 2026-safe without checking scope | Cross-cup mistakes |

#### Verify

| Check | Pass when | Evidence |
| --- | --- | --- |
| Docs or scripts clearly mark wipe vs upsert | Operator can tell without reading source | [phase-15-data-hygiene](phase-15-data-hygiene.md#inventory) |
| WC 2026 Final data remains intact after hygiene work | No unintended wipe | No wipe scripts run |

---

### Task 5 — Gate (`p15-gate`) **[Done]**

**Goal:** Close Phase 15 formally.

> **Success:** Roadmap / index / changes-review mark Phase 15 Done; dual docs mirrored; high/critical review findings cleared; `dotnet test` green **if** API/SPA code changed (with consent). **Gate closed 3 Aug 2026** — [phase-15-gate](phase-15-gate.md). Bugbot: no high/critical; medium README `REPLACE_ME` fixed. `dotnet test` **N/A**; Python wipe-guard **2** OK.

#### Do

| Step | Action | Status |
| --- | --- | --- |
| 1 | Bugbot + Markdown docs review (phase gate skill) | **Done** |
| 2 | Fix high/critical findings | **Done** — none; medium README fixed |
| 3 | Consented tests only when code changed | **N/A** — Python wipe-guard **2** OK |
| 4 | Mark roadmap Task 1–5 `[x]`; Phase 15 **[Done]** | **Done** |

#### Don't

| Avoid | Why |
| --- | --- |
| Skip review because “docs-only deploy” | Still verify secrets not committed and checklists honest |
| Start a Phase 16 without closing this gate | Workflow rule |
