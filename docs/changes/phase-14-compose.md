# WorldCup System — Phase 14 Task 4: Production Compose

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 14 plan](phase-14-planned.md) · [Secrets inventory](phase-14-secrets.md) · [CORS/JWT](phase-14-cors.md)

## Phase 14 Task 4 — Production Compose (`p14-compose`)

Opened **27 Jul 2026**. Checklist: [Roadmap Phase 14](../roadmap.md#phase-14) — Task 4 **[x]** Done. Next: Task 5 SPA prod → **Done** ([phase-14-spa-prod](phase-14-spa-prod.md)); next Task 6 migrate/smoke.

> **Success:** `docker-compose.prod.yml` override sets `ASPNETCORE_ENVIRONMENT=Production`, requires **`PROD_*`** secrets via `${VAR:?…}` (fail if unset/empty; Dev `.env` keys do not satisfy), clears DevAdmin/DevUser env, keeps `/health` check. Local `docker-compose.yml` unchanged for casual Dev.

<a id="review-gate"></a>

## Review gate

High/critical items that must clear **before** treating Task 4 as closed:

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Prod path still ships `ASPNETCORE_ENVIRONMENT=Development` | Critical | **Cleared** | Override sets `Production` |
| Weak JWT / DB defaults usable without setting env | Critical | **Cleared** | `${PROD_*:?…}` — Compose fails if unset/empty |
| Local Dev `.env` silently satisfies prod `${VAR:?}` (Bugbot) | High | **Cleared** | Distinct `PROD_*` names — not in local `.env.example` Dev section |
| Real secrets baked into override YAML | Critical | **Cleared by design** | Placeholders only; values from host env |
| DevAdmin/DevUser still seeded in Production | High | **Cleared** | Env cleared in override; seed only when `IsDevelopment()` |
| Local Dev Compose broken / forced to set prod secrets | High | **Cleared** | Base `docker-compose.yml` unchanged; prod uses `-f … -f docker-compose.prod.yml` |
| Duplicate Postgres port bind from override `ports:` merge | High | **Cleared by design** | No `ports` in prod file (Compose appends lists) |
| Healthcheck lost or invalid | High | **Cleared** | API inherits `/health`; DB uses container `$$POSTGRES_*` (not host Dev interpolation) |
| Prod DB healthcheck vs `PROD_*` remap (Bugbot Task 7) | High | **Cleared** | Base `pg_isready -U $$POSTGRES_USER -d $$POSTGRES_DB` follows override env |
| Dual docs VitePress ↔ git mirror | High | **Cleared this pass** | `docs/` and `WorldCup-System/docs/` |
| Postgres still published via base `5432:5432` | Medium | **Documented** | Firewall / unpublish on shared hosts |
| Operator pastes weak values into `PROD_*` anyway | Medium | **Documented** | Host secret store; never commit filled prod env |

**Gate verdict:** Bugbot high (Dev `.env` satisfying prod) **fixed** via `PROD_*` names. Formal Phase 14 close remains Task 7 (`p14-gate`).

---

## Locked approach

**Option A** — override file (preferred in [phase-14-planned](phase-14-planned.md)):

```powershell
cd WorldCup-System
# Export PROD_* env (or a gitignored prod env file — not .env.example Dev keys)
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

Local Dev remains:

```powershell
docker compose up
```

---

## Required env (prod Compose)

| Compose / host env | Maps to | Note |
| --- | --- | --- |
| `PROD_POSTGRES_USER` | DB + connection string | **Required** (`:?`) — not `POSTGRES_USER` |
| `PROD_POSTGRES_PASSWORD` | DB + connection string | **Required** — strong password |
| `PROD_POSTGRES_DB` | DB + connection string | **Required** |
| `PROD_JWT_SECRET` | `JWT__Secret` | **Required** — ≥32 chars — not `JWT_SECRET` |
| `PROD_JWT_VALID_ISSUER` | `JWT__ValidIssuer` | **Required** — public API URL |
| `PROD_JWT_VALID_AUDIENCE` | `JWT__ValidAudience` | **Required** — SPA audience / origin string |
| `PROD_CORS_ALLOWED_ORIGINS` | `Cors__AllowedOrigins` | **Required** — no trailing slash, no `*` |
| `API_PORT` | Host publish | Optional; base default `5055` |

Why `PROD_*`? Compose auto-loads project `.env`. Local Dev keys (`JWT_SECRET`, `POSTGRES_PASSWORD`, …) would otherwise pass `${VAR:?}` checks with weak defaults. Distinct names force an intentional Production set.

Canonical inventory: [phase-14-secrets](phase-14-secrets.md). CORS rules: [phase-14-cors](phase-14-cors.md).

---

## Behavior

| Concern | Prod override |
| --- | --- |
| Environment | `ASPNETCORE_ENVIRONMENT=Production` |
| Dev seed | `DevAdmin__*` / `DevUser__*` set to empty (base values overridden) |
| Secrets | `PROD_*` only; Compose errors if unset; Dev `.env` alone fails |
| Health | Same as base: `curl -f http://localhost:8080/health` |
| Postgres port | Still published via base file — firewall on shared hosts |

---

## Git status (Task 4 slice)

From nested repo `WorldCup-System/` (27 Jul 2026):

```
?? docker-compose.prod.yml
 M .env.example
?? README.md
?? docs/changes/phase-14-compose.md
```

Plus dual docs (roadmap / index / changes-review / planned / secrets / VitePress) and workspace-root `README.md` § Production Docker Compose (outside nested git).

---

## Files changed

| Path | Change |
| --- | --- |
| `docker-compose.prod.yml` | **New** — Production override (`PROD_*` required) |
| `.env.example` | Prod Compose run command + `PROD_*` placeholders (commented) |
| `docs/changes/phase-14-compose.md` | This page (dual) |
| Nested `README.md` | Prod Compose pointer |
| Workspace root `README.md` | § Production Docker Compose (outside nested git) |
| Docs (dual) | Roadmap / index / changes-review / planned / secrets / VitePress |

Roadmap mapping: [Phase 14](../roadmap.md#phase-14) — `p14-compose` **Done** `[x]`.

---

## Diff snippets

### `docker-compose.prod.yml` (NEW)

```yaml
services:
  db:
    environment:
      POSTGRES_USER: ${PROD_POSTGRES_USER:?Set PROD_POSTGRES_USER for production}
      POSTGRES_PASSWORD: ${PROD_POSTGRES_PASSWORD:?Set PROD_POSTGRES_PASSWORD for production}
      POSTGRES_DB: ${PROD_POSTGRES_DB:?Set PROD_POSTGRES_DB for production}

  api:
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: Host=db;Port=5432;Database=${PROD_POSTGRES_DB:?…};Username=${PROD_POSTGRES_USER:?…};Password=${PROD_POSTGRES_PASSWORD:?…}
      JWT__Secret: ${PROD_JWT_SECRET:?Set PROD_JWT_SECRET (≥32 chars) for production}
      JWT__ValidIssuer: ${PROD_JWT_VALID_ISSUER:?…}
      JWT__ValidAudience: ${PROD_JWT_VALID_AUDIENCE:?…}
      Cors__AllowedOrigins: ${PROD_CORS_ALLOWED_ORIGINS:?…}
      DevAdmin__Email: ""
      DevAdmin__Password: ""
      DevUser__Email: ""
      DevUser__Password: ""
```

### Operator PowerShell example

```powershell
$env:PROD_POSTGRES_USER = "worldcup"
$env:PROD_POSTGRES_PASSWORD = "REPLACE_ME_STRONG_DB_PASSWORD"
$env:PROD_POSTGRES_DB = "WorldCupDb"
$env:PROD_JWT_SECRET = "REPLACE_ME_JWT_SECRET_AT_LEAST_32_CHARS"
$env:PROD_JWT_VALID_ISSUER = "https://api.example.com"
$env:PROD_JWT_VALID_AUDIENCE = "https://app.example.com"
$env:PROD_CORS_ALLOWED_ORIGINS = "https://app.example.com"

cd WorldCup-System
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

---

## Verify

| Check | Pass | Evidence |
| --- | --- | --- |
| Local `docker compose up` still Dev | Yes | Base unchanged |
| Prod override sets Production | Yes | `ASPNETCORE_ENVIRONMENT: Production` |
| Missing `PROD_JWT_SECRET` fails | Yes | `${PROD_JWT_SECRET:?…}` |
| Dev `.env` alone cannot satisfy prod | Yes | Local keys are `JWT_SECRET` / `POSTGRES_*`, not `PROD_*` |
| No DevAdmin seed in Production | Yes | Empty seed env + `IsDevelopment()` gate |
| Healthcheck still valid | Yes | Inherited `/health` |
| No duplicate Postgres `ports` | Yes | No `ports:` in prod file |

---

## Related

- [Phase 14 planned Task 4](phase-14-planned.md#task-4--production-compose-p14-compose)
- [Phase 14 secrets](phase-14-secrets.md)
- [Phase 14 CORS](phase-14-cors.md)
- [Roadmap Phase 14](../roadmap.md#phase-14)
- [Changes Review](../changes-review.md)
