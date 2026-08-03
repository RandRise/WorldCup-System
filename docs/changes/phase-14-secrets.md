# WorldCup System — Phase 14 Task 2: Prod Secrets Inventory

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 14 plan](phase-14-planned.md)

## Phase 14 Task 2 — Prod secrets inventory (`p14-secrets`)

Opened **27 Jul 2026**. Checklist: [Roadmap Phase 14](../roadmap.md#phase-14).

> **Success:** Canonical Production env-var inventory with **placeholders only**. README + `.env.example` point here. No runtime code changes in Task 2. CORS origin env wiring is **Done** in Task 3 — [phase-14-cors](phase-14-cors.md).

### Review gate

High/critical items that must clear **before** testing or treating Task 2 as closed (docs-only slice — no `dotnet test`):

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Real passwords / JWT secrets in git or Markdown | Critical | **Cleared by design** | Placeholders only (`REPLACE_ME_*`) |
| Table covers API boot-required keys | High | **Cleared** | See required table below |
| Dual docs VitePress vs git mirror | High | **Cleared** | Mirrored `docs/` ↔ `WorldCup-System/docs/` |
| Runtime behavior changed in Task 2 | High | **N/A — docs only** | No `Program.cs` / Compose behavior edits |
| Weak local Compose defaults used on a shared/prod host | Medium (residual) | **Cleared (Task 4)** | Use [phase-14-compose](phase-14-compose.md) override; do not reuse `.env.example` weak values |
| CORS env key documented prematurely as implemented | Medium | Accepted | Listed as Task 3 deliverable |

**Gate verdict:** No high/critical blockers. Residual medium: do not deploy with local Compose defaults.

### Never commit real values

- Do **not** commit real passwords, JWT signing keys, connection strings, or a filled `.env`.
- Safe to commit: `WorldCup-System/.env.example` (placeholders / local-dev defaults marked clearly).
- Production: host secret store, CI secrets, or server env — never `appsettings.json`.

### Required Production env vars (API boot)

ASP.NET Core maps nested config with `__`. Set these on the host (or via Compose prod override in Task 4).

| Env var | Config key | Purpose | Prod note / placeholder |
| --- | --- | --- | --- |
| `ConnectionStrings__DefaultConnection` | `ConnectionStrings:DefaultConnection` | Postgres Npgsql | `Host=REPLACE_ME_DB_HOST;Port=5432;Database=REPLACE_ME_DB;Username=REPLACE_ME_USER;Password=REPLACE_ME_PASSWORD` |
| `JWT__Secret` | `JWT:Secret` | Token signing key | `REPLACE_ME_JWT_SECRET_AT_LEAST_32_CHARS` — unique per environment |
| `JWT__ValidIssuer` | `JWT:ValidIssuer` | JWT issuer claim | Match public API URL, e.g. `https://api.REPLACE_ME.example` |
| `JWT__ValidAudience` | `JWT:ValidAudience` | JWT audience claim | Match SPA origin / audience string, e.g. `https://app.REPLACE_ME.example` |
| `Cors__AllowedOrigins` | `Cors:AllowedOrigins` | Browser CORS allow-list | Semicolon-separated SPA origin(s), e.g. `https://app.REPLACE_ME.example` — **no trailing slash, no `*`** (Task 3) |
| `ASPNETCORE_ENVIRONMENT` | hosting | Environment name | Must be `Production` (DevAdmin/DevUser seed is **off**) |

`Program.cs` throws at startup if `ConnectionStrings:DefaultConnection` or `JWT:Secret` is missing.

### CORS origins (Task 3 — Done)

| Env var | Status | Note |
| --- | --- | --- |
| `Cors__AllowedOrigins` | **Implemented** | Semicolon/comma list or indexed `Cors__AllowedOrigins__N`. Defaults to `http://localhost:4200`. Detail: [phase-14-cors](phase-14-cors.md). Do not use `*`. |

### Dev-only — omit in Production

These are read only when `ASPNETCORE_ENVIRONMENT=Development` (`Program.cs` seed block).

| Env var | Config key | Purpose |
| --- | --- | --- |
| `DevAdmin__Email` | `DevAdmin:Email` | Seed Admin account email |
| `DevAdmin__Password` | `DevAdmin:Password` | Seed Admin password (falls back to local default if unset in Dev) |
| `DevUser__Email` | `DevUser:Email` | Seed User account email |
| `DevUser__Password` | `DevUser:Password` | Seed User password (falls back to local default if unset in Dev) |

**Production:** leave unset. Do not put `DevAdmin__*` / `DevUser__*` in prod Compose.

### Compose / local Docker helpers (not API config keys)

Used by `docker-compose.yml` variable substitution — local Dev defaults only:

| Compose env | Maps to | Local default (example file only) |
| --- | --- | --- |
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | DB service + connection string build | Local placeholders in `.env.example` |
| `JWT_SECRET` | `JWT__Secret` | Weak Dev string — **replace for any shared host** |
| `JWT_VALID_ISSUER` / `JWT_VALID_AUDIENCE` | JWT issuer/audience | Localhost URLs |
| `CORS_ALLOWED_ORIGINS` | `Cors__AllowedOrigins` | Localhost SPA origin |
| `DEV_ADMIN_*` / `DEV_USER_*` | Dev seed | Dev only |
| `API_PORT` | Host port publish | `5055` |

Prod Compose override uses **distinct** `PROD_*` names (`PROD_JWT_SECRET`, `PROD_POSTGRES_PASSWORD`, …) so a Dev `.env` cannot silently satisfy Production — **Done** — [phase-14-compose](phase-14-compose.md).

### Optional non-secret config (`MatchResultSync`)

Public FIFA calendar/timeline URLs live in `appsettings.json` under `MatchResultSync`. Override via env if needed (`MatchResultSync__BaseUrl`, etc.) — **not required to boot** and not secrets.

### Inventory source (Task 2)

| Source | Keys read |
| --- | --- |
| `Program.cs` | `JWT:Secret`, `ConnectionStrings:DefaultConnection`, `JWT:ValidAudience`, `JWT:ValidIssuer`, `Cors:AllowedOrigins`, `DevAdmin:*`, `DevUser:*`, `MatchResultSync` section |
| `UserController` / `CompanyController` | Same JWT keys for token issue |
| `appsettings.json` | JWT issuer/audience defaults (localhost); `Cors:AllowedOrigins`; MatchResultSync; AllowedHosts |
| `appsettings.Development.json` | DevAdmin/DevUser emails |
| `docker-compose.yml` | Connection string, JWT, CORS, DevAdmin/DevUser, `ASPNETCORE_ENVIRONMENT=Development` |
| `.env.example` | Compose placeholders for local Dev + Production CORS comment |

### Operator checklist

1. Copy `WorldCup-System/.env.example` → `.env` for **local Docker only** (gitignored).
2. For Production, set the **required** table via host env / secret store — never commit values.
3. Confirm `ASPNETCORE_ENVIRONMENT=Production`.
4. Confirm no `DevAdmin__*` / `DevUser__*` on the prod host.
5. Set CORS origins to the real SPA origin (no trailing slash) — see [phase-14-cors](phase-14-cors.md).
6. Use prod Compose override instead of Dev Compose defaults — [phase-14-compose](phase-14-compose.md).

### Pointers

| Doc / file | Role |
| --- | --- |
| Workspace root `README.md` § Configuration | Quick table + link to this page |
| `WorldCup-System/.env.example` | Local Compose placeholders |
| [phase-14-planned Task 2](phase-14-planned.md#task-2--prod-secrets-inventory-p14-secrets) | Agent do/don't |
| Task 3 CORS | **Done** — [phase-14-cors](phase-14-cors.md) |
| Task 4 Compose | **Done** — [phase-14-compose](phase-14-compose.md) |
| Task 5 SPA prod | **Done** — [phase-14-spa-prod](phase-14-spa-prod.md) |

### Files changed (Task 2)

| Path | Location | Change |
| --- | --- | --- |
| `docs/changes/phase-14-secrets.md` | VitePress + git mirror | **New** — this page |
| `WorldCup-System/.env.example` | Nested git repo | Header + commented Production placeholders |
| `README.md` | Workspace root (**outside** nested git) | Production secrets inventory section |
| `docs/roadmap.md` | Dual | Task 2 `[x]` Done |
| `docs/index.md` | Dual | Phase 14 Tasks 1–5 Done; next Task 6 migrate/smoke |
| `docs/changes-review.md` | Dual | Latest + mapping + Review gate + per-file |
| `docs/changes/phase-14-planned.md` | Dual | Task 2 marked Done |
| `docs/changes/whole-project-upload-gate.md` | Dual | Historical packaging wording |
| `docs/workflow.md` | Dual | Phase 14 ask-before note |
| `docs/.vitepress/config.mts` | Dual | Sidebar: Secrets (Task 2) + planned |

Roadmap mapping: [Phase 14](../roadmap.md#phase-14) — `p14-secrets` **Done**.

### Diff snippets

#### `.env.example` (nested git)

```bash
# NEVER commit a real .env. NEVER put production passwords or JWT secrets here.
# Production inventory: docs/changes/phase-14-secrets.md

# --- Production (host / secret store — DO NOT uncomment with real values) ---
# ASPNETCORE_ENVIRONMENT=Production
# ConnectionStrings__DefaultConnection=Host=REPLACE_ME_DB_HOST;...
# JWT__Secret=REPLACE_ME_JWT_SECRET_AT_LEAST_32_CHARS
# JWT__ValidIssuer=https://api.REPLACE_ME.example
# JWT__ValidAudience=https://app.REPLACE_ME.example
# Omit DevAdmin__* / DevUser__* in Production.
# CORS origin env wiring: Phase 14 Task 3.
```

#### Workspace `README.md` (outside nested git)

```markdown
### Production secrets inventory

Canonical Production env-var table (placeholders only)…
- Detail: docs/changes/phase-14-secrets.md
- Local Docker Compose placeholders: WorldCup-System/.env.example
```

### Related

- [Phase 14 planned](phase-14-planned.md)
- [Roadmap Phase 14](../roadmap.md#phase-14)
- [Changes Review](../changes-review.md)
