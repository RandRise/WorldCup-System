# WorldCup System — Phase 13 Planned Work

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md)

## Phase 13 — Company-Scoped Competitions

**Status: In progress** — Tasks 1–5 done; Task 6 **tests** cleared (suite **419**); Bugbot formal gate still open. Checklist: [Roadmap Phase 13](../roadmap.md#phase-13) · [tests gate](phase-13-tests-gate.md).

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
| 2 | `p13-company-model` | Data | `Company` entity, `User.CompanyId`, migration, repos | **Done** — [detail](phase-13-company-model.md) (interim Bugbot/docs gate; full gate Task 6) |
| 3 | `p13-company-api` | API | Create company (Admin), join by code, my company, rotate code (CompanyAdmin) | **Done** — [detail](phase-13-company-api.md) (interim Bugbot/docs gate; full gate Task 6) |
| 4 | `p13-leaderboard-scope` | Betting | Scope `LeaderboardService` + authorize `GetLeaderboard`; no cross-company leak | **Done** — [detail](phase-13-leaderboard-scope.md) (interim gate; full gate Task 6) |
| 5 | `p13-spa` | Client | Join company UI; company-scoped leaderboard; CompanyAdmin basics | **Done** — [detail](phase-13-spa.md) (interim Bugbot/Jasmine pending) |
| 6 | `p13-tests-gate` | Gate | Unit/integration isolation + `dotnet test` green; Bugbot/docs formal close | **Tests cleared** (suite **419**) — Bugbot formal **open** — [detail](phase-13-tests-gate.md) |

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

**User:** nullable `CompanyId` FK → `Company`. Users with null company: can browse tournament data and place bets; company leaderboard returns an **empty list** (SPA prompts join) — not HTTP 400.

**Roles:** seed `CompanyAdmin` at startup. **Bootstrap:** first successful join when the company has zero members becomes CompanyAdmin; platform Admin may also assign the role. Documented in [Task 2](phase-13-company-model.md); enforced in Task 3 Join.

**Migration:** EF migration `AddCompany`; update `ApplicationDbContext`, `RepositoryManager`, Identity role seed. Done — see [Task 2](phase-13-company-model.md). **Interim review:** Bugbot + docs running for Task 2; full phase gate remains Task 6.

---

### Task 3 — Company API (`p13-company-api`)

**Done** — see [phase-13-company-api](phase-13-company-api.md).

| Endpoint | Auth | Behavior |
| --- | --- | --- |
| `POST Company/Create` | Admin | Create company + generate invite code |
| `POST Company/Join` | JWT | Body: invite code → set `User.CompanyId` (reject if already in a company); first joiner → CompanyAdmin + JWT re-issue |
| `GET Company/Mine` | JWT | Current company summary + whether caller is CompanyAdmin (`Company: null` if none) |
| `POST Company/RotateInviteCode` | CompanyAdmin (or Admin) | New code; invalidate old |
| `GET Company/Members` | CompanyAdmin (or Admin) | Same-company members only |
| `GET Company/GetAll` | Admin | List all companies (ops/demo) |

---

### Task 4 — Leaderboard scope (`p13-leaderboard-scope`) — **Done**

Implemented — see [phase-13-leaderboard-scope](phase-13-leaderboard-scope.md).

- `LeaderboardService.GetLeaderboard(long? companyId, …)` — null company → empty; filter bets to same-company users.
- `GET Bet/GetLeaderboard` — `[Authorize]`; company from JWT → DB `User.CompanyId` (never client company id); optional `worldCupId` kept.
- `GetMySummary` rank uses company-scoped board (`Rank = null` when no company).
- Betting place/resolve unchanged; SPA empty-state / login prompt delivered in Task 5.
- Interim Bugbot/docs/test gate closed for Task 4; full phase gate remains Task 6 (**open**).

---

### Task 5 — SPA (`p13-spa`) — **Done**

Implemented — see [phase-13-spa](phase-13-spa.md).

- `/company` — join by invite; CompanyAdmin invite + rotate + members; Admin create + GetAll.
- Dashboard join banner / company chip; shell **Company** nav link.
- Leaderboard `authGuard`; company name header; no-company → join CTA.
- Reused existing WC styling tokens; no branding work.
- Interim Bugbot/docs/Jasmine gate pending; full phase gate remains Task 6.

---

### Task 6 — Tests & gate (`p13-tests-gate`) — **Open**

Formal phase gate still **open**. Tests portion may already exist — see [phase-13-tests-gate](phase-13-tests-gate.md).

- Unit: join validation, leaderboard filter by company, invite uniqueness — covered (+ bidirectional leak + invite non-reuse).
- Integration: two companies → leaderboard A never includes users from B — may be cleared (`LeaderboardIsolationIntegrationTests`).
- `dotnet test` suite may show **419** — does not close Phase 13 alone.
- Remaining: Bugbot + markdown formal review to mark Phase 13 Done.

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
| Company model + migration | Task 2 (`p13-company-model`) — **Done** · [detail](phase-13-company-model.md) |
| Company API | Task 3 (`p13-company-api`) — **Done** · [detail](phase-13-company-api.md) |
| Leaderboard isolation | Task 4 (`p13-leaderboard-scope`) — **Done** · [detail](phase-13-leaderboard-scope.md) |
| SPA join + board | Task 5 (`p13-spa`) — **Done** · [detail](phase-13-spa.md) (interim Bugbot/Jasmine pending) |
| Tests + review gate | Task 6 (`p13-tests-gate`) — **Open** · [detail](phase-13-tests-gate.md) (tests may exist; formal Bugbot/docs still open) |

### Related

- [Roadmap Phase 13](../roadmap.md#phase-13)
- [Phase 4 — Betting](phase-4-betting.md) — scoring / leaderboard origin
- [Data Model](../data-model.md) — `Company` + `User.CompanyId` (Task 2)
- [Changes Review](../changes-review.md)
