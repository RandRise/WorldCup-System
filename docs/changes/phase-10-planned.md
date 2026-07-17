# WorldCup System — Phase 10 Planned Work

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md)

## Phase 10 — Match Sync, Fixtures UX, Visual Polish & Cleanup

**Status: In progress** — Tasks 1–3 **complete** (reviewed, tested); Task 4 cleanup not started. Detail: [phase-10-match-sync](phase-10-match-sync.md) · [phase-10-fixtures-ux](phase-10-fixtures-ux.md) · [phase-10-styling](phase-10-styling.md).

Opened **17 Jul 2026** after Phase 9 ops close-out planning. Checklist: [Roadmap Phase 10](../roadmap.md#phase-10).

### Goals (summary)

| # | Theme | Outcome | Status |
| --- | --- | --- | --- |
| 1 | Post-match external sync + bet resolve | After FT, pull official/result feed updates into our match model, then resolve open bets | **Done** — gate closed; [detail](phase-10-match-sync.md) |
| 2 | Fixtures betting UX | Users reach bettable matches without scrolling past long finished-history lists | **Done** — gate closed; [detail](phase-10-fixtures-ux.md) |
| 3 | Professional World Cup styling | Stronger brand atmosphere (palette, typography, backgrounds) across the SPA | **Done** — gate closed; [detail](phase-10-styling.md) |
| 4 | Project cleanup | Scan workspace/solution; remove duplicates and unused code/assets we will never use | Planned |

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

### 2 — Fixtures page: bettable matches first — **Done** (gate closed)

#### Problem

Fixtures previously sorted by date ascending. With many **finished** matches first, bettable matches sat at the bottom — heavy scroll on mobile and desktop.

#### Implemented UX

1. **Default = Action** — open (scheduled) + live; finished hidden from first scroll
2. **Filter chips:** Action · Open bets · Live · Finished · All
3. **Sections:** Open for betting → Live → Finished (collapsible; expanded on Finished/All)
4. **Jump to next bet** — scrolls to earliest `canBet` match
5. **Live poll** — clears `canBet` when status leaves `Scheduled`

Helpers + Jasmine: `fixture-sections.ts` / `.spec.ts`. Full detail: [phase-10-fixtures-ux](phase-10-fixtures-ux.md).

#### Touch points (implemented)

- `worldcup-client/.../fixtures/fixture-sections.ts` (+ spec)
- `worldcup-client/.../fixtures/fixtures.component.ts` / `.html` / `.scss`
- API `?status=` **not** added — client-side split is enough at current scale

---

### 3 — Professional World Cup visual styling — **Done** (gate closed)

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

#### Implemented

- Global tokens in `worldcup-client/src/styles.scss` — gold primary, success/danger/blue host accents
- CSS-only night-pitch atmosphere (radial blooms + pitch-line texture); fonts via Google Fonts (Bebas Neue + Manrope)
- Shell brand mark (emoji removed), glass topbar; home brand-first hero (no hero cards)
- Fixtures / leaderboard / bets / bracket / auth / dashboard accent polish
- Motions: `page-enter`, `live-pulse`, primary CTA `:active` scale; `prefers-reduced-motion` honored
- Post-Bugbot: pending bet pill (no pulse), amber warnings, admin finished = success green

Full detail: [phase-10-styling](phase-10-styling.md).

#### Assets

Google Fonts (Bebas Neue, Manrope) + original CSS backgrounds. No FIFA brand asset scraping. Documented in the phase change log.

#### Review gate (Task 3)

| Item | Severity | Status |
| --- | --- | --- |
| Bugbot highs/criticals | High | **Cleared** |
| Medium findings | Medium | **Fixed** (pending pill, amber warnings, reduced-motion, admin finished) |
| Jasmine + API regression | — | **Passed** — Jasmine **25** / 25; API **268** / 268 |

---

### 4 — Project cleanup (duplicates & unused)

#### Problem

The workspace has grown duplicates and leftovers. Example: a stale `worldcup-client` at the workspace root (old green theme) while the canonical SPA lives under `WorldCup-System/worldcup-client` — running `npm start` from the wrong folder hides Phase 10 UI work. Similar risk exists for mirrored docs and other unused trees.

#### Scope (when implementing)

1. **Inventory** — scan workspace + solution for duplicate projects, dead folders, unused packages, cancelled-feature leftovers, orphan scripts/assets, unused API/client surface
2. **Decide keep vs delete** — document each removal in a Task 4 detail page before deleting
3. **Remove safely** — delete only confirmed unused/duplicate paths; update README / workflow docs so the canonical client path is obvious
4. **Verify** — `dotnet test` + client Jasmine still pass; `ng serve` from the documented path

#### Known candidates (starting list — confirm before delete)

| Candidate | Why suspect |
| --- | --- |
| Workspace-root `worldcup-client/` | Stale duplicate of `WorldCup-System/worldcup-client` (missing Task 2–3) |
| `WorldCup-System/docs/` vs workspace `docs/` | Possible dual docs trees — keep one source of truth |
| Root `node_modules` / VitePress-only vs client deps | Confirm layout; avoid breaking docs tooling |
| Old DB dump / backup files at workspace root | Ops artifact — keep or archive outside repo |
| Cancelled Phase items leftovers | Code/docs for cancelled CSV seed, bulk stadium import, live external API stubs if any remain |

#### Explicit non-goals

- Refactoring working APIs “for cleanliness” without unused evidence
- Deleting git history or force-pushing
- Removing Phase 9 Final FT tooling still needed for close-out

---

### Phase 10 Roadmap Mapping

| Task id | Status | Notes |
| --- | --- | --- |
| `p10-match-sync` | **Done** | FIFA calendar + ExternalMatchId + Admin sync + tests; [detail](phase-10-match-sync.md) |
| RabbitMQ decision | **Done** | v1 without broker — documented and followed |
| `p10-fixtures-ux` | **Done** | Action default + chips + sections; Jasmine 12; [detail](phase-10-fixtures-ux.md) |
| `p10-wc-styling` | **Done** | WC-inspired palette, atmosphere, typography; Jasmine **25** + API **268**; [detail](phase-10-styling.md) |
| `p10-cleanup` | Planned | Scan + remove duplicates / unused; detail page when work starts |

### Explicit non-goals (until revisited)

- Replacing Admin live console for in-match event entry
- Full HTML website crawler as primary source
- RabbitMQ / MassTransit in the first delivery of sync
- Copying FIFA proprietary logos or scraping brand kits

### Related

- [Phase 10 Task 1 detail](phase-10-match-sync.md)
- [Phase 10 Task 2 detail](phase-10-fixtures-ux.md)
- [Phase 10 Task 3 detail](phase-10-styling.md)
- [Roadmap Phase 10](../roadmap.md#phase-10)
- [WC 2026 live data](wc2026-live-data.md) — current offline/manual path
- [Phase 7](phase-7-bugfixes.md) — cancelled broad external live API
- [Phase 4 betting](phase-4-betting.md) — resolve rules to reuse
