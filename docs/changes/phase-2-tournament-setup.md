# WorldCup System — Phase 2 Tournament Setup Changes

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md)

## Phase 2 — Tournament Setup APIs

Full diff detail for unstaged changes. [← Back to Changes Review](../changes-review.md)

> **Info:** Source: `git status` + `git diff` on `master` (uncommitted, 22 Jun 2026). Repo: `WorldCup-System/` — 26 modified, 2 deleted, 17 untracked entries (~34 new files including test project).

### Review Gate — Fix Before Testing

| Blocker | Severity | File / area | Required fix |
| --- | --- | --- | --- |
| Destructive migration not applied | Critical | `Data/Migrations/20260619131653_IdentityLongKeys.cs` | Backup/reset dev DB; `dotnet ef database update -p Data -s WorldCup-System` |
| Phase 2 source untracked | Critical | Team/Coach/Player controllers, services, DTOs, tests, migration pair | `git add` all untracked paths before commit |
| Secrets not in committed config | Critical | `appsettings.json` — JWT secret + connection string removed | User Secrets: `JWT:Secret`, `ConnectionStrings:DefaultConnection`, `DevAdmin:Password` |
| `CreateNewUser` unprotected | High | `UserController.cs` | `[Authorize(Roles = "Admin")]` or document public registration |
| Dev admin password missing from config | High | `Program.cs` dev seed, `appsettings.Development.json` | Set `DevAdmin:Password` in User Secrets for Swagger smoke tests |
| `DeleteTeam` FK failures | High | `TeamService.DeleteTeam`, `ApplicationDbContext` FK config | Validate no coaches/players before delete, or cascade in service |

### Architecture — New Tournament Layer

### Phase 2 Roadmap Mapping

| Task | Status | Files |
| --- | --- | --- |
| `p2-repos` | Done | IRepositoryManager.cs, RepositoryManager.cs |
| `p2-team-dto` | Done | Core/DTOs/Teams/TeamDTO.cs, Core/Services/Teams/TeamService.cs |
| `p2-team-api` | Done | Controllers/TeamController.cs |
| `p2-coach` | Done | Core/DTOs/Coaches/, CoachService, CoachController |
| `p2-position` | Done | Program.cs seed, PlayerPositionService, PlayerPositionController |
| `p2-player` | Done | Core/DTOs/Players/, PlayerService, PlayerController |
| `p2-stadium-csv` | Done | StadiumDTO.UploadStadiumDto, StadiumService, StadiumController |
| `p2-validation` | Partial | ApiErrorHelper + new DTO annotations; legacy controllers inconsistent |

<a id="repos"></a>

### Data/Repos — p2-repos

RepositoryManager extended with lazy-initialized repos for tournament entities.

`Repository.GetByIdAsync` implemented (Phase 1 carryover, used by all new services).

<a id="team-dto"></a>

### Core/DTOs/Teams/TeamDTO.cs (untracked)

Read DTO with resolved country/group names. Write DTOs use `[Range]` validation.

<a id="team-service"></a>

### Core/Services/Teams/TeamService.cs (untracked)

Enforces one team per country. Resolves country/group names for list and detail projections.

<a id="team-controller"></a>

### WorldCup-System/Controllers/TeamController.cs (untracked)

Route: `[controller]/[action]`. Public GET; Admin-only mutating actions.

<a id="coach"></a>

### Coach — DTOs, Service, Controller (untracked)

`AddCoachDTO`/`UpdateCoachDTO` with `[Required]`, `[StringLength(40)]`, `[Range]`. Service validates team exists via `GetByIdAsync`.

<a id="player"></a>

### Player — DTOs, Service, Controller (untracked)

Jersey number 1–99 validated on DTO. Service enforces unique number per team. Maps `PositionName` on read.

<a id="position"></a>

### PlayerPosition — p2-position

Startup seed in `Program.cs`; read-only list endpoint. No mutating API (positions are reference data).

<a id="stadium-csv"></a>

### Stadium CSV Import — p2-stadium-csv

`UploadStadiumDto` added to StadiumDTO.cs. Service validates .csv extension and 2 MB limit. Controller endpoint `ImportStadiumsFromCsv`.

<a id="api-error"></a>

### WorldCup-System/Controllers/ApiErrorHelper.cs (untracked)

Central exception-to-HTTP mapping for consistent JSON error bodies (`p2-validation`).

<a id="program"></a>

### WorldCup-System/Program.cs

Registers Phase 2 services. Seeds roles, dev admin, and player positions on startup.

<a id="authorize"></a>

### Domain Controllers — Admin Authorization

Phase 1 carryover completed in this diff: mutating endpoints on City, Country, Group, Stadium, WorldCup, User (except CreateNewUser) require Admin role.

<a id="secrets"></a>

### Configuration — p1-secrets progress

`UserSecretsId` added to csproj. Sensitive values removed from committed appsettings.

<a id="migration"></a>

### Data/Migrations/ — IdentityLongKeys (untracked)

Phase 1 carryover required before Phase 2 integration tests. Destructive: drops `Users` table, converts Identity to `long` keys.

<a id="tests"></a>

### WorldCup-System.Tests/ (untracked, 49 facts)

xUnit (net8.0) + Moq. Phase 2 coverage highlighted below.

| Test file | Facts | Covers |
| --- | --- | --- |
| `TeamServiceTests` | 4 | GetTeams mapping, country uniqueness, assign to group, delete |
| `CoachServiceTests` | 2 | Add coach, get by team |
| `PlayerServiceTests` | 2 | Add player, jersey number conflict |
| `StadiumServiceTests` | 9 | CSV validation (empty, wrong ext, size), city lookup, duplicate skip |
| `UserServiceTests` | 7 | Identity CRUD via UserManager |
| `RepositoryTests` | 2 | GetByIdAsync found / not found |
| `*ControllerTests` | 15 | Authorize attributes, action delegation (City, Group, Stadium, WorldCup, User) |
| `City/Group/WorldCup service tests` | 8 | DTO Id mapping (Phase 1 carryover) |

**Not covered:** Team/Coach/Player controllers (integration), EF migration apply, User Secrets bootstrap, DeleteTeam FK edge case.

<a id="endpoints"></a>

### New API Endpoints

| Controller | Action | Method | Auth |
| --- | --- | --- | --- |
| Team | GetTeams | GET | Public |
| Team | GetTeamById/{id} | GET | Public |
| Team | AddTeam | POST | Admin |
| Team | UpdateTeam | POST | Admin |
| Team | AssignTeamToGroup | POST | Admin |
| Team | DeleteTeam/{id} | DELETE | Admin |
| Coach | GetCoaches | GET | Public |
| Coach | GetCoachesByTeam/{teamId} | GET | Public |
| Coach | AddCoach / UpdateCoach | POST | Admin |
| Coach | DeleteCoach/{id} | DELETE | Admin |
| Player | GetPlayers / GetPlayersByTeam/{teamId} | GET | Public |
| Player | AddPlayer / UpdatePlayer | POST | Admin |
| Player | DeletePlayer/{id} | DELETE | Admin |
| PlayerPosition | GetPlayerPositions | GET | Public |
| Stadium | ImportStadiumsFromCsv | POST (form) | Admin |
