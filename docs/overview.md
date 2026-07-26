# WorldCup System — Overview

## Docs navigation

[Dashboard](index.md) | [System Overview](overview.md) | [Completion Roadmap](roadmap.md) | [Dev Workflow](workflow.md) | [API Status](api-status.md) | [Data Model](data-model.md) | [Changes Review](changes-review.md)

---
## System Overview

Architecture, tech stack, and repository layout. Updated after project audit (17 Jul 2026).

### What This System Does

A FIFA World Cup management platform covering the full tournament lifecycle:

- Countries, cities, stadiums (stored once in the database)
- Tournament setup — World Cups, groups, teams, squads
- Match management — scheduling, knockout bracket, live stats, goals, cards (admin live console)
- User betting — predictions, scoring (3/0), leaderboard, auto-resolve
- Public SPA — fixtures, bracket, standings, bets, user dashboard; admin hub for ops
- Offline WC 2026 data import (groups + finished matches through both semi-finals)

### Tech Stack

### Backend

| Framework | ASP.NET Core 8 Web API |
| --- | --- |
| ORM | Entity Framework Core 8 |
| Database | PostgreSQL (Npgsql) |
| Auth | ASP.NET Identity + JWT Bearer |
| API Docs | Swagger / OpenAPI |
| Logging | Serilog (compact JSON console) |

### Frontend & Ops

| Frontend | Angular SPA (`WorldCup-System/worldcup-client`, port 4200) |
| --- | --- |
| MCP Server | Node.js + TypeScript + pg |
| Tests | xUnit — services, controllers, WebApplicationFactory |
| Containers | Docker Compose (API + PostgreSQL) |
| CI | GitHub Actions — dotnet + Angular lint/test |

### Solution Architecture

### Repository Layout

### Layer Responsibilities

### WorldCup-System (API)

HTTP endpoints, JWT auth, Swagger, DI wiring, health checks. Controllers delegate to Core services.

### Core

Business logic, DTOs, service interfaces — teams, matches, goals, cards, bets, standings, leaderboard, etc.

### Data

EF Core `ApplicationDbContext`, entities, generic repository, `RepositoryManager` (all domain tables wired).

### Open technical debt

> **Info:** Phase 1 foundation debt is cleared. Remaining gaps are product features, not blockers for group-stage use.

| Issue | Location | Impact |
| --- | --- | --- |
| External live scores API | — | Cancelled for live — Phase 10 post-match FIFA calendar sync available; Phase 11 timeline scorers **Done** |
| Recent events wrong scorers | Goals / sync | Phase 11 honesty + real scorers **Done** |
| Global (cross-company) leaderboard | Betting / leaderboard | Replaced by company-scoped boards in [Phase 13 Task 4](changes/phase-13-leaderboard-scope.md); join via [Task 3 Company API](changes/phase-13-company-api.md) + [Task 5 SPA](changes/phase-13-spa.md) (Task 5 interim / Task 6 full gate still open) |
