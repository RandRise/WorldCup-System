# WorldCup System — Phase 10 Task 3: Professional World Cup Styling

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 10 plan](phase-10-planned.md)

## Phase 10 Task 3 — Professional World Cup visual styling

Opened **17 Jul 2026** as implementation of [phase-10-planned](phase-10-planned.md) item 3 (`p10-wc-styling`). Checklist: [Roadmap Phase 10](../roadmap.md#phase-10).

> **Success:** **Task 3 complete (17 Jul 2026).** Black/white/gold theme tokens, night-pitch CSS atmosphere, Bebas Neue + Manrope typography, brand-first home, shell polish, motions. Review gate closed — Jasmine **25** + API **268** passed (CSS/HTML-only; no new logic tests).

### Review gate

> **Success:** **Gate closed (17 Jul 2026).** No high/critical Bugbot findings; medium findings fixed; tests green.

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Bugbot Phase 10 Task 3 review | High (gate) | **Cleared** | No high/critical findings |
| Pending bets pulsed as live | Medium | **Fixed** | My Bets uses `pending` pill (no pulse) |
| Warning alerts used gold branding | Medium | **Fixed** | Amber warning tokens (not trophy gold) |
| Incomplete `prefers-reduced-motion` | Medium | **Fixed** | Global reduced-motion disables animations/transitions |
| Admin finished pill still gold | Low | **Fixed** | Live console finished uses `--success` |
| Google Fonts CDN (privacy / offline / CSP) | Medium | Accepted v1 | Fail soft to system fallbacks |
| `color-mix` / `backdrop-filter` support | Medium | Accepted v1 | Modern browsers; readable without blur |
| Accessibility (gold-on-dark contrast) | Medium | Mitigated | CTAs: dark text on gold; accents `--primary-bright` |
| Jasmine / API regression | — | **Passed** | Jasmine **25** / 25; `dotnet test` **268** / 268 |
| FIFA / proprietary brand assets | — | N/A | Explicitly not used — CSS-only atmosphere |

### Delivered checklist

| Outcome | Status |
| --- | --- |
| Global CSS theme tokens (black / white / gold + host accents) | Done |
| Atmospheric body background (radial blooms + pitch-line texture) | Done |
| Typography: Bebas Neue display + Manrope UI | Done |
| Shell brand mark (no emoji) + sticky glass topbar + page enter | Done |
| Home: brand-first hero (no card clutter); destination links | Done |
| Fixtures / leaderboard / bets / bracket / auth / dashboard polish | Done |
| Motions: page enter, live pill pulse, primary CTA press | Done |
| `prefers-reduced-motion` respect | Done |
| FIFA / proprietary brand assets | **Not used** — CSS-only atmosphere |

### Git file list (Task 3)

From `WorldCup-System` repo (`git status` / `git diff` — uncommitted):

| Path | Change |
| --- | --- |
| `worldcup-client/src/styles.scss` | Theme tokens, atmosphere, global components, motions (**+228 / −~45** net) |
| `worldcup-client/src/index.html` | Font preconnect + Bebas Neue / Manrope stylesheet |
| `worldcup-client/.../shell/shell.component.html` | Brand text `WorldCup` (emoji removed) |
| `worldcup-client/.../shell/shell.component.scss` | Glass topbar, gold shimmer brand, page-enter, reduced-motion |
| `worldcup-client/.../home/home.component.html` | Brand-first hero; destinations nav (no hero cards) |
| `worldcup-client/.../home/home.component.scss` | Hero / brand-mark / destinations layout |
| `worldcup-client/.../fixtures/fixtures.component.scss` | Display titles, gold score/hover |
| `worldcup-client/.../leaderboard/leaderboard.component.scss` | Gold summary + display stats |
| `worldcup-client/.../bets/my-bets.component.scss` | Link accent → `--primary-bright` |
| `worldcup-client/.../bracket/bracket.component.scss` | Display round titles |
| `worldcup-client/.../dashboard/dashboard.component.scss` | Display stat values in gold |
| `worldcup-client/.../auth/login/login.component.scss` | Gold border + display title |
| `worldcup-client/.../auth/register/register.component.scss` | Same as login |

**Diffstat:** 13 files, **384 insertions / 125 deletions**.

### Design tokens (summary)

| Token | Role | Example |
| --- | --- | --- |
| `--bg` / `--bg-elevated` | Near-black night pitch | `#07090c` / `#0c1016` |
| `--surface` / `--surface-hover` | Elevated panels | `#121820` / `#182030` |
| `--primary` / `--primary-bright` | Trophy gold CTA / accents | `#c9a84c` / `#e8d48b` |
| `--border-gold` | Soft gold edges | `rgba(201, 168, 76, 0.45)` |
| `--success` / `--success-soft` | Finished / positive (host green) | `#2f9e6a` |
| `--danger` / `--danger-soft` | Live / errors (host red) | `#d64545` |
| `--accent-blue` / `--accent-blue-soft` | Scheduled pills (host blue) | `#3a6ea5` |
| `--font-display` / `--font-ui` | Bebas Neue / Manrope | — |
| `--page-max` | Content width | `1100px` |

Former green primary (`#3d9a5f`) is replaced by gold; success green is reserved for finished / positive alerts.

### Assets & sources

| Asset | Source |
| --- | --- |
| Fonts | Google Fonts — [Bebas Neue](https://fonts.google.com/specimen/Bebas+Neue), [Manrope](https://fonts.google.com/specimen/Manrope) |
| Background | Original CSS gradients + repeating pitch-line pattern in `styles.scss` |
| Logos / FIFA marks | None — brand wordmark “WorldCup” only |

### UX / visual behavior

1. **Atmosphere** — Fixed body background: gold bloom at top, soft green/blue washes, pitch-line `::before` texture; `app-root` above texture (`z-index: 1`).
2. **Brand** — Shell + home use display font with gold gradient shimmer; `prefers-reduced-motion` falls back to solid `--primary-bright`.
3. **Home** — Hero is not a `.card`; brand-mark is the hero signal; three destination links replace feature cards.
4. **Status pills** — Scheduled → blue; Live → red + pulse; Finished → green success (not gold).
5. **Primary buttons** — Gold gradient, dark text, light press scale; outline buttons use gold border on hover.

```text
index.html (fonts)
  → styles.scss (:root tokens + body atmosphere + globals)
  → shell (sticky glass topbar + brand shimmer + page-enter)
  → home (brand-first hero + destinations)
  → feature SCSS (display titles / gold accents)
```

### Diff highlights

#### Theme tokens (`styles.scss`)

```scss
:root {
  --bg: #07090c;
  --primary: #c9a84c;
  --primary-bright: #e8d48b;
  --font-display: 'Bebas Neue', 'Arial Narrow', sans-serif;
  --font-ui: 'Manrope', 'Segoe UI', sans-serif;
  /* … host accents, border-gold, page-max … */
}
```

#### Home hero (no cards)

```html
<section class="hero">
  <p class="brand-mark">WorldCup</p>
  <h1>Predict every match. Climb the pool.</h1>
  <p class="lede">…</p>
  <div class="hero-actions">…</div>
</section>
<nav class="destinations" aria-label="Quick links">…</nav>
```

#### Motions + reduced motion

- `@keyframes live-pulse` on `.status-pill.live`
- `@keyframes page-enter` on `.page`
- `@keyframes brand-shimmer` on shell / home brand marks
- `@media (prefers-reduced-motion: reduce)` disables animations and restores solid gold brand color

### Roadmap mapping

| Task id | Status | Notes |
| --- | --- | --- |
| `p10-wc-styling` | **Done in code** (gate pending) | This page — 13 client files, 384+/125− |
| `p10-match-sync` | **Done** | [phase-10-match-sync](phase-10-match-sync.md) |
| `p10-fixtures-ux` | **Done** | [phase-10-fixtures-ux](phase-10-fixtures-ux.md) |

### Explicit non-goals

- Copying FIFA proprietary logos, emblems, or official brand kits
- Photograph scraping from fifa.com
- Redesigning Admin live console interaction model (tokens only via globals)
- Self-hosting font files (CDN in v1; revisit if CSP/offline requires it)

### Related

- [Phase 10 plan](phase-10-planned.md)
- [Roadmap Phase 10](../roadmap.md#phase-10)
- [Fixtures UX](phase-10-fixtures-ux.md)
- [Match sync](phase-10-match-sync.md)
- [Changes Review](../changes-review.md)
