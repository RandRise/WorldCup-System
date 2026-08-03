# WorldCup System — Project Hub

## Docs navigation

[Dashboard](index.md) | [System Overview](overview.md) | [Completion Roadmap](roadmap.md) | [Dev Workflow](workflow.md) | [API Status](api-status.md) | [Data Model](data-model.md) | [Changes Review](changes-review.md)

---
## Project Dashboard

FIFA World Cup management platform — status as of **3 Aug 2026**.

> **Success:** **Phases 1–14 complete.** Phase 14 gate closed (Bugbot high fixed; suite **462**) — [phase-14-gate](changes/phase-14-gate.md) · [phase-14-planned](changes/phase-14-planned.md). See [Changes Review](changes-review.md).

> **Info:** Browse these docs with VitePress (`npm run docs:dev` from the workspace root) or open any `.md` file in Cursor preview. Checklists use `[x]` / `[ ]` task items.

- **Overall Plan Progress:** 14 / 14 phases done — product + deploy readiness complete — [Phase 14](roadmap.md#phase-14)
- **Backend API Complete:** 100% Phases 1–8 + Phase 10 Task 1 sync + Phase 11 (stage / timeline / resolve / apply / sync wire / scorers backfill / honesty; suite **382**)
- **Frontend SPA:** 100% Phases 1–8 + Phase 10 Tasks 2–3 (fixtures UX + WC styling); canonical path `WorldCup-System/worldcup-client`

### Component Status

### ✅ Done

- Auth & Identity (JWT, roles, GetMe, CORS, secrets)
- Full domain APIs — teams, coaches, players, matches, knockout bracket, goals, cards, stats, standings, bets
- Angular SPA — fixtures, bracket, standings, bets, leaderboard, user dashboard, admin hub / live console
- Offline WC 2026 import — groups A–L + finished matches through Final (Spain champions)
- Tests (**268** passed baseline; suite **382** with Phase 11), Docker Compose, health checks, Serilog, GitHub Actions CI
- MCP read-only database server
- **Phase 9 Done:** Final FT Argentina 0–1 Spain a.e.t. (Ferran Torres 106', MatchId 116); bets 2/2; scripts + commits `dcb322b` / `7886b40` — [detail](changes/phase-9-ops-closeout.md)
- **Phase 10 Task 1:** FIFA calendar sync, `ExternalMatchId`, Admin SyncResult / SyncFinishedResults / SetExternalMatchId, orientation + tests; no RabbitMQ
- **Phase 10 Task 2:** Fixtures Action default, filter chips, Open/Live/Finished sections, jump-to-bet, `fixture-sections` + Jasmine **12** — gate closed
- **Phase 10 Task 3:** Black/gold theme, night-pitch atmosphere, Bebas Neue + Manrope, brand-first home — gate closed; [detail](changes/phase-10-styling.md)
- **Phase 10 Task 4:** Removed stale root `worldcup-client` + orphan DB dump; dual docs kept (VitePress + git mirror); README canonical path — [detail](changes/phase-10-cleanup.md)
- **Phase 11 Task 1:** `Match.ExternalStageId`, migration, SetExternal optional stage, calendar IdStage + SyncResult auto-fill — [detail](changes/phase-11-external-stage.md)
- **Phase 11 Task 2:** `FifaTimelineEventsProvider` + DTOs + `TimelineBaseUrl` HttpClient; Goal!/Own Goal/Penalty parse — [detail](changes/phase-11-timeline-provider.md)
- **Phase 11 Task 3:** `TimelinePlayerResolver` + `Player.ExternalPlayerId`; exact → normalized → create; gate closed — [detail](changes/phase-11-player-resolve.md)
- **Phase 11 Task 4:** `TimelineScorerApplyService` — in-place Goal rewrite when counts align; gate closed (**353**) — [detail](changes/phase-11-scorer-apply.md)
- **Phase 11 Task 5:** SyncResult / SyncFinishedResults wire timeline + apply (fail-soft); gate closed (**361**) — [detail](changes/phase-11-sync-wire.md)
- **Phase 11 Task 6:** Admin `SyncScorers` / `SyncScorersForWorldCup` + script; FT/bets unchanged; gate closed (**375**) — [detail](changes/phase-11-backfill.md)
- **Phase 11 Task 7:** Recent events honesty — omit `"Tournament Scorer"`; gate closed (**380**) — [detail](changes/phase-11-recent-events-honesty.md)
- **Phase 11 Task 8:** Tests + review gate — Bugbot highs fixed; suite **382** — [detail](changes/phase-11-tests-gate.md)

### ✅ Phase 9 — Done

| # | Item | Status | Doc |
| --- | --- | --- | --- |
| 1 | Schedule Final (Argentina vs Spain) | **Done** | [phase-9-ops-closeout](changes/phase-9-ops-closeout.md) |
| 2 | Record Final FT | **Done** | Argentina 0–1 Spain a.e.t.; Ferran Torres 106' |
| 3 | Optional ThirdPlace | **Skipped** | — |
| 4 | `AddMatchStage` + Feeder* repair | **Done** | same |
| 5 | Commit Phase 7–8 tree | **Done** | `dcb322b` + `7886b40` |
| 6 | Knockout re-advance UX | **Done** | same |

Full checklist: [Roadmap Phase 9](roadmap.md#phase-9).

### ✅ Phase 10 — Done

| # | Item | Status | Doc |
| --- | --- | --- | --- |
| 1 | Post-match FIFA sync → apply → resolve bets | **Done** | [phase-10-match-sync](changes/phase-10-match-sync.md) |
| — | No RabbitMQ in v1 | **Done** | same |
| 2 | Fixtures UX — bettable / live first | **Done** | [phase-10-fixtures-ux](changes/phase-10-fixtures-ux.md) |
| 3 | WC professional styling | **Done** | [phase-10-styling](changes/phase-10-styling.md) |
| 4 | Project cleanup — duplicates / unused | **Done** | [phase-10-cleanup](changes/phase-10-cleanup.md) |

Full checklist: [Roadmap Phase 10](roadmap.md#phase-10).

### ✅ Phase 11 — Done

| # | Item | Status | Doc |
| --- | --- | --- | --- |
| 1 | External stage id for timeline URLs | **Done** | [phase-11-external-stage](changes/phase-11-external-stage.md) |
| 2 | FIFA timeline events provider | **Done** | [phase-11-timeline-provider](changes/phase-11-timeline-provider.md) |
| 3 | Player identity resolution | **Done** | [phase-11-player-resolve](changes/phase-11-player-resolve.md) |
| 4 | Real-scorer apply (idempotent) | **Done** | [phase-11-scorer-apply](changes/phase-11-scorer-apply.md) |
| 5 | Wire SyncResult / SyncFinishedResults | **Done** | [phase-11-sync-wire](changes/phase-11-sync-wire.md) |
| 6 | Admin scorers-only backfill | **Done** | [phase-11-backfill](changes/phase-11-backfill.md) |
| 7 | Recent events honesty (hide placeholders) | **Done** | [phase-11-recent-events-honesty](changes/phase-11-recent-events-honesty.md) |
| 8 | Tests + review gate | **Done** | [phase-11-tests-gate](changes/phase-11-tests-gate.md) |

Full checklist: [Roadmap Phase 11](roadmap.md#phase-11).

### 📋 Phase 12 — Done

| # | Item | Status | Doc |
| --- | --- | --- | --- |
| 1 | Document Phase 12 plan | **Done** | [phase-12-planned](changes/phase-12-planned.md) |
| 2 | `simulate_wc2030.py` — cup, hosts+random, draw | **Done** | [phase-12-draw](changes/phase-12-draw.md) |
| 3 | Group fixtures + score simulation | **Done** | [phase-12-group-sim](changes/phase-12-group-sim.md) |
| 4 | Best thirds + R32→Final bracket | **Done** | [phase-12-bracket](changes/phase-12-bracket.md) |
| 5 | Smoke run + tests + review gate | **Done** | [phase-12-tests-gate](changes/phase-12-tests-gate.md) |

Full checklist: [Roadmap Phase 12](roadmap.md#phase-12).

### ✅ Phase 13 — Done

| # | Item | Status | Doc |
| --- | --- | --- | --- |
| 1 | Document Phase 13 plan | **Done** | [phase-13-planned](changes/phase-13-planned.md) |
| 2 | Company model + migration | **Done** | [phase-13-company-model](changes/phase-13-company-model.md) |
| 3 | Company API (create / join / admin) | **Done** | [phase-13-company-api](changes/phase-13-company-api.md) |
| 4 | Leaderboard company scope | **Done** | [phase-13-leaderboard-scope](changes/phase-13-leaderboard-scope.md) |
| 5 | SPA join + company board | **Done** | [phase-13-spa](changes/phase-13-spa.md) |
| 6 | Tests + review gate | **Done** (suite **438**; Bugbot clean) | [phase-13-tests-gate](changes/phase-13-tests-gate.md) |

Full checklist: [Roadmap Phase 13](roadmap.md#phase-13).

### ✅ Phase 14 — Deploy Readiness (Done)

| # | Item | Status | Doc |
| --- | --- | --- | --- |
| 1 | Push Phase 13 to origin | **Done** (`5fdd1c4`) | [phase-14-planned](changes/phase-14-planned.md) |
| 2 | Prod secrets inventory | **Done** | [phase-14-secrets](changes/phase-14-secrets.md) |
| 3 | CORS + JWT for production | **Done** | [phase-14-cors](changes/phase-14-cors.md) |
| 4 | Production Compose override | **Done** | [phase-14-compose](changes/phase-14-compose.md) |
| 5 | SPA production build / apiUrl | **Done** | [phase-14-spa-prod](changes/phase-14-spa-prod.md) |
| 6 | Migrate + smoke checklist | **Done** | [phase-14-migrate-smoke](changes/phase-14-migrate-smoke.md) |
| 7 | Review gate | **Done** (suite **462**) | [phase-14-gate](changes/phase-14-gate.md) |

Full checklist: [Roadmap Phase 14](roadmap.md#phase-14) · agent rules in [phase-14-planned](changes/phase-14-planned.md).

### Phase snapshot

| Phase | Name | Status |
| --- | --- | --- |
| 1 | [Foundation & Auth](roadmap.md#phase-1) | Done |
| 2 | [Tournament Setup APIs](roadmap.md#phase-2) | Done |
| 3 | [Match Lifecycle](roadmap.md#phase-3) | Done |
| 4 | [Betting Module](roadmap.md#phase-4) | Done |
| 5 | [Frontend SPA](roadmap.md#phase-5) | Done |
| 6 | [Quality & Ops](roadmap.md#phase-6) | Done |
| 7 | [Post-Launch / Live](roadmap.md#phase-7) | Done |
| 8 | [User & Admin Dashboards](roadmap.md#phase-8) | Done |
| 9 | [Ops & Tournament Close-Out](roadmap.md#phase-9) | Done |
| 10 | [Match Sync, Fixtures UX, Styling & Cleanup](roadmap.md#phase-10) | Done |
| 11 | [Real Goalscorers (FIFA Timeline)](roadmap.md#phase-11) | Done |
| 12 | [2030 World Cup Simulation](roadmap.md#phase-12) | Done |
| 13 | [Company-Scoped Competitions](roadmap.md#phase-13) | Done |
| 14 | [Deploy Readiness](roadmap.md#phase-14) | Done (suite **462**) |

### Quick Navigation

#### 🗺️ Completion Roadmap

14 phases with checklists reflecting the latest code review.

#### ⚙️ Dev Workflow

Setup, migrations, API development, and testing steps.

#### 🔌 API Status

All domain controllers wired — coverage matrix.

#### 🗄️ Data Model

Entity relationships and table inventory.

#### 📋 System Overview

Tech stack, folder structure, and architecture.

#### 🔍 Changes Review

Phases **1–14** Done. See [Changes Review](changes-review.md) · [phase-14-gate](changes/phase-14-gate.md).

### Recommended next work

#### Ship / ops

Commit and push Phase 14 working tree when ready (ask before commit/push). Deploy with [phase-14-secrets](changes/phase-14-secrets.md) · [phase-14-compose](changes/phase-14-compose.md) · [phase-14-migrate-smoke](changes/phase-14-migrate-smoke.md). Replace SPA `REPLACE_ME` before production `ng build`.

#### Maintenance / polish

Prefer ops hygiene (avoid accidental wipe re-import of WC 2026) — [Changes Review](changes-review.md) · [Phase 9 detail](changes/phase-9-ops-closeout.md).

### Local Dev URLs

| Service | URL | Status |
| --- | --- | --- |
| API (Swagger) | `http://localhost:5055/swagger` | Available |
| Frontend (Angular) | `http://localhost:4200` | Built |
| Health | `http://localhost:5055/health` | Available |
| PostgreSQL | User Secrets / Docker Compose | Configured |
| MCP Database Tools | Cursor MCP — `worldcup-database` | Complete |
| Docs (VitePress) | `npm run docs:dev` from workspace root | Available |
