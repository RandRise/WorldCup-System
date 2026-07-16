# WorldCup System — Phase 1 Identity Changes

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md)

## Phase 1 — Foundation & Auth Hardening

Full diff detail for unstaged changes. [← Back to Changes Review](../changes-review.md)

> **Info:** Source: `git status` + `git diff` on `master` (uncommitted, 19 Jun 2026). Repo: `WorldCup-System/` — 18 modified, 2 deleted, 3 untracked.

### Review Gate — Fix Before Testing

| Blocker | Severity | File / area | Required fix |
| --- | --- | --- | --- |
| Destructive migration not applied | Critical | `Data/Migrations/20260619131653_IdentityLongKeys.cs` | Backup/reset dev DB; `dotnet ef database update -p Data -s WorldCup-System` |
| Migration files untracked | Critical | `IdentityLongKeys.cs`, `IdentityLongKeys.Designer.cs` | `git add` before commit |
| Domain controllers lack `[Authorize]` | High | City, Stadium, Group, WorldCup, Country controllers | Complete [p1-authorize](../roadmap.md#phase-1) |
| User create/update/list unprotected | High | `UserController.cs` | Add role policies to mutating/list actions |
| JWT secret in appsettings | High | `appsettings.json` — [p1-secrets](../roadmap.md#phase-1) | User Secrets / environment variables |

### Architecture Shift

### Phase 1 Roadmap Mapping

| Task | Status | Files |
| --- | --- | --- |
| `p1-identity` | Done | User.cs, ApplicationDbContext.cs, Program.cs, UserService.cs, UserController.cs, Migrations |
| `p1-getbyid` | Done | Repository.cs |
| `p1-authorize` | Partial | UserController.cs (RemoveUser only) |
| `p1-cors` | Done | Program.cs |
| `p1-email` | Done | Program.cs |
| `p1-cleanup` | Done | WeatherForecast deleted, CityService.cs |
| `p1-secrets` | Todo | — |
| `p1-readme` | Todo | — |
| `p1-dto-ids` | Done | StadiumDTO, WorldCupDTO, City/Group/Stadium/WorldCup services |

<a id="user-entity"></a>

### Data/Entities/User.cs

Domain user becomes the Identity user. Removes duplicate `int Id` in favor of inherited `long Id`.

<a id="bet"></a>

### Data/Entities/Bet.cs

Foreign key aligned with `User.Id` (`long`).

<a id="dbcontext"></a>

### Data/Context/ApplicationDbContext.cs

Wires EF Identity to custom `User` entity and long role keys. Removes redundant `DbSet<User>`.

<a id="migration"></a>

### Data/Migrations/20260619131653_IdentityLongKeys.cs (untracked)

Destructive Drops legacy `Users` table; converts all Identity PK/FK columns from `string` to `bigint`; adds domain columns to `AspNetUsers`.

Snapshot `ApplicationDbContextModelSnapshot.cs` updated in index to match — `User` maps to `AspNetUsers` with full Identity columns.

<a id="migration-fix"></a>

### Migration Fix — IdentityLongKeys applies on PostgreSQL (Data/Migrations/20260619131653_IdentityLongKeys.cs)

> **Info:** The migration converts ASP.NET Identity keys (`AspNetUsers.Id`, `AspNetRoles.Id` and related FK columns) and `Bet.UserId` from `text`/`int` to `bigint` (`long`). As originally scaffolded it failed mid-apply on PostgreSQL; `Up()` and `Down()` were rewritten so all 20 migrations now apply cleanly.

#### Problem 1 — missing `USING` clause

PostgreSQL will not implicitly cast `text` → `bigint`. EF's scaffolded `AlterColumn<long>` emitted a bare `TYPE bigint`, which the server rejects.

#### Problem 2 — FK columns retyped while referenced columns were still `text`

PostgreSQL refuses to retype a column while a foreign key ties it to a column of a different type. The fix drops the 6 Identity FKs first, converts every key, then recreates the FKs. `Up()` also clears pre-existing GUID-keyed users/roles (which cannot be cast to `bigint`); the Dev admin is re-seeded at startup. `Down()` mirrors the same drop → retype → recreate ordering in reverse.

#### Verification

After the rewrite: all 20 migrations apply cleanly via `dotnet ef database update -p Data -s WorldCup-System`, the API boots and seeds the Dev admin, and all 90 tests pass.

Destructive The migration drops the legacy `Users` table and deletes existing Identity users/roles. **Mitigation:** take a `pg_dump` backup before applying; Dev admin is automatically re-seeded on startup.

<a id="user-dto"></a>

### Core/DTOs/Users/UserDTO.cs

<a id="dto-ids"></a>

### Core/DTOs — StadiumDTO, WorldCupDTO

List DTOs now expose primary keys for client-side references.

<a id="user-service"></a>

### Core/Services/Users/UserService.cs

Removed repository dependency; all user operations go through `UserManager<User>`.

<a id="city-service"></a>

### Core/Services/Cities/CityService.cs

Removes `NotImplementedException` stub; maps `Id` and `CountryId` in list projection.

<a id="list-services"></a>

### Core/Services — Group, Stadium, WorldCup

List projections now include `Id` field.

<a id="repository"></a>

### Data/Repos/Repository.cs

`p1-getbyid` — replaces `NotImplementedException`.

<a id="user-controller"></a>

### WorldCup-System/Controllers/UserController.cs

Delegates create to service; Admin-only delete; improved login error handling.

<a id="program"></a>

### WorldCup-System/Program.cs

<a id="weather-deleted"></a>

### Deleted — WeatherForecastController.cs, WeatherForecast.cs

Template API scaffold removed per `p1-cleanup`.

<a id="solution"></a>

### WorldCup-System.sln

<a id="tests"></a>

### WorldCup-System.Tests/ (untracked)

xUnit (net8.0) + Moq. Constructor injects only `UserManager<User>` — matches refactored service.

| Test | Covers |
| --- | --- |
| `GetAllUsers_ReturnsMappedUserDtos` | `_userManager.Users` → `UserDTO` with `long` Id |
| `GetAllUsers_WhenNoUsers_ReturnsEmptyList` | Empty queryable |
| `CreateNewUser_WhenIdentityCreationSucceeds_CreatesUserWithExpectedFields` | Email as UserName, Name, SecurityStamp, `AddToRoleAsync("User")` |
| `UpdateUser_WhenUserExists_UpdatesNameViaUserManager` | `FindByIdAsync` + `UpdateAsync` |
| `UpdateUser_WhenUserNotFound_DoesNotUpdate` | No-op when missing |
| `RemoveUser_WhenUserExists_DeletesViaUserManager` | `DeleteAsync` |
| `RemoveUser_WhenUserNotFound_DoesNotDelete` | No delete when missing |

**Not covered:** `UserController` login/create, role seeding, JWT, EF migration apply, domain controller auth.

<a id="data-csproj"></a>

### Data/Data.csproj

Indentation-only change on EF Tools package reference.
