# WorldCup System

Full-stack FIFA World Cup management platform — tournament setup, fixtures, knockout bracket, live match operations, user betting, and company-scoped leaderboards.

**Stack:** ASP.NET Core 8 · Angular · PostgreSQL · EF Core · Docker · JWT / Identity

## Features

- Countries, cities, stadiums, World Cups, groups, teams, and squads
- Match scheduling, live stats (goals/cards), and knockout bracket
- Post-match FIFA timeline sync (scorers / results) via MatchSync services
- User betting, scoring, and company-scoped leaderboards
- Admin SPA for ops; public SPA for fixtures, standings, and bets
- Offline WC 2026 data import and WC 2030 multi-cup simulation scripts

## Repository layout

```
WorldCup-System/          # ASP.NET Core Web API (host)
Core/                     # Business logic, DTOs, services
Data/                     # EF Core, entities, migrations
WorldCup-System.Tests/    # xUnit tests
worldcup-client/          # Angular SPA (port 4200)
docs/                     # Roadmap, API status, change notes
scripts/                  # Ops / import / simulation scripts
```

## Quick start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/)
- EF Core CLI: `dotnet tool install --global dotnet-ef`
- Node.js (for the Angular client)

### Configure secrets

Sensitive values are **not** stored in `appsettings.json`. Use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) (Development) or environment variables (production):

```powershell
# From the repo root (this folder — siblings Data/ and WorldCup-System/)
cd WorldCup-System
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=WorldCupDb;Username=postgres;Password=YOUR_PASSWORD"
dotnet user-secrets set "JWT:Secret" "YOUR_JWT_SIGNING_KEY_AT_LEAST_32_CHARS"
dotnet user-secrets set "DevAdmin:Password" "Admin123!"
cd ..
```

### Database, API, tests, client

```powershell
# Still / again at the repo root (this folder). Do not stay inside WorldCup-System/ (API host) — `-p Data` will fail.
dotnet ef database update -p Data -s WorldCup-System
dotnet run --project WorldCup-System
dotnet test WorldCup-System.Tests\WorldCup-System.Tests.csproj

cd worldcup-client
npm start
```

- API / Swagger: Development HTTPS port from launch settings (Swagger at `/swagger`)
- SPA: `http://localhost:4200` (calls the API — start the API first)
- Optional local Dev: `docker compose up` for API + PostgreSQL
- Production override: `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build` (requires `PROD_*` host secrets — see `docs/changes/phase-14-compose.md`)
- Production SPA: set `worldcup-client/src/environments/environment.ts` `apiUrl`, then `npm ci` + `npm run build`; serve `dist/worldcup-client/browser/` — see `docs/changes/phase-14-spa-prod.md`
- Target migrate + smoke: confirm connection string, `dotnet ef database update -p Data -s WorldCup-System`, then health/company smoke checklist — see `docs/changes/phase-14-migrate-smoke.md` (never wipe-import against prod)

## Auth

- ASP.NET Identity + JWT Bearer
- Roles: `Admin`, `User`
- In Development, seeded accounts use User Secrets / env passwords (see `.env.example` / docs)

## Docs

See [`docs/`](docs/) for roadmap, API status, data model, and phase change notes.

## License

Private / personal project unless otherwise stated.
