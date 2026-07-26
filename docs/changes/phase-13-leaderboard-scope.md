# WorldCup System — Phase 13 Task 4: Leaderboard Scope

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 13 plan](phase-13-planned.md)

## Phase 13 Task 4 — Leaderboard scope (`p13-leaderboard-scope`)

Opened **23 Jul 2026**. Checklist: [Roadmap Phase 13](../roadmap.md#phase-13).

> **Success:** **Task 4 implemented + interim gate closed (23 Jul 2026).** `GetLeaderboard` requires JWT; company scope from caller’s `User.CompanyId` (DB); null company → empty list; optional `worldCupId` kept; `GetMySummary` rank is company-scoped. Cross-company rows excluded. Bugbot found no bugs; **14** Task 4 cases; suite **399** passed. SPA join UX is Task 5. **Full phase gate** remains Task 6.

### Review gate

High/critical items that must clear **before** interim testing (`dotnet test`):

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Bugbot high/critical on leaderboard scope | High (interim gate) | **Cleared** | [Bugbot](dbc61316-b1d4-4847-ba22-c69de5937626) found no bugs |
| Markdown docs review (this page + changes-review) | High (interim gate) | **Cleared** | Dual-docs mirror VitePress + git |
| Leaderboard unit tests + `dotnet test` | High (interim gate) | **Cleared** | **14** Task 4 cases; suite **399** passed |
| Client-supplied `companyId` query accepted | Critical | **Cleared by design** | No `companyId` param; scope from JWT → `UserManager` → `CompanyId` |
| Unauthenticated `GetLeaderboard` still public | High | **Cleared** | `[Authorize]` on endpoint |
| Cross-company leak in rankings | High | **Cleared by design** | Filter bets to users with matching `CompanyId` |
| Null company returns 400 | Medium | **Cleared by design** | Empty list (SPA prompts join in Task 5) |
| Public SPA `/leaderboard` without login | Medium | Accepted until Task 5 | API 401 for anonymous; join UX in Task 5 |
| Company Create/Join not built yet | Info | **Cleared Task 3** | [phase-13-company-api](phase-13-company-api.md) |
| Full two-company integration leak test | Info | Deferred Task 6 | Unit isolation covered here |

### Decisions locked this task

| Topic | Decision |
| --- | --- |
| Auth | `GET Bet/GetLeaderboard` requires `[Authorize]` (breaking vs public Phase 4) |
| Scope source | Caller’s `User.CompanyId` from DB after JWT resolve — never query-string company id |
| Null company | Empty leaderboard list; `GetMySummary` still returns personal points with `Rank = null` |
| Optional filter | `worldCupId` unchanged (tournament filter inside company board) |
| Place/resolve bets | Unchanged — scoring still 3/0; only ranking **visibility** is tenant-scoped |
| Admin override | Not in v1 (no global board for Admin) |

### Deliverables

| Item | Detail | Status |
| --- | --- | --- |
| Interface | `ILeaderboardService.GetLeaderboard(long? companyId, int? worldCupId)` | **Done** |
| Service | Filter members by `CompanyId`; null → empty | **Done** |
| Summary rank | `GetMySummary` uses company-scoped board for `Rank` | **Done** |
| Controller | `[Authorize]` + load `CompanyId` via `UserManager.FindByIdAsync` | **Done** |
| Unit tests | Null company, cross-company exclusion, ranking, worldCup filter, controller wiring | **Done** — suite **399** |
| Company HTTP API | Create / join / mine / rotate / members | **Done Task 3** — [phase-13-company-api](phase-13-company-api.md) |
| SPA join + board UX | Auth guard / empty-state prompt | **Not this task** — Task 5 |
| Formal phase gate | Isolation integration + Bugbot/docs + suite | Task 6 |

### Behavior

```text
Authenticated user
  → Resolve userId from JWT
  → Load User.CompanyId from DB
  → If null → []
  → Else rank only bets from users with same CompanyId
  → Optional worldCupId still filters matches
```

### Acceptance

- [x] Unauthenticated `GetLeaderboard` → 401.
- [x] Authenticated user in company A never sees company B rows (unit).
- [x] Authenticated user with null `CompanyId` → `[]` (200).
- [x] Optional `worldCupId` still works inside the company board.
- [x] PlaceBet / ResolveBets unchanged.
- [x] Interim Bugbot + docs + `dotnet test` (gate closed — suite **399**).

### Git file list (Task 4 — from `git status` / `git diff`)

Repo: `WorldCup-System` (branch `master`, uncommitted). Diff slice: **+200 / −30** across 5 code/test files.

| Status | Path | Diff |
| --- | --- | --- |
| Modified | `Core/Services/Bets/ILeaderboardService.cs` | +6 / −1 |
| Modified | `Core/Services/Bets/LeaderboardService.cs` | +25 / −8 |
| Modified | `WorldCup-System/Controllers/BetController.cs` | +22 / −2 |
| Modified | `WorldCup-System.Tests/Services/Bets/LeaderboardServiceTests.cs` | +98 / −14 |
| Modified | `WorldCup-System.Tests/Controllers/BetControllerTests.cs` | +49 / −5 |
| **New** | `docs/changes/phase-13-leaderboard-scope.md` (this page) | docs |
| Modified | `docs/roadmap.md`, `docs/changes-review.md`, `docs/api-status.md`, `docs/changes/phase-13-planned.md`, `docs/index.md`, `docs/overview.md`, `docs/.vitepress/config.mts` | docs |

### Diff snippets (git)

#### `ILeaderboardService` — company-scoped signature

```csharp
List<LeaderboardEntryDTO> GetLeaderboard(long? companyId, int? worldCupId = null);
```

#### `LeaderboardService` — null company + member filter

```csharp
if (!companyId.HasValue)
{
    return new List<LeaderboardEntryDTO>();
}

List<User> companyUsers = _repository.User
    .Find(existingUser => existingUser.CompanyId == companyId.Value)
    .ToList();
```

#### `BetController.GetLeaderboard` — authorize + DB scope

```csharp
[Authorize]
[HttpGet]
public async Task<IActionResult> GetLeaderboard([FromQuery] int? worldCupId = null)
{
    long userId = await ResolveCurrentUserIdAsync();
    User? user = await _userManager.FindByIdAsync(userId.ToString());
    List<LeaderboardEntryDTO> entries =
        _leaderboardService.GetLeaderboard(user.CompanyId, worldCupId);
    return Ok(entries);
}
```

### Roadmap mapping

| Roadmap item | Status | This doc |
| --- | --- | --- |
| Docs / planned backlog | **Done** | [phase-13-planned](phase-13-planned.md) |
| `p13-company-model` | **Done** | [phase-13-company-model](phase-13-company-model.md) |
| `p13-company-api` | **Done** | [phase-13-company-api](phase-13-company-api.md) |
| `p13-leaderboard-scope` | **Done** (interim gate closed; suite **399**) | This file |
| `p13-spa` | Planned | Task 5 |
| `p13-tests-gate` | Planned | Task 6 (full phase gate) |

### Related

- [Phase 13 plan](phase-13-planned.md)
- [Task 2 — Company model](phase-13-company-model.md)
- [Task 3 — Company API](phase-13-company-api.md)
- [Phase 4 — Betting](phase-4-betting.md)
- [Changes Review](../changes-review.md)
- Next: Task 5 SPA (`p13-spa`) · full phase gate Task 6
