# WorldCup System — Phase 13 Task 5: SPA Company UX

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 13 plan](phase-13-planned.md) · [Task 3 API](phase-13-company-api.md) · [Task 4 scope](phase-13-leaderboard-scope.md) · [Task 6 tests](phase-13-tests-gate.md)

## Phase 13 Task 5 — SPA (`p13-spa`)

Opened **23 Jul 2026**. Checklist: [Roadmap Phase 13](../roadmap.md#phase-13).

> **Success:** **Task 5 interim gate closed (23 Jul 2026).** Join-company UX, company-scoped leaderboard empty-state, CompanyAdmin invite/members, Admin create. Bugbot high fixed: manage actions gated on JWT (`canManageCompany`), not DB-only `GetMine.isCompanyAdmin`, so Join token fail-soft no longer breaks reload. Phase 13 Done with suite **438** — [phase-13-tests-gate](phase-13-tests-gate.md).

### Review gate

High/critical items that must clear **before** interim testing (Jasmine / smoke) and before declaring Task 5 interim closed:

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Bugbot high/critical on SPA company UX | High (interim gate) | **Cleared** | [Bugbot](1940b9df-6b02-4762-9388-25ea07e937e3) high fixed (Join token miss → re-login); mediums fixed (Admin rotate `companyId`; applyAccessToken no clearSession) |
| Markdown docs review (this page + changes-review) | High (interim gate) | **Cleared** | Dual-docs mirror VitePress + git |
| Jasmine company/auth specs + smoke | High (interim gate) | **Cleared** | Focused Jasmine **10** SUCCESS; API suite **419** |
| Join applies CompanyAdmin JWT | High | **Cleared** | `applyAccessToken` when token present; else re-login prompt |
| Leaderboard without login | High | **Cleared** | `/leaderboard` uses `authGuard` |
| Leaderboard without company | High | **Cleared** | Empty CTA → `/company` (no global board) |
| Client trusts query `companyId` for board | Critical | **N/A** | Leaderboard still uses server company scope only; no client company id on bet API |
| Admin `getMembers(companyId)` for non-member Admin | Medium | Accepted v1 | Only when Admin and not CompanyAdmin; API still authorizes |
| Branding / custom themes | Info | Out of scope | Reuse existing tokens only |
| Full isolation / formal phase gate | Info | **Cleared** | [phase-13-tests-gate](phase-13-tests-gate.md) — suite **438** |

### Decisions locked this task

| Topic | Decision |
| --- | --- |
| Route | `/company` (auth required) — join + CompanyAdmin + Admin create |
| Nav | Authenticated users see **Company** link in shell |
| Leaderboard | Auth required; shows company name; no-company → join CTA (skips board API when no company) |
| Dashboard | Banner when no company; chip when joined; quick link to Company |
| Admin | Hub card → `/company`; create + GetAll on company page |
| Theme | Existing black/gold tokens; no new brand identity |
| JWT after Join | Persist `token` via `applyAccessToken` → `GetMe` so `CompanyAdmin` role appears immediately |

### Deliverables

| Item | Detail | Status |
| --- | --- | --- |
| Models | `Company`, `CompanyMine`, `CompanyMember`, join/create DTOs in `api.models.ts` | **Done** |
| API client | `CompanyApiService` — Mine / Join / Rotate / Members / Create / GetAll | **Done** |
| Auth | `isCompanyAdmin` + `applyAccessToken` | **Done** |
| Company page | Join form, invite rotate, members table, Admin create + list | **Done** |
| Leaderboard UX | Company name + empty join CTA; `authGuard` | **Done** |
| Dashboard UX | Join banner / company chip + Company quick link | **Done** |
| Jasmine | `company-api.service.spec.ts` (4) + `auth.service.company.spec.ts` (2) | **Done** (run pending) |
| Formal phase gate | Isolation + Bugbot + `dotnet test` | Task 6 — **tests cleared; Bugbot open** |

### Behavior

```text
Authenticated user → /company
  → GET Company/Mine
  → No company → join form (POST Company/Join)
       → if response.token → AuthService.applyAccessToken → GetMe
  → Has company → summary; CompanyAdmin/Admin → invite + rotate + members
  → Platform Admin → create form + GET Company/GetAll

Authenticated user → /leaderboard
  → GET Company/Mine first
  → No company → join CTA (skip GetLeaderboard)
  → Has company → GET Bet/GetLeaderboard + GetMySummary (server-scoped)

Anonymous → /leaderboard
  → authGuard → login
```

### Acceptance

- [x] Authenticated user with no company can join via invite code on `/company`.
- [x] First joiner receiving a new JWT refreshes session roles (`CompanyAdmin` via `applyAccessToken`).
- [x] Leaderboard redirects anonymous users to login; members without a company see join CTA.
- [x] CompanyAdmin sees invite code, can rotate, and list members.
- [x] Platform Admin can create companies and see invite codes on `/company`.
- [ ] Interim Bugbot + Jasmine smoke (gate still pending).
- [x] Task 6 isolation tests written + suite **419** (Bugbot formal still open).

### Git file list (Task 5)

Repo: `WorldCup-System` (branch `master`, uncommitted). Client under `worldcup-client/`.

**Working tree (23 Jul 2026):** `git status` shows **11 modified** client files (**+201 / −60**) plus **untracked** company feature + specs.

| Status | Path | Notes |
| --- | --- | --- |
| **New** | `worldcup-client/src/app/core/api/company-api.service.ts` | ~48 LOC |
| **New** | `worldcup-client/src/app/core/api/company-api.service.spec.ts` | 4 Jasmine cases (~82 LOC) |
| **New** | `worldcup-client/src/app/features/company/company.component.ts` | ~143 LOC |
| **New** | `worldcup-client/src/app/features/company/company.component.html` | ~141 LOC — join / admin / members |
| **New** | `worldcup-client/src/app/features/company/company.component.scss` | ~95 LOC — existing tokens |
| **New** | `worldcup-client/src/app/core/auth/auth.service.company.spec.ts` | 2 Jasmine cases (~48 LOC) |
| Modified | `worldcup-client/src/app/core/models/api.models.ts` | +39 company DTOs |
| Modified | `worldcup-client/src/app/core/auth/auth.service.ts` | +9 `isCompanyAdmin` / `applyAccessToken` |
| Modified | `worldcup-client/src/app/app.routes.ts` | `/company` + leaderboard `authGuard` |
| Modified | `worldcup-client/src/app/core/layout/shell/shell.component.html` | Company nav |
| Modified | `worldcup-client/src/app/features/leaderboard/leaderboard.component.{ts,html,scss}` | company empty-state |
| Modified | `worldcup-client/src/app/features/dashboard/dashboard.component.{ts,html,scss}` | banner / chip |
| Modified | `worldcup-client/src/app/features/admin/hub/admin-hub.component.html` | Companies card |
| **New** | `docs/changes/phase-13-spa.md` (this page) | docs |
| Modified | `docs/roadmap.md`, `docs/changes-review.md`, `docs/api-status.md`, `docs/changes/phase-13-planned.md`, `docs/index.md`, `docs/.vitepress/config.mts` | docs |

> **Note:** Working tree also shows Tasks 2–4 / Task 6 C# and test files. Those are **out of scope** for this page — document only Task 5 SPA client + docs above. Task 6 detail: [phase-13-tests-gate](phase-13-tests-gate.md).

### Diff snippets (git)

#### Routes — `/company` + leaderboard auth

```typescript
{ path: 'company', component: CompanyComponent, canActivate: [authGuard] },
{ path: 'leaderboard', component: LeaderboardComponent, canActivate: [authGuard] },
```

#### Auth — CompanyAdmin + Join token refresh

```typescript
readonly isCompanyAdmin = computed(() =>
  (this.currentUserSignal()?.roles ?? []).includes('CompanyAdmin'),
);

/** Persist a re-issued JWT (e.g. Join → CompanyAdmin) and refresh GetMe. */
async applyAccessToken(token: string): Promise<void> {
  localStorage.setItem(TOKEN_STORAGE_KEY, token);
  await this.initializeSession();
}
```

#### Models — company DTOs (excerpt)

```typescript
export interface Company {
  id: number;
  name: string;
  slug?: string | null;
  inviteCode?: string | null;
  createdAt: string;
  memberCount: number;
}

export interface JoinCompanyResponse {
  company: Company;
  becameCompanyAdmin: boolean;
  isCompanyAdmin: boolean;
  token?: string | null;
  expiration?: string | null;
}
```

#### API client — `CompanyApiService` (excerpt)

```typescript
getMine(): Promise<CompanyMine> {
  return firstValueFrom(this.http.get<CompanyMine>(`${this.baseUrl}/Company/Mine`));
}

join(request: JoinCompanyRequest): Promise<JoinCompanyResponse> {
  return firstValueFrom(
    this.http.post<JoinCompanyResponse>(`${this.baseUrl}/Company/Join`, request),
  );
}
```

#### Company page — Join applies token

```typescript
const result = await this.companyApi.join({
  inviteCode: this.joinForm.controls.inviteCode.value.trim(),
});

if (result.token) {
  await this.auth.applyAccessToken(result.token);
}
```

#### Leaderboard — mine first, then board

```typescript
const mine = await this.companyApi.getMine();
this.company.set(mine.company);

if (mine.company == null) {
  this.entries.set([]);
  this.summary.set(null);
  return;
}

const [rows, summary] = await Promise.all([
  this.betApi.getLeaderboard(scopedId),
  this.betApi.getMySummary(scopedId),
]);
```

#### Dashboard — join banner / chip

```html
@if (!company()) {
  <div class="join-banner">
    <div>
      <h2>Join your company pool</h2>
      <p>Enter an invite code to see company rankings and compete with coworkers.</p>
    </div>
    <a routerLink="/company" class="btn btn-primary">Join company</a>
  </div>
} @else {
  <p class="company-chip">Pool: <strong>{{ company()?.name }}</strong></p>
}
```

#### Shell + Admin hub

```html
@if (auth.isAuthenticated()) {
  <a routerLink="/company" routerLinkActive="active">Company</a>
}

<!-- Admin hub -->
<a routerLink="/company" class="ops-card">
  <h2>Companies</h2>
  <p>Create workplace pools, share invite codes, and list members.</p>
</a>
```

### Phase 13 Roadmap Mapping

| Roadmap item | Status | Detail |
| --- | --- | --- |
| Docs / planned backlog | **Done** | [phase-13-planned](phase-13-planned.md) |
| `p13-company-model` | **Done** | [phase-13-company-model](phase-13-company-model.md) |
| `p13-company-api` | **Done** | [phase-13-company-api](phase-13-company-api.md) |
| `p13-leaderboard-scope` | **Done** | [phase-13-leaderboard-scope](phase-13-leaderboard-scope.md) |
| `p13-spa` | **Done this task** (interim gate closed; Jasmine **10**; suite **419**) | This file |
| `p13-tests-gate` | **Tests cleared; Bugbot open** | [phase-13-tests-gate](phase-13-tests-gate.md) |

### Related

- [Roadmap Phase 13](../roadmap.md#phase-13)
- [Phase 13 plan](phase-13-planned.md)
- [Company API](phase-13-company-api.md)
- [Leaderboard scope](phase-13-leaderboard-scope.md)
- [Changes Review](../changes-review.md)
- Next: Task 5 interim Bugbot/Jasmine + Task 6 Bugbot formal → Phase 13 Done — [tests gate](phase-13-tests-gate.md)
