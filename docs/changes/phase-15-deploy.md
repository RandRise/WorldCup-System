# WorldCup System — Phase 15 Task 2: Production Deploy

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 15 plan](phase-15-planned.md) · [Phase 14 Compose](phase-14-compose.md) · [Secrets](phase-14-secrets.md)

## Phase 15 Task 2 — Production deploy (`p15-deploy`)

Opened **3 Aug 2026**. Checklist: [Roadmap Phase 15](../roadmap.md#phase-15) — Task 2 **[x]** Done. Next: Task 3 **Done** — [phase-15-migrate-smoke](phase-15-migrate-smoke.md); then Task 4 data hygiene.

> **Success:** Local ship rehearsal brought API + Postgres up with `docker-compose.prod.yml` and session-only `PROD_*` secrets (never committed). `/health` returned **HTTP 200** `Healthy`. Both services reported healthy. Aligns with Task 1 SPA rehearsal at `http://localhost:5055`.

<a id="review-gate"></a>

## Review gate

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Prod Compose not actually run | **Critical** | **Cleared** | `up -d --build` succeeded; api + db healthy |
| `/health` not OK | **Critical** | **Cleared** | `http://localhost:5055/health` → 200 `Healthy` |
| Real secrets committed to git | **Critical** | **Cleared** | `PROD_*` set in shell session only |
| Dev `.env` keys used for prod | **High** | **Cleared** | Distinct `PROD_*` names |
| Dual docs VitePress ↔ git mirror | **High** | **Cleared this Markdown pass** | `docs/` and `WorldCup-System/docs/` |

**Gate verdict:** Task 2 **high/critical** items **Cleared**. Formal Phase 15 close remains Task 5 (`p15-gate`). Migrate/smoke remains Task 3.

---

## Locked approach

| Choice | Decision |
| --- | --- |
| Host | **Local ship rehearsal** (same machine as Task 1) — not a public cloud VM yet |
| Secrets | Random `PROD_*` in PowerShell session — never written to git |
| CORS / JWT | Issuer `http://localhost:5055`; audience + CORS `http://localhost:4200` |
| Compose | `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build` |

---

## What ran (3 Aug 2026)

| Step | Result |
| --- | --- |
| Docker Desktop + WSL | Installed; engine ready after reboot |
| `PROD_*` session env | Set (postgres user `worldcup`, random password/JWT ≥32 chars) |
| Image build | `worldcup-system-api` built |
| Containers | `worldcup-system-db-1` + `worldcup-system-api-1` **healthy** |
| Health | `GET http://localhost:5055/health` → **200** `Healthy` |

### Operator command (rehearsal)

```powershell
cd WorldCup-System
# Export PROD_* first (never commit values)
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
# Then: Invoke-WebRequest http://localhost:5055/health
```

Required env names: [phase-14-compose](phase-14-compose.md) · [phase-14-secrets](phase-14-secrets.md).

---

## Files changed

| Path | Change |
| --- | --- |
| `docs/changes/phase-15-deploy.md` | **New** — this page (dual) |
| Docs (dual) | Roadmap / index / changes-review / planned / VitePress |

No API/SPA source changes in Task 2 (ops-only).

Roadmap mapping: [Phase 15](../roadmap.md#phase-15) — `p15-deploy` **Done** `[x]`.

---

## Verify

| Check | Pass when | Evidence |
| --- | --- | --- |
| Health endpoint OK | HTTP 200 | `Healthy` from `:5055/health` |
| Prod Compose used | Override + `PROD_*` | Both containers healthy |
| No secrets in git | Placeholders only in `.env.example` | Session env only |

---

## Related

- [Phase 15 planned Task 2](phase-15-planned.md#task-2--production-deploy-p15-deploy)
- [Phase 14 Production Compose](phase-14-compose.md)
- [Phase 14 secrets](phase-14-secrets.md)
- [Phase 15 Task 1 SPA apiUrl](phase-15-spa-url.md)
- [Roadmap Phase 15](../roadmap.md#phase-15)
- [Changes Review](../changes-review.md)
