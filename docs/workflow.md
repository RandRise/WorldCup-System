# WorldCup System — Dev Workflow

## Docs navigation

[Dashboard](index.md) | [System Overview](overview.md) | [Completion Roadmap](roadmap.md) | [Dev Workflow](workflow.md) | [API Status](api-status.md) | [Data Model](data-model.md) | [Changes Review](changes-review.md)

---
## Development Workflow

Step-by-step guide for setting up, developing, and extending the system. Project status: see [Dashboard](index.md) / [Roadmap](roadmap.md).

### 1. Initial Setup

#### Prerequisites


.NET 8 SDK, PostgreSQL, Node.js 18+ (for MCP server), Cursor IDE with MCP configured.

#### Database

Create PostgreSQL database. Store the connection string and JWT secret in **User Secrets** or environment variables (not committed `appsettings.json`). Docker Compose can also run API + Postgres together.

#### Apply migrations

From the Data project directory:

#### Run the API

Prefer the launcher (frees port `5055` and starts Postgres if needed). Only one API instance can bind that port. Swagger: `http://localhost:5055/swagger`

#### MCP server (optional)

Enables read-only DB queries from Cursor. Already configured in `.cursor/mcp.json`.

### 2. Feature Development Pattern

Follow this order for each new domain (Team, Match, Bet, etc.):

### Repository layer

- Add entity to `IRepositoryManager` and `RepositoryManager`
- Use generic `IRepository<T>` from `Data/Repos/`
- Implement `GetByIdAsync` first if not done (Phase 1)

### Service layer

- Create folder under `Core/Services/{Domain}/`
- Define interface + implementation
- Map entities ↔ DTOs in service methods
- Register in `Program.cs` DI container

### Controller layer

- Route: `[controller]/[action]` (existing convention)
- Inject service via constructor
- Add `[Authorize]` on write operations
- Return appropriate HTTP status codes

### Verification

- Test via Swagger UI at `/swagger`
- Query DB via MCP tools in Cursor
- Check off task in [Roadmap](roadmap.md)

### 2.1 Per-Phase Completion Gate

After implementing all tasks in a roadmap phase, follow this order before starting the next phase:

| Step | What | Gate |
| --- | --- | --- |
| 1. Implement | Complete roadmap tasks for the current phase | In progress |
| 2. Review | Bugbot code review + update [Changes Review](changes-review.md) | No unfixed high/critical issues |
| 3. Test | Extend `WorldCup-System.Tests`, run `dotnet test` | All tests pass |
| 4. Advance | Start next phase in [Roadmap](roadmap.md) | Only after 2 + 3 |

Cursor rule: `.cursor/rules/phase-completion-review.mdc` — agent enforces this loop automatically.

### 3. Entity Change Workflow

When modifying the data model:

1. Edit entity in `Data/Entities/`
2. Update `ApplicationDbContext` if needed (relationships, indexes)
3. Create migration:

1. Review generated migration file in `Data/Migrations/`
2. Apply: `dotnet ef database update`
3. Update DTOs and services to match schema changes

### 4. Reference Data (One-Time Populate)

Countries, cities, and stadiums are normal database rows. Insert them once; they persist across API restarts. There is no CSV import or demo-seed endpoint anymore.

| Data | How to populate | List API | Notes |
| --- | --- | --- | --- |
| Countries | SQL / migration fixture (no public create endpoint yet) | `Country/GetAllCountries` | Required for admin Teams dropdown |
| Cities | SQL / migration fixture (no public create endpoint yet) | `City/GetAllCities` | Required before stadiums (FK) |
| Stadiums | Admin `Stadium/AddStadium` (Swagger or API client) | `Stadium/GetStadiums` | Required for match schedule venues |
| World Cup + Groups | `WorldCup/CreateNewWorldCup`, `Group/AddGroup` | List endpoints on those controllers | Ready |
| Teams, Players, Matches | Admin UI — Teams & squads, Match schedule | Team / Player / Match controllers | Ready |
| Player positions | Auto-seeded at API startup (GK, DF, MF, FW) | `PlayerPosition` list | Identity roles + DevAdmin/DevUser also seeded in Development |

Recommended create order: Countries → Cities → Stadiums → WorldCup → Groups → Teams → Players → Matches

### 5. Auth Testing Workflow

#### Create user


POST `User/CreateNewUser` with name, email, password.

#### Login

POST `User/Login` → receive JWT token and expiration.

#### Authorize in Swagger

Click "Authorize" in Swagger UI, enter `Bearer {token}`.

#### Test protected endpoints

Mutating endpoints require JWT auth with Admin/User role policies (Phase 1). Use Swagger **Authorize** with `Bearer {token}` after login.

### 6. MCP Database Exploration

Use Cursor MCP tools to inspect the database while developing:

| Tool | Purpose |
| --- | --- |
| `get_database_info` | Connection details and PostgreSQL version |
| `list_tables` | All tables in the database |
| `describe_table` | Columns and foreign keys for a table |
| `query` | Read-only SELECT (max 1000 rows) |

Example: `SELECT * FROM "Countries" LIMIT 10;`

### 7. Git Workflow (Suggested)

- One feature branch per roadmap phase or sub-feature
- Small, focused commits with clear messages
- PR review before merging to main
- Update roadmap checklists as tasks complete
