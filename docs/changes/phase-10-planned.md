# WorldCup System — Phase 10 Planned Work

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md)

## Phase 10 — Match Sync, Fixtures UX & Visual Polish

**Status: In progress** — Task 1 (post-match sync) **complete** (reviewed, tested, committed); Tasks 2–3 not started. Implementation detail: [phase-10-match-sync](phase-10-match-sync.md).

Opened **17 Jul 2026** after Phase 9 ops close-out planning. Checklist: [Roadmap Phase 10](../roadmap.md#phase-10).

### Goals (summary)

| # | Theme | Outcome | Status |
| --- | --- | --- | --- |
| 1 | Post-match external sync + bet resolve | After FT, pull official/result feed updates into our match model, then resolve open bets | **Done** — gate closed; [detail](phase-10-match-sync.md) |
| 2 | Fixtures betting UX | Users reach bettable matches without scrolling past long finished-history lists | Planned |
| 3 | Professional World Cup styling | Stronger brand atmosphere (palette, typography, backgrounds) across the SPA | Planned |

---

### 1 — Post-match result sync & bet resolution

#### Problem

Today, finished scores arrive via **offline import scripts** or the **Admin live console**. Phase 7 cancelled a general “external football API” for live play-by-play. We still want a **post-match** path: when a match is finished (or we believe it is), call something that fetches the authoritative result and then runs the existing bet-resolution pipeline.

#### Recommended approach (decision) — implemented

**Prefer a licensed/structured HTTP data source over HTML scraping.** v1 uses **FIFA’s public calendar JSON** (`api.fifa.com` calendar/matches). Scraping FIFA’s website remains a last resort.

**Pipeline (implemented):**

```text
Trigger (Admin API — SyncResult / SyncFinishedResults)
    → FifaCalendarMatchResultProvider.fetch(ExternalMatchId)
    → Map to our Match (score / FT status)
    → Idempotent Goal apply via MatchResultSyncService
    → Existing ResolveBetsForMatch + TryAdvanceFromMatch
```

**API surface (implemented):**

- `POST /api/Match/SyncResult/{matchId}` — Admin-only; pulls external result for one match and applies it
- `POST /api/Match/SyncFinishedResults?worldCupId=` — batch sync for matches with `ExternalMatchId` in that World Cup
- `POST /api/Match/SetExternalMatchId` — store FIFA `IdMatch` ↔ our `Match.Id`
- Column `Match.ExternalMatchId` (unique when set) — migration `AddMatchExternalMatchId`

Reuse existing bet resolve (`BetService.ResolveBetsForMatch`) rather than inventing a second scoring path. Full detail: [phase-10-match-sync](phase-10-match-sync.md).

#### RabbitMQ — use or not?

**Decision for v1: do not introduce RabbitMQ.** — **Documented and done** (no broker wired).

| Factor | Without RabbitMQ | With RabbitMQ |
| --- | --- | --- |
| Current scale | One tournament, admin/scheduled sync | Same |
| Ops cost | None new | Broker in Docker Compose, connection strings, retries topology |
| Failure handling | Retry in hosted service / Polly | Built-in queues + DLQ |
| Decoupling crawl → resolve | In-process after persist | Useful if workers scale separately |

For this app, an **Admin-triggered sync** that fetches → updates DB → resolves bets in one flow is enough. Revisit RabbitMQ (or MassTransit) **only if** we add continuous multi-match live crawling, need durable retries across process restarts, or multiple consumers (notify, standings rebuild, etc.).

#### Open decisions (when starting implementation)

- [x] Choose data provider (FIFA calendar JSON vs paid third-party) and document ToS/key storage — FIFA calendar; season/competition ids in `MatchResultSync` config
- [x] Mapping table or config for external match IDs — `Match.ExternalMatchId` + Admin SetExternalMatchId
- [x] Idempotency: re-sync must not double-create goals or double-award points — count compare; resolve reuses existing path
- [x] Whether sync also writes cards/stats or score-only for betting (1X2) — **score-only** for v1

#### Relationship to Phase 7

Phase 7 item `p7-external-api` was **cancelled** for live in-match feeds. Phase 10 item 1 is a **narrower revisit**: post-match / FT sync + resolve, not a full live replacement of the Admin console.

---

### 2 — Fixtures page: bettable matches first

#### Problem

Fixtures currently sort by date ascending (`fixtures.component.ts`). With many **finished** matches first, the matches the user still needs to bet on sit at the bottom — heavy scroll on mobile and desktop.

#### Recommended UX (pick one primary + optional filters)

1. **Default sort / sections (preferred):**
   - **Open for betting** (scheduled, kickoff in future, no FT) — top
   - **Live** — next
   - **Finished** — collapsed or below, or behind a filter
2. **Filter chips:** `All` · `Open bets` · `Live` · `Finished` (default = `Open bets` or a combined “Action” view)
3. **Deep link / scroll:** optional “Jump to next match to bet” when some open fixtures remain

Keep finished results available for history; do not delete them — just de-prioritize them in the default view.

#### Touch points (when implementing)

- `worldcup-client/.../fixtures/fixtures.component.ts` (+ template/styles)
- Possibly API query params later (`?status=open`) if payloads get large; client-side split is enough at current scale

---

### 3 — Professional World Cup visual styling

#### Problem

SPA styling is functional but not strongly branded as a World Cup product. Need a more professional look and a **fitting atmospheric background**.

#### Design direction (research notes, 17 Jul 2026)

Inspired by FIFA World Cup 2026 visual system (not copying proprietary logos/marks verbatim):

- **Core palette:** black, white, and **gold** (trophy / institutional) rather than generic purple gradients
- **Host accents (optional, sparingly):** US blue, Mexico green, Canada red as secondary accents — not all at once
- **Atmosphere:** full-bleed or soft stadium/night-pitch imagery, subtle light bloom, depth — not flat single-color pages
- **Typography:** bold, graphic display + clear UI sans (avoid default Inter/Roboto-only look)
- **Motion:** 2–3 intentional transitions (page enter, live pulse, bet confirm) — not noise

Respect existing frontend design rules in the project: one composition per viewport where relevant, brand-forward, no card clutter in heroes, avoid purple-on-white / cream-serif clichés.

#### Scope (when implementing)

- Global theme tokens (CSS variables) in Angular styles
- Shell / nav / fixtures / bets / leaderboard / dashboards consistency
- Background treatment for main public pages (fixtures + betting first)
- Accessibility: contrast WCAG-minded with gold-on-dark

#### Assets

Prefer licensed or original photography / abstract pitch textures; avoid scraping FIFA brand assets. Document asset sources in the phase change log when work starts.

---

### Phase 10 Roadmap Mapping

| Task id | Status | Notes |
| --- | --- | --- |
| `p10-match-sync` | **Done** | FIFA calendar + ExternalMatchId + Admin sync + tests; [detail](phase-10-match-sync.md) |
| RabbitMQ decision | **Done** | v1 without broker — documented and followed |
| `p10-fixtures-ux` | Planned | Prioritize open/live over finished on fixtures |
| `p10-wc-styling` | Planned | WC-inspired palette, atmosphere, typography |

### Explicit non-goals (until revisited)

- Replacing Admin live console for in-match event entry
- Full HTML website crawler as primary source
- RabbitMQ / MassTransit in the first delivery of sync

### Related

- [Phase 10 Task 1 detail](phase-10-match-sync.md)
- [Roadmap Phase 10](../roadmap.md#phase-10)
- [WC 2026 live data](wc2026-live-data.md) — current offline/manual path
- [Phase 7](phase-7-bugfixes.md) — cancelled broad external live API
- [Phase 4 betting](phase-4-betting.md) — resolve rules to reuse
