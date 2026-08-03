# WorldCup System — Phase 15 Task 1: SPA production `apiUrl`

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 15 plan](phase-15-planned.md) · [Phase 14 SPA prod](phase-14-spa-prod.md)

## Phase 15 Task 1 — SPA production `apiUrl` (`p15-spa-url`)

Opened **3 Aug 2026**. Checklist: [Roadmap Phase 15](../roadmap.md#phase-15) — Task 1 **[x]** Done. Next: Task 2 **Done** — [phase-15-deploy](phase-15-deploy.md); then Task 3 migrate/smoke.

> **Success:** Production `environment.ts` no longer embeds `REPLACE_ME`. Chosen host for v1 is a **local ship rehearsal** URL so operators can learn the deploy loop without a public domain. Dev `environment.development.ts` unchanged.

<a id="review-gate"></a>

## Review gate

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Prod `apiUrl` still `REPLACE_ME` | **Critical** | **Cleared** | `environment.ts` → `http://localhost:5055` |
| Silent / undocumented localhost in prod | **High** | **Cleared by design** | Documented local rehearsal; swap when public host exists |
| Dev `ng serve` broken | **High** | **Cleared** | `environment.development.ts` still `http://localhost:5055` |
| Secrets in SPA env | **Critical** | **Cleared** | Public API origin only — no JWT/DB secrets |
| Dual docs VitePress ↔ git mirror | **High** | **Cleared this Markdown pass** | `docs/` and `WorldCup-System/docs/` identical |

**Gate verdict:** Task 1 **high/critical** items **Cleared**. Formal Phase 15 close remains Task 5 (`p15-gate`).

---

## Locked approach

| Choice | Decision |
| --- | --- |
| Host for Task 1 | **Local ship rehearsal:** `http://localhost:5055` (Compose/API publish port used in Dev) |
| Why not wait for a domain | Phase 15 goal includes learning deploy; no public host required for Tasks 1–3 rehearsal |
| Later public ship | Replace `apiUrl` with the real API origin, align CORS/JWT, rebuild SPA |
| Dev | Unchanged — `environment.development.ts` + `ng serve` |

---

## Chosen host

| Environment | `apiUrl` | Notes |
| --- | --- | --- |
| Production build (`environment.ts`) | `http://localhost:5055` | Local rehearsal — **not** a cloud URL |
| Development (`environment.development.ts`) | `http://localhost:5055` | Unchanged |

When moving off localhost, also update:

| Concern | Must match |
| --- | --- |
| SPA `apiUrl` | Public API origin |
| `JWT__ValidIssuer` | Same public API URL (typical) |
| `Cors__AllowedOrigins` / `PROD_CORS_ALLOWED_ORIGINS` | SPA origin (static host), **not** the API URL |
| `JWT__ValidAudience` | SPA audience / origin string |

---

## Files changed

| Path | Change |
| --- | --- |
| `worldcup-client/src/environments/environment.ts` | `apiUrl` → local rehearsal; comment points at this page |
| `worldcup-client/README.md` | Production section reflects rehearsal URL |
| `docs/changes/phase-15-spa-url.md` | This page (dual) |
| Docs (dual) | Roadmap / index / changes-review / planned / VitePress |

Roadmap mapping: [Phase 15](../roadmap.md#phase-15) — `p15-spa-url` **Done** `[x]`.

---

## Verify

| Check | Pass when | Evidence |
| --- | --- | --- |
| Production env has no `REPLACE_ME` | Grep clean in `environment.ts` | `apiUrl: 'http://localhost:5055'` |
| Dev env still localhost / Dev API | Unchanged | `environment.development.ts` |
| Chosen host documented | This page | [Chosen host](#chosen-host) |

---

## Related

- [Phase 15 planned Task 1](phase-15-planned.md#task-1--spa-production-apiurl-p15-spa-url)
- [Phase 14 SPA production build](phase-14-spa-prod.md)
- [Phase 14 CORS](phase-14-cors.md)
- [Roadmap Phase 15](../roadmap.md#phase-15)
- [Changes Review](../changes-review.md)
