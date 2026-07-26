# WorldCup System — Phase 13 Task 2: Company Model

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 13 plan](phase-13-planned.md)

## Phase 13 Task 2 — Company model (`p13-company-model`)

Opened **23 Jul 2026**. Checklist: [Roadmap Phase 13](../roadmap.md#phase-13).

> **Success:** **Task 2 implemented + interim gate closed (23 Jul 2026).** `Company` entity, `User.CompanyId`, migration `AddCompany`, repos, `CompanyAdmin` role seed, startup repair. Bugbot found no bugs; **+4** Company repo smoke tests; suite **391** passed. APIs / leaderboard / SPA remain Tasks 3–5. **Full phase gate** remains Task 6.

### Review gate

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Bugbot high/critical findings on Task 2 model | High (interim gate) | **Cleared** | [Bugbot](19908638-de40-4ea4-a16d-38f63954ba77) found no bugs |
| Markdown docs review (this page + changes-review) | High (interim gate) | **Cleared** | Dual-docs mirror VitePress + git |
| Task 2 unit smoke + `dotnet test` | High (interim gate) | **Cleared** | **4** new Company repo tests; suite **391** passed |
| Omit `AddCompany` migration / snapshot from commit | Critical | **Open until commit** | Include `20260723120000_AddCompany.cs` + snapshot |
| Schema drift: EF migration vs `Program.cs` repair SQL | High | **Cleared by design** | Startup `CREATE TABLE IF NOT EXISTS` + FK mirrors migration |
| Invite uniqueness / uppercase normalize at write | High | **Deferred Task 3** | Unique index `Uq_Company_InviteCode` exists; Join/Create must store uppercase |
| Dual docs VitePress vs git mirror drift | High | **Cleared this pass** | Mirror `docs/` ↔ `WorldCup-System/docs/` |
| CompanyAdmin role never assigned | Medium | Accepted until Task 3 | Role seeded; first-join bootstrap + Admin assign in Task 3 |
| No Company HTTP API / leaderboard scope yet | Info | Expected | Tasks 3–4 |
| Full isolation / leak tests | Info | Deferred Task 6 | Two-company leaderboard regression |
| Users with null `CompanyId` | Medium | Accepted v1 | Browse/bet OK; company board empty (Task 4) |
| Circular FK `Company.CreatedByUser` ↔ `User.Company` | Medium | Accepted | Both `ClientSetNull`; CreatedBy ≠ membership |

### Decisions locked this task

| Topic | Decision |
| --- | --- |
| PK | `Company.Id` is `long` (aligns with Identity `User` keys) |
| Invite code | Stored **uppercase**; unique index `Uq_Company_InviteCode`; join compares normalized uppercase (Task 3) |
| Slug | Optional; unique filtered index when not null; unused for routing in v1 |
| Membership | One company per user (`User.CompanyId` nullable) |
| Users with null company | Browse + bet OK; company leaderboard returns **empty list** (not 400) — Task 4 |
| CompanyAdmin bootstrap | **First successful join** to a company with **zero members** receives `CompanyAdmin` (Task 3 Join). Platform `Admin` may also assign the role later. Role row seeded at startup. |
| CreatedByUserId | Optional audit FK only; does not imply membership or CompanyAdmin |

### Deliverables

| Item | Detail | Status |
| --- | --- | --- |
| Entity | `Data/Entities/Company.cs` — Name, Slug?, InviteCode, CreatedAt, CreatedByUserId? | **Done** |
| User FK | `User.CompanyId` + nav `Company` | **Done** |
| DbContext | `DbSet<Company>`; indexes/checks; FK `FK_User_Company` / `FK_Company_CreatedByUser` | **Done** |
| Repos | `IRepositoryManager.Company` + `RepositoryManager` | **Done** |
| Role | Seed `"CompanyAdmin"` alongside Admin / User in `Program.cs` | **Done** |
| Migration | `20260723120000_AddCompany` + snapshot | **Done** |
| Startup repair | `CREATE TABLE IF NOT EXISTS "Company"` + `CompanyId` column / FK in `Program.cs` | **Done** |
| Company HTTP API | Create / join / mine / rotate / members | **Not this task** — Task 3 |
| Leaderboard scope | Filter by caller company | **Not this task** — Task 4 |
| SPA join UI | Invite prompt + company board | **Not this task** — Task 5 |
| Formal tests + phase gate | Isolation tests + Bugbot/docs + `dotnet test` | **Interim cleared** (Bugbot clean; suite **391**); full gate Task 6 |

### Schema sketch

```text
Company
  Id (bigint PK)
  Name (varchar 128)
  Slug (varchar 64, nullable, unique when set)
  InviteCode (varchar 32, unique, uppercase)
  CreatedAt (timestamptz)
  CreatedByUserId (bigint?, FK AspNetUsers)

AspNetUsers
  CompanyId (bigint?, FK Company)  — IX_AspNetUsers_CompanyId
```

### Acceptance

- EF can persist a `Company` and set `User.CompanyId`.
- `CompanyAdmin` role exists after API start (any environment that runs role seed).
- No Company HTTP API yet — that is Task 3.
- Interim gate **cleared**: Bugbot no bugs; suite **391**; full phase gate still Task 6.

### Git file list (Task 2 — from `git status` / `git diff`)

Repo: `WorldCup-System` (branch `master`, uncommitted).

| Status | Path |
| --- | --- |
| **New** | `Data/Entities/Company.cs` |
| **New** | `Data/Migrations/20260723120000_AddCompany.cs` |
| Modified | `Data/Entities/User.cs` — nullable `CompanyId` + nav |
| Modified | `Data/Context/ApplicationDbContext.cs` — `DbSet<Company>` + Fluent config |
| Modified | `Data/Migrations/ApplicationDbContextModelSnapshot.cs` |
| Modified | `Data/Repos/IRepositoryManager.cs` — `IRepository<Company> Company` |
| Modified | `Data/Repos/RepositoryManager.cs` — lazy `Company` repo |
| Modified | `WorldCup-System/Program.cs` — `CompanyAdmin` seed + IF NOT EXISTS repair SQL |
| **New** | `docs/changes/phase-13-company-model.md` (this page) |
| Modified | `docs/changes-review.md`, `docs/roadmap.md`, `docs/index.md`, `docs/data-model.md`, `docs/overview.md`, `docs/changes/phase-13-planned.md`, `docs/.vitepress/config.mts` |

### Diff snippets (git)

#### Entity — `Company.cs` (new)

```csharp
public class Company
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public required string InviteCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public ICollection<User> Members { get; set; } = new List<User>();
}
```

#### User — `CompanyId` FK

```csharp
public long? CompanyId { get; set; }
public Company? Company { get; set; }
```

#### DbContext — indexes / FKs (excerpt)

```csharp
e.HasIndex(company => company.InviteCode)
    .IsUnique()
    .HasDatabaseName("Uq_Company_InviteCode");
e.HasIndex(company => company.Slug)
    .IsUnique()
    .HasFilter("\"Slug\" IS NOT NULL")
    .HasDatabaseName("Uq_Company_Slug");

e.HasOne(user => user.Company)
    .WithMany(company => company.Members)
    .HasForeignKey(user => user.CompanyId)
    .OnDelete(DeleteBehavior.ClientSetNull)
    .HasConstraintName("FK_User_Company");
```

#### Program.cs — role seed + repair

```csharp
string[] roles = { "Admin", "User", "CompanyAdmin" };
// + CREATE TABLE IF NOT EXISTS "Company" ... Uq_Company_InviteCode ...
// + ALTER TABLE "AspNetUsers" ADD COLUMN IF NOT EXISTS "CompanyId" ...
```

#### Migration — `20260723120000_AddCompany`

Creates `Company` table (checks + FKs), unique indexes on `InviteCode` / filtered `Slug`, adds nullable `AspNetUsers.CompanyId` + `FK_User_Company`.

### Roadmap mapping

| Roadmap item | Status | This doc |
| --- | --- | --- |
| Docs / planned backlog | **Done** | [phase-13-planned](phase-13-planned.md) |
| `p13-company-model` | **Done** | This file |
| `p13-company-api` | **Done** | [phase-13-company-api](phase-13-company-api.md) |
| `p13-leaderboard-scope` | **Done** | [phase-13-leaderboard-scope](phase-13-leaderboard-scope.md) |
| `p13-spa` | Planned | Task 5 |
| `p13-tests-gate` | Planned | Task 6 (full phase gate) |

### Related

- [Phase 13 plan](phase-13-planned.md)
- [Task 3 — Company API](phase-13-company-api.md)
- [Changes Review](../changes-review.md)
- [Data Model](../data-model.md)
- Next: [Task 5 — SPA](../roadmap.md#phase-13) (`p13-spa`)
