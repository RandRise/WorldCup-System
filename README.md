# WorldCup System

Full-stack FIFA World Cup management platform — tournament setup, live match ops, knockout bracket, user betting, and company-scoped prediction pools.

**Status:** Complete (Phases **1–15**). Product, deploy packaging, local production rehearsal, and WC 2026 ops hygiene are done.

**Stack:** ASP.NET Core 8 · Angular · PostgreSQL · EF Core · Docker · JWT / Identity · Serilog

## Features

| Area | What’s included |
| --- | --- |
| Tournament | Countries, cities, stadiums, World Cups, groups, teams, coaches, squads |
| Matches | Schedule, goals/cards/stats, group standings, knockout bracket + feeder advance |
| Live ops | Admin live console, live snapshot polling, FIFA calendar/timeline result + scorer sync |
| Betting | Place predictions, 3/0 scoring, auto-resolve at FT, leaderboard |
| Companies | Soft multi-tenancy — invite join, company-scoped leaderboards |
| Data | Offline WC 2026 import through Final; WC 2030 simulation scripts |
| Ops | Docker Compose (Dev + Production override), health checks, CI, wipe-guard hygiene |

## Repository layout

```
WorldCup-System/          # ASP.NET Core Web API (host)
Core/                     # Business logic, DTOs, services
Data/                     # EF Core, entities, migrations
WorldCup-System.Tests/    # xUnit tests
worldcup-client/          # Angular SPA (port 4200)
docs/                     # Roadmap, API status, phase change notes
scripts/                  # Ops / import / simulation scripts
docker-compose.yml        # Local Dev stack
docker-compose.prod.yml   # Production override (requires PROD_* secrets)
```

## Quick start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/)
- EF Core CLI: `dotnet tool install --global dotnet-ef`
- Node.js (Angular client)

### Configure secrets

Sensitive values are **not** in `appsettings.json`. Use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) (Development) or environment variables (production):

```powershell
# From the repo root (siblings Data/ and WorldCup-System/)
cd WorldCup-System
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=WorldCupDb;Username=postgres;Password=YOUR_PASSWORD"
dotnet user-secrets set "JWT:Secret" "YOUR_JWT_SIGNING_KEY_AT_LEAST_32_CHARS"
dotnet user-secrets set "DevAdmin:Password" "Admin123!"
cd ..
```

### Database, API, tests, client

```powershell
# At the repo root — do not stay inside WorldCup-System/ (API host) or `-p Data` fails
dotnet ef database update -p Data -s WorldCup-System
dotnet run --project WorldCup-System
dotnet test WorldCup-System.Tests\WorldCup-System.Tests.csproj

cd worldcup-client
npm start
```

| Surface | URL |
| --- | --- |
| SPA | `http://localhost:4200` |
| API (typical Dev HTTP) | `http://localhost:5055` |
| Swagger (Development) | HTTPS port from launch settings → `/swagger` |
| Health | `GET /health` |

### Dev accounts (Development seed)

| Role | Email | Password (local default) | SPA |
| --- | --- | --- | --- |
| Admin | `admin@localhost` | `Admin123!` | `/admin` |
| User | `user@localhost` | `User123!` | `/dashboard`, bets |

Passwords come from User Secrets / env; see `.env.example`.

## Docker

```powershell
# Local Dev
docker compose up -d --build

# Production override (set PROD_* on the host — never commit real values)
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

Required Production vars: `PROD_POSTGRES_USER`, `PROD_POSTGRES_PASSWORD`, `PROD_POSTGRES_DB`, `PROD_JWT_SECRET`, `PROD_JWT_VALID_ISSUER`, `PROD_JWT_VALID_AUDIENCE`, `PROD_CORS_ALLOWED_ORIGINS`.

Detail: [`docs/changes/phase-14-compose.md`](docs/changes/phase-14-compose.md) · [`phase-14-secrets.md`](docs/changes/phase-14-secrets.md) · [`phase-15-deploy.md`](docs/changes/phase-15-deploy.md)

## Production SPA

1. Set `worldcup-client/src/environments/environment.ts` `apiUrl` (no trailing slash). Local ship rehearsal uses `http://localhost:5055`; use your public API origin for a real host.
2. `npm ci` then `npm run build` in `worldcup-client/`
3. Serve `dist/worldcup-client/browser/` (SPA fallback to `index.html`); align CORS with the SPA origin

Detail: [`docs/changes/phase-15-spa-url.md`](docs/changes/phase-15-spa-url.md) · [`phase-14-spa-prod.md`](docs/changes/phase-14-spa-prod.md)

## Auth & roles

- ASP.NET Identity + JWT Bearer
- Roles: `Admin`, `User`, `CompanyAdmin` (company invite manage)
- CORS policy `Spa` — config-driven origins (`Cors__AllowedOrigins` / `PROD_CORS_ALLOWED_ORIGINS`)

## Data ops caution

Do **not** wipe-import against a DB you care about. Bare `scripts/import_wc2026_finished_matches.py` refuses without `--i-understand-this-wipes-wc2026`. Prefer upsert scripts such as `add_sf2_and_final.py` for Final corrections.

Inventory: [`docs/changes/phase-15-data-hygiene.md`](docs/changes/phase-15-data-hygiene.md)

## Docs

See [`docs/`](docs/) — roadmap (Phases 1–15 Done), API status, data model, and phase change notes. Start at [`docs/index.md`](docs/index.md) · gate: [`docs/changes/phase-15-gate.md`](docs/changes/phase-15-gate.md).

## License

Private / personal project unless otherwise stated.
