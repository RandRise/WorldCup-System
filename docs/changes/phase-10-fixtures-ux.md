# WorldCup System — Phase 10 Task 2: Fixtures Betting UX

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 10 plan](phase-10-planned.md)

## Phase 10 Task 2 — Fixtures page: bettable matches first

Opened **17 Jul 2026** as implementation of [phase-10-planned](phase-10-planned.md) item 2. Checklist: [Roadmap Phase 10](../roadmap.md#phase-10).

> **Success:** **Task 2 complete (17 Jul 2026).** Client-side Action default, filter chips, Open → Live → Finished sections, collapsible finished, jump-to-next-bet. Review gate closed — Jasmine **12** passed; API `dotnet test` **268** passed (client-only task; no new C# tests).

### Review gate

> **Success:** **Gate closed (17 Jul 2026).** No high/critical Bugbot findings; two medium jump-CTA issues fixed; tests green.

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Bugbot Phase 10 Task 2 review | High (gate) | **Cleared** | No high/critical findings |
| Live poll leaves stale `canBet` after kickoff | High | **Fixed** | Snapshot merge sets `canBet: false` when status ≠ `Scheduled` |
| Jump bet no-ops on Live filter | Medium | **Fixed** | `scrollToNextBet` switches Live/Finished → Action before scroll |
| Scroll-after-filter race | Medium | **Fixed** | `setTimeout` after filter change so Open section is in DOM |
| Unknown `Match.status` values bucket to Open | Medium | Accepted for v1 | Only `Live` / `Finished` are special-cased; others → open |
| Server `?status=` filter | — | N/A | Explicit non-goal; client split sufficient |
| Jasmine `fixture-sections` specs | Medium (gate) | **Passed** | **12** / 12 |
| Backend `dotnet test` regression | — | **Passed** | **268** / 268 (no C# changes) |


### Delivered checklist

| Outcome | Status |
| --- | --- |
| Default **Action** view (open + live; hide finished) | Done |
| Filter chips: Action · Open bets · Live · Finished · All | Done |
| Sections: Open for betting → Live → Finished | Done |
| Finished collapsible (collapsed on Action/Open/Live; expanded on Finished/All) | Done |
| Jump to next bettable match | Done |
| Live snapshot clears `canBet` when status leaves Scheduled | Done |
| Pure helpers + Jasmine unit tests (`fixture-sections`) | Done |
| Backend API `?status=` | **Not needed** — client-side |

### Git file list (Task 2 working tree)

From `WorldCup-System` repo (`git status` / diff):

| Path | Change |
| --- | --- |
| `worldcup-client/.../fixtures/fixture-sections.ts` | **New** — group / filter / next-bet helpers |
| `worldcup-client/.../fixtures/fixture-sections.spec.ts` | **New** — Jasmine coverage |
| `worldcup-client/.../fixtures/fixtures.component.ts` | Modified — filter state, computeds, snapshot `canBet` |
| `worldcup-client/.../fixtures/fixtures.component.html` | Modified — toolbar, sections, shared card template |
| `worldcup-client/.../fixtures/fixtures.component.scss` | Modified — toolbar + section toggle styles |
| `docs/roadmap.md` | Modified — Task 2 checked; gate pending |
| `docs/changes/phase-10-planned.md` | Modified — Task 2 status |
| `docs/changes/phase-10-fixtures-ux.md` | **New** — this detail page |

Tracked diffstat (excluding untracked helpers): **~248 insertions / ~57 deletions** across 5 files; plus ~191 lines in new `fixture-sections*.ts`.

### UX behavior

1. **Default filter = Action** — scheduled/open and live only; finished history stays out of the first scroll.
2. **All** — same section order; finished starts expanded.
3. **Finished** chip — finished list only, expanded.
4. **Open bets** / **Live** — single bucket; finished collapsed when switching away from Finished/All.
5. **Jump to next bet** — earliest `canBet` match by kickoff; if filter is Live or Finished, switches to Action first, then scrolls to `#fixture-{id}` after render.

Classification uses match `status` (`Scheduled` → open, `Live`, `Finished`). Within open, `canBet: true` sorts ahead of TBD/non-bettable scheduled rows; then kickoff ascending (`date` localeCompare).

```text
matches[]
  → groupFixtures()     → { open, live, finished }  (sorted)
  → filterFixtureSections(activeFilter)
  → template sections + optional finished collapse
```

### Helpers (`fixture-sections.ts`)

| Export | Role |
| --- | --- |
| `FixtureFilter` / `FIXTURE_FILTER_OPTIONS` | Chip ids + labels |
| `fixtureBucket` | `Live` → live, `Finished` → finished, else open |
| `compareFixtures` | `canBet` first, then `date` ascending |
| `groupFixtures` | Split + sort into three sections |
| `filterFixtureSections` | Apply Action / Open / Live / Finished / All |
| `nextBettableMatch` | First `canBet` after sort, or `null` |

### Component wiring

- Signals: `activeFilter` (default `'action'`), `finishedExpanded` (default `false`).
- Computeds: `sections`, `nextBet`, `hasVisibleFixtures`.
- `setFilter` expands finished for `finished` / `all`; collapses for `action` / `open` / `live`.
- Shared `#fixtureCard` `ng-template` keeps card markup DRY across sections.
- Empty filter view: “No matches in this view. Try another filter.”

### Live snapshot `canBet` sync

```ts
canBet: live.status === 'Scheduled' ? match.canBet : false,
```

Prevents bet buttons lingering after a match goes Live/Finished via the 30s poll.

### Explicit non-goals

- Server-side fixture filtering (`?status=open`)
- Phase 10 Task 3 visual branding — **done in code** (gate pending); see [phase-10-styling](phase-10-styling.md)
- Changing list API payloads or pagination

### Diff highlights

#### Filter + sections (component)

```ts
protected readonly activeFilter = signal<FixtureFilter>('action');
protected readonly sections = computed(() =>
  filterFixtureSections(groupFixtures(this.matches()), this.activeFilter()),
);
protected readonly nextBet = computed(() => nextBettableMatch(this.matches()));
```

#### Action filter (helper)

```ts
case 'action':
  return { open: sections.open, live: sections.live, finished: [] };
```

#### Template structure

- Toolbar: tablist chips + optional “Jump to next bet”
- Sections: Open for betting → Live → Finished (toggle + count + chevron)
- Card: `id="fixture-{match.id}"` for scroll targets; `scroll-margin-top` in SCSS

### Tests (`fixture-sections.spec.ts`)

Covers: status bucketing, Open→Live→Finished grouping/order, bettable-before-TBD sort, Action hides finished, single-bucket filters, `nextBettableMatch` pick / null.

### Phase 10 roadmap mapping

| Task id | Status | Notes |
| --- | --- | --- |
| `p10-match-sync` | **Done** | Gate closed; [phase-10-match-sync](phase-10-match-sync.md) |
| RabbitMQ decision | **Done** | No broker in v1 |
| `p10-fixtures-ux` | **Done in code** | Gate **pending**; this page |
| `p10-wc-styling` | **Done in code** (gate pending) | [phase-10-styling](phase-10-styling.md) |

### Related

- [Phase 10 planned backlog](phase-10-planned.md)
- [Phase 10 Task 1 sync](phase-10-match-sync.md)
- [Changes Review](../changes-review.md)
- [Roadmap Phase 10](../roadmap.md#phase-10)
