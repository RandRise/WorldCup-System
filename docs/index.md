# WorldCup System — Project Hub

## Docs navigation

[Dashboard](index.md) | [System Overview](overview.md) | [Completion Roadmap](roadmap.md) | [Dev Workflow](workflow.md) | [API Status](api-status.md) | [Data Model](data-model.md) | [Changes Review](changes-review.md)

---
## Project Dashboard

FIFA World Cup management platform — status as of code audit **17 Jul 2026**.

> **Success:** **Phases 1–8 complete in code.** Phase 7–8 review + test gate passed (16 Jul 2026) — **245 tests** green. **Current work:** [Phase 9](roadmap.md#phase-9) (Final FT + commit still open) and **[Phase 10](roadmap.md#phase-10) Task 1** — post-match FIFA sync API done in code; fixtures UX / styling not started; Task 1 review gate open. See [sync detail](changes/phase-10-match-sync.md) · [plan](changes/phase-10-planned.md) · [WC 2026 data log](changes/wc2026-live-data.md).

> **Info:** Browse these docs with VitePress (`npm run docs:dev` from the workspace root) or open any `.md` file in Cursor preview. Checklists use `[x]` / `[ ]` task items.

- **Overall Plan Progress:** 8 / 10 phases done — [Phase 9 in progress](roadmap.md#phase-9); [Phase 10 in progress](roadmap.md#phase-10) (Task 1 API)
- **Backend API Complete:** 100% Phases 1–8 product scope + Phase 10 Task 1 sync endpoints
- **Frontend SPA:** 100% Phases 1–8 product scope (Task 1 client models/API only; no fixtures UX/styling yet)

### Component Status

### ✅ Done

- Auth & Identity (JWT, roles, GetMe, CORS, secrets)
- Full domain APIs — teams, coaches, players, matches, knockout bracket, goals, cards, stats, standings, bets
- Angular SPA — fixtures, bracket, standings, bets, leaderboard, user dashboard, admin hub / live console
- Offline WC 2026 import — groups A–L + finished matches through both semi-finals
- Tests (245 passed), Docker Compose, health checks, Serilog, GitHub Actions CI
- MCP read-only database server
- Phase 9 (partial): Final fixture scheduled; Feeder* migration repair confirmed; knockout re-advance UX
- Phase 10 Task 1 (API): FIFA calendar sync, `ExternalMatchId`, Admin SyncResult / SyncFinishedResults / SetExternalMatchId; no RabbitMQ

### ⏳ Phase 9 — Remaining

- Record **Final** result after FT (19 Jul) — Admin live console, sync API, or extend import
- **Commit** Phase 7–8 (+ later) working tree when you ask

### 🔄 Phase 10 — In progress

- **Done:** Post-match external result sync → apply → resolve bets (no RabbitMQ in v1) — [detail](changes/phase-10-match-sync.md)
- **Open:** Fixtures UX — bettable / live matches first
- **Open:** World Cup–fitting professional styling & backgrounds  
  Plan: [phase-10-planned](changes/phase-10-planned.md)

### Phase snapshot

| Phase | Name | Status |
| --- | --- | --- |
| 1 | [Foundation & Auth](roadmap.md#phase-1) | Done |
| 2 | [Tournament Setup APIs](roadmap.md#phase-2) | Done |
| 3 | [Match Lifecycle](roadmap.md#phase-3) | Done |
| 4 | [Betting Module](roadmap.md#phase-4) | Done |
| 5 | [Frontend SPA](roadmap.md#phase-5) | Done |
| 6 | [Quality & Ops](roadmap.md#phase-6) | Done |
| 7 | [Post-Launch / Live](roadmap.md#phase-7) | Done (uncommitted) |
| 8 | [User & Admin Dashboards](roadmap.md#phase-8) | Done (uncommitted) |
| 9 | [Ops & Tournament Close-Out](roadmap.md#phase-9) | In progress |
| 10 | [Match Sync, Fixtures UX & Styling](roadmap.md#phase-10) | In progress (Task 1 API) |

### Quick Navigation

#### 🗺️ Completion Roadmap

10 phases with checklists reflecting the latest code review.

#### ⚙️ Dev Workflow

Setup, migrations, API development, and testing steps.

#### 🔌 API Status

All domain controllers wired — coverage matrix.

#### 🗄️ Data Model

Entity relationships and table inventory.

#### 📋 System Overview

Tech stack, folder structure, and architecture.

#### 🔍 Changes Review

Phase 10 Task 1 match sync; Phase 9 ops close-out; Phase 8 dashboards; Phase 7 bugfixes; prior phases.

### Recommended next work

#### Close Phase 10 Task 1 gate

Fix high sync review risks, add MatchSync tests, run `dotnet test` — see [Review gate](changes/phase-10-match-sync.md#review-gate).

#### Phase 9 — Final FT & commit

After 19 Jul, record the Final score. Commit Phase 7–10 when ready.

#### Phase 10 Tasks 2–3 — After you say go

Fixtures betting priority, WC visual polish — see [plan](changes/phase-10-planned.md).

### Local Dev URLs

| Service | URL | Status |
| --- | --- | --- |
| API (Swagger) | `http://localhost:5055/swagger` | Available |
| Frontend (Angular) | `http://localhost:4200` | Built |
| Health | `http://localhost:5055/health` | Available |
| PostgreSQL | User Secrets / Docker Compose | Configured |
| MCP Database Tools | Cursor MCP — `worldcup-database` | Complete |
