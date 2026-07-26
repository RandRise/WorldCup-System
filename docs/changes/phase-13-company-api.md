# WorldCup System — Phase 13 Task 3: Company API

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 13 plan](phase-13-planned.md) · [Task 2](phase-13-company-model.md) · [Task 4](phase-13-leaderboard-scope.md)

## Phase 13 Task 3 — Company API (`p13-company-api`)

Opened **23 Jul 2026**. Checklist: [Roadmap Phase 13](../roadmap.md#phase-13).

> **Success:** **Task 3 implemented + interim gate closed (23 Jul 2026).** `CompanyController` + `CompanyService` — Create / Join / Mine / RotateInviteCode / Members / GetAll. Invite codes uppercase; first joiner → `CompanyAdmin` (role before membership); Join JWT flat like Login. Bugbot highs fixed; **+15** CompanyService tests; suite **414** passed. SPA remains Task 5. **Full phase gate** remains Task 6.

### Review gate

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Bugbot high/critical findings on Task 3 API | High (interim gate) | **Cleared** | [Bugbot](9242f801-cb75-4d1f-86e9-1f09f037cec8) highs fixed (role-before-join; Join token fail-soft + flat shape; orphan CompanyAdmin strip) |
| Markdown docs review (this page + changes-review) | High (interim gate) | **Cleared** | Dual-docs mirror VitePress + git |
| Task 3 unit tests + `dotnet test` | High (interim gate) | **Cleared** | **15** CompanyService tests; suite **414** passed |
| Cross-company member / invite leak | High | **Mitigated in service** | Membership from DB user; CompanyAdmin cannot override to other company |
| Invite uniqueness / uppercase normalize | High | **Done this task** | Create/Join/Rotate store + compare uppercase |
| CompanyAdmin first-join bootstrap | High | **Done this task** | Zero members → `AddToRoleAsync` **before** `CompanyId`; JWT re-issue |
| JWT re-issue on Join without CompanyAdmin | High | **Cleared by design** | Flat `token`/`expiration` only when `BecameCompanyAdmin`; build failure does not fail Join |
| `UnauthorizedAccessException` → 403 | High | **Done this task** | `ApiErrorHelper` maps to 403 |
| Leaderboard company scope | Info | **Done Task 4** | [phase-13-leaderboard-scope](phase-13-leaderboard-scope.md) |
| SPA join UI | Info | Expected | Task 5 |
| Full isolation / leak tests | Info | Deferred Task 6 | Two-company leaderboard regression |
| Concurrent first-join race (two CompanyAdmins) | Medium | Accepted v1 | Rare; revisit with transaction if needed |

### Decisions locked this task

| Topic | Decision |
| --- | --- |
| Create | Platform `Admin` only; generates unique 8-char invite (no ambiguous chars `I/O/0/1`) |
| Join | Any authenticated user; reject if already in a company (no leave in v1) |
| First joiner | Becomes `CompanyAdmin`; response includes fresh JWT when role granted |
| Mine | 200 with `Company: null` when not joined (SPA prompt); invite code only for CompanyAdmin/Admin |
| Rotate / Members | `CompanyAdmin` → own company; platform `Admin` may pass `companyId` |
| GetAll | Platform `Admin` only (ops / demo seed) |
| Isolation | Never trust client `companyId` alone for CompanyAdmin; Admin override explicit |

### Endpoints

| Method | Route | Auth | Behavior |
| --- | --- | --- | --- |
| `POST` | `Company/Create` | Admin | Body: name + optional slug → company + invite |
| `POST` | `Company/Join` | JWT | Body: invite code → set `User.CompanyId`; optional token payload |
| `GET` | `Company/Mine` | JWT | Company summary + `isCompanyAdmin` |
| `POST` | `Company/RotateInviteCode` | CompanyAdmin / Admin | New code; old invalidated |
| `GET` | `Company/Members` | CompanyAdmin / Admin | Same-company members (`?companyId=` Admin) |
| `GET` | `Company/GetAll` | Admin | All companies with invite codes |

### Deliverables

| Item | Detail | Status |
| --- | --- | --- |
| DTOs | `Core/DTOs/Companies/CompanyDTO.cs` | **Done** |
| Service | `ICompanyService` / `CompanyService` | **Done** |
| Controller | `CompanyController` | **Done** |
| DI | `Program.cs` `AddScoped<ICompanyService, CompanyService>` | **Done** |
| ApiErrorHelper | `UnauthorizedAccessException` → 403 | **Done** |
| Leaderboard scope | Filter by caller company | **Done Task 4** — [detail](phase-13-leaderboard-scope.md) |
| SPA | Join + company board | **Not this task** — Task 5 |
| Formal tests + phase gate | Isolation + Bugbot + `dotnet test` | Interim after this task; full gate Task 6 |

### Acceptance

- Admin can create a company and receive an uppercase invite code.
- User joins by code; first joiner is CompanyAdmin and receives a new JWT.
- Second join with same user rejected; invalid code → 404.
- CompanyAdmin rotates invite / lists members for own company only.
- Platform Admin lists all companies and can manage by `companyId`.

### Git file list (Task 3)

Repo: `WorldCup-System` (branch `master`, uncommitted). From `git status` / `git diff` **23 Jul 2026**.

| Status | Path |
| --- | --- |
| **New** | `Core/DTOs/Companies/CompanyDTO.cs` |
| **New** | `Core/Services/Companies/ICompanyService.cs` |
| **New** | `Core/Services/Companies/CompanyService.cs` |
| **New** | `WorldCup-System/Controllers/CompanyController.cs` |
| Modified | `WorldCup-System/Program.cs` — `AddScoped<ICompanyService, CompanyService>` (+ Task 2 role/repair already present) |
| Modified | `WorldCup-System/Controllers/ApiErrorHelper.cs` — 403 for `UnauthorizedAccessException` |
| **New** | `WorldCup-System.Tests/Services/Companies/CompanyServiceTests.cs` |
| **New** | `docs/changes/phase-13-company-api.md` (this page) |
| Modified | roadmap / changes-review / api-status / planned / index / VitePress |

### Diff snippets (git)

#### DTOs — `CompanyDTO.cs` (new)

```csharp
public class CreateCompanyDTO
{
    [Required] [StringLength(128)]
    public required string Name { get; set; }
    [StringLength(64)]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string? Slug { get; set; }
}

public class JoinCompanyDTO
{
    [Required] [StringLength(32, MinimumLength = 4)]
    public required string InviteCode { get; set; }
}

// + CompanyDTO, RotateInviteCodeDTO, CompanyMineDTO,
//   CompanyMemberDTO, JoinCompanyResultDTO
```

#### Service — invite normalize + first-join (excerpt)

```csharp
string inviteCode = NormalizeInviteCode(dto.InviteCode); // Trim + ToUpperInvariant
// ...
if (memberCountBefore == 0)
{
    await _userManager.AddToRoleAsync(user, CompanyAdminRole);
    becameCompanyAdmin = true;
}
```

#### Controller — Join JWT re-issue (excerpt)

```csharp
JoinCompanyResultDTO result = await _companyService.JoinAsync(dto, userId);
object? tokenPayload = null;
if (result.BecameCompanyAdmin)
{
    tokenPayload = await BuildTokenPayloadAsync(userId);
}
return Ok(new { company = result.Company, becameCompanyAdmin = ...,
                isCompanyAdmin = ..., token = tokenPayload });
```

#### ApiErrorHelper — 403 mapping

```csharp
UnauthorizedAccessException => new ObjectResult(new { error = exception.Message })
{
    StatusCode = StatusCodes.Status403Forbidden
},
```

#### Program.cs — DI

```csharp
builder.Services.AddScoped<ICompanyService, CompanyService>();
```

### Phase 13 Roadmap Mapping

| Roadmap item | Status |
| --- | --- |
| Docs / planned backlog | **Done** — [phase-13-planned](phase-13-planned.md) |
| `p13-company-model` | **Done** — [phase-13-company-model](phase-13-company-model.md) |
| `p13-company-api` | **Done this task** (interim gate pending) |
| `p13-leaderboard-scope` | **Done** (separate; interim gate pending) — [phase-13-leaderboard-scope](phase-13-leaderboard-scope.md) |
| `p13-spa` | Planned — Task 5 |
| `p13-tests-gate` | Planned — Task 6 |

### Related

- [Phase 13 plan](phase-13-planned.md)
- [Task 2 — Company model](phase-13-company-model.md)
- [Task 4 — Leaderboard scope](phase-13-leaderboard-scope.md)
- [Changes Review](../changes-review.md)
- [API Status](../api-status.md)
- [Roadmap Phase 13](../roadmap.md#phase-13)
- Next: [Task 5 — SPA](../roadmap.md#phase-13) (`p13-spa`)
