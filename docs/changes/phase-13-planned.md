# WorldCup System — Phase 13 Planned Work

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md)

## Phase 13 — Company-Scoped Competitions

**Status: Planned** — docs only **21 Jul 2026**. Checklist: [Roadmap Phase 13](../roadmap.md#phase-13).

Soft multi-tenancy so workplaces can run private World Cup prediction pools on one deployment: **shared tournament data**, **isolated people / rankings**. Portfolio-friendly scope — no branding, billing, or per-company hosting.

### Why

Today the app is a **single shared tenant**: bets and `GetLeaderboard` rank all users (optionally by World Cup). For workplace fun, Company A must not see Company B’s members or scores. Tournament fixtures/scores stay global.

### Locked decisions (v1)

| Choice | Decision |
| --- | --- |
| Tenancy model | **Soft multi-tenancy** — one DB, one API; scope by `CompanyId` |
| Join path | **Invite code** (human-readable, unique per company) |
| Membership | **One company per user** in v1 (`User.CompanyId` nullable FK) |
| Roles | Keep `Admin` / `User`; add **`CompanyAdmin`** for invite regen + member list |
| Shared data | WorldCup, matches, goals, standings, bracket — unchanged, visible to all authenticated users |
| Private data | Company membership, company leaderboard, company member list |
| Leaderboard | Auth required; filter by caller’s company (+ optional `worldCupId`); never trust client-supplied `companyId` alone |
| Branding / subdomains | **Out of scope** — no logos, custom themes, or `acme.app` hosts in v1 |
| Email-domain auto-join | **Out of scope** — messy with personal email; invite code only |
| Multi-company membership | **Out of scope** for v1 (revisit later if needed) |

### Architecture

```text
Platform Admin creates Company (name + invite code)
  → User registers / logs in
  → User joins via invite code → User.CompanyId set
  → Place bets as today (Match still global)
  → GetLeaderboard aggregates BetResult only for users in same Company
  → CompanyAdmin can list members / rotate invite code
```

**Isolation rule:** authorization uses membership from JWT user → DB (`User.CompanyId` or claim refreshed at login), not a raw query-string company id from the client.

### Goals (summary)

| # | Task id | Theme | Outcome | Status |
| --- | --- | --- | --- | --- |
| 1 | docs | Docs | Roadmap Phase 13 + this planned doc | **Done** (this file) |
| 2 | `p13-company-model` | Data | `Company` entity, `User.CompanyId`, migration, repos | Planned |
| 3 | `p13-company-api` | API | Create company (Admin), join by code, my company, rotate code (CompanyAdmin) | Planned |
| 4 | `p13-leaderboard-scope` | Betting | Scope `LeaderboardService` + authorize `GetLeaderboard`; no cross-company leak | Planned |
| 5 | `p13-spa` | Client | Join company UI; company-scoped leaderboard; CompanyAdmin basics | Planned |
| 6 | `p13-tests-gate` | Gate | Unit/integration tests + Bugbot/docs review + `dotnet test` green | Planned |

---

### Task 2 — Company model (`p13-company-model`)

**Entity (sketch):**

| Field | Notes |
| --- | --- |
| `Id` | `long` PK |
| `Name` | Display name (required, length-capped) |
| `Slug` | Optional unique URL-safe key for future use; not required for routing in v1 |
| `InviteCode` | Unique, case-insensitive; regenerateable |
| `CreatedAt` | UTC |
| `CreatedByUserId` | Optional audit |

**User:** nullable `CompanyId` FK → `Companies`. Users with null company: can browse tournament data and place bets, but leaderboard returns empty / “join a company” (decide at implement: empty list vs 400).

**Roles:** seed `CompanyAdmin`. First joiner of a new company may become CompanyAdmin, **or** only platform Admin assigns CompanyAdmin — pick one at implement and document.

**Migration:** EF migration; update `ApplicationDbContext`, `RepositoryManager`, Identity role seed.

---

### Task 3 — Company API (`p13-company-api`)

Suggested surface (names flexible):

| Endpoint | Auth | Behavior |
| --- | --- | --- |
| `POST Company/Create` | Admin | Create company + generate invite code |
| `POST Company/Join` | User | Body: invite code → set `User.CompanyId` (reject if already in another company in v1) |
| `GET Company/Mine` | User | Current company summary + whether caller is CompanyAdmin |
| `POST Company/RotateInviteCode` | CompanyAdmin (or Admin) | New code; invalidate old |
| `GET Company/Members` | CompanyAdmin (or Admin) | Same-company members only |

Platform `Admin` may list all companies (optional `GET Company/GetAll`) for ops/demo seeding.

---

### Task 4 — Leaderboard scope (`p13-leaderboard-scope`)

- Change `LeaderboardService` to require a company scope derived from the current user (or explicit Admin override later — not required in v1).
- `GET Bet/GetLeaderboard` — add `[Authorize]`; filter to callers in the same company; keep optional `worldCupId`.
- Regression: unauthenticated or cross-company access must not return other companies’ rows.
- Betting place/resolve rules unchanged (scoring still 3/0); only **visibility** of rankings (and member lists) is tenant-scoped.

---

### Task 5 — SPA (`p13-spa`)

- After login / dashboard: if no company → prompt to enter invite code.
- Leaderboard page: show company name; only same-company ranks.
- Lightweight CompanyAdmin: show invite code + rotate + member list (no design system / branding work).
- Reuse existing WC styling tokens; no new brand identity.

---

### Task 6 — Tests & gate (`p13-tests-gate`)

- Unit: join validation, leaderboard filter by company, invite uniqueness.
- Integration: two companies → leaderboard A never includes users from B.
- Phase gate: Bugbot + markdown changes doc, then `dotnet test` on `WorldCup-System.Tests` (no `dotnet build` unless consented).

### Explicit non-goals (v1)

- Subdomains, custom domains, white-label branding / logos
- Billing, seats, SSO, email-domain auto-join
- Separate databases or deployments per company
- Private tournaments (per-company match graphs)
- Intra-company “Marketing vs Engineering” teams (nice-to-have later)
- Showing colleagues’ live picks before kickoff (privacy policy later)

### Risks

- **Existing users** with no company — need clear UX and leaderboard behavior.
- **Public leaderboard today** — flipping to auth + scope is a breaking API change; document in api-status when implementing.
- **CompanyAdmin assignment** — avoid orphan companies with no admin; define bootstrap rule in Task 2/3.
- **JWT claims** — if company id is in the token, refresh or re-issue after Join so authorization stays consistent.

### Demo / portfolio story

1. Seed two companies (e.g. Acme, Globex) with distinct invite codes.
2. Two users join different companies, place bets on the same World Cup matches.
3. Each sees only their company’s leaderboard.

README blurb (when implemented): workplace prediction pools; tournament shared, rankings isolated.

### Phase 13 Roadmap Mapping

| Roadmap item | This doc |
| --- | --- |
| Docs / planned backlog | This file |
| Company model + migration | Task 2 (`p13-company-model`) |
| Company API | Task 3 (`p13-company-api`) |
| Leaderboard isolation | Task 4 (`p13-leaderboard-scope`) |
| SPA join + board | Task 5 (`p13-spa`) |
| Tests + review gate | Task 6 (`p13-tests-gate`) |

### Related

- [Roadmap Phase 13](../roadmap.md#phase-13)
- [Phase 4 — Betting](phase-4-betting.md) — scoring / leaderboard origin
- [Data Model](../data-model.md) — planned `Companies` row when Task 2 lands
- [Changes Review](../changes-review.md)
