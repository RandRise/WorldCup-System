# WorldCup System — Phase 14 Task 3: CORS + JWT for Production

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 14 plan](phase-14-planned.md) · [Secrets inventory](phase-14-secrets.md)

## Phase 14 Task 3 — CORS + JWT for production (`p14-cors`)

Opened **27 Jul 2026**. Checklist: [Roadmap Phase 14](../roadmap.md#phase-14) — Task 3 **[x]** Done. Next: Task 4 Compose Done → Task 5 SPA prod.

> **Success:** Config-driven SPA CORS allow-list (env/`appsettings`). Dev default `http://localhost:4200` preserved. JWT issuer/audience remain env-overridable (`JWT__ValidIssuer` / `JWT__ValidAudience`) — documented, not new code. No `AllowAnyOrigin` / `*`.

<a id="review-gate"></a>

## Review gate

High/critical items that must clear **before** testing or treating Task 3 as closed:

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| `AllowAnyOrigin` + credentials (browser credentialed CORS open to any origin) | Critical | **Cleared by design** | Explicit `WithOrigins(corsOrigins)` only; `*` rejected at resolve |
| Production override path missing (still hardcoded localhost) | High | **Cleared** | `Cors__AllowedOrigins` semicolon/comma string or indexed `Cors__AllowedOrigins__N` |
| Env scalar `Cors__AllowedOrigins` ignored when JSON array children exist | High | **Cleared (Bugbot)** | Resolver prefers `section.Value` over indexed children; regression test added |
| Dev localhost broken after change | High | **Cleared** | `appsettings.json` array + resolver fallback + Compose `${CORS_ALLOWED_ORIGINS:-http://localhost:4200}` |
| Policy name mismatch (`AddPolicy` ≠ `UseCors`) | High | **Cleared** | Both use `CorsOriginsResolver.PolicyName` (`"Spa"`) |
| JWT issuer/audience not overridable for real SPA host | High | **Cleared** | Already config-driven; documented with CORS (no new JWT code) |
| Dual docs VitePress ↔ git mirror | High | **Cleared this pass** | `docs/` and `WorldCup-System/docs/` |
| Trailing-slash origin mismatch (browser Origin vs allow-list) | Medium | **Cleared** | Resolver strips trailing `/`; docs say no slash |
| Unit + integration coverage for allow/deny / `*` | High (before formal gate) | **Cleared** | `CorsOriginsResolverTests` + `CorsIntegrationTests`; suite **462** passed |

**Gate verdict:** Bugbot high (env scalar ignored under JSON array merge) **fixed**; re-review clean. Suite **462** passed. Task 4 Production Compose **Done** — [phase-14-compose](phase-14-compose.md).

---

## Behavior

### CORS

| Source | How to set |
| --- | --- |
| `appsettings.json` | `"Cors": { "AllowedOrigins": [ "http://localhost:4200" ] }` |
| Env (semicolon/comma list) | `Cors__AllowedOrigins=https://app.example.com;https://www.example.com` |
| Env (indexed) | `Cors__AllowedOrigins__0=https://app.example.com` |
| Compose (local) | `CORS_ALLOWED_ORIGINS` → `Cors__AllowedOrigins` |

Rules:

- Scheme + host [+ port], **no trailing slash** (slash is stripped if present).
- Do **not** use `*`.
- Empty / missing config → `http://localhost:4200`.
- Middleware order unchanged: `UseCors` before `UseAuthentication` / `UseAuthorization`.

Policy name: **`Spa`** (replaces hardcoded `AngularDev` + fixed origin).

### JWT (already configurable — document only)

| Env var | Config key | Prod note |
| --- | --- | --- |
| `JWT__ValidIssuer` | `JWT:ValidIssuer` | Match public API URL |
| `JWT__ValidAudience` | `JWT:ValidAudience` | Match SPA origin / audience string |
| `JWT__Secret` | `JWT:Secret` | ≥32 chars; required at startup |

Token issue (`UserController` / `CompanyController`) and validation (`Program.cs`) both read the same keys. No JWT code changes in Task 3.

---

## Git status (Task 3 slice)

From nested repo `WorldCup-System/` (focus: CORS/JWT wiring; docs churn may also be dirty):

```
 M .env.example
 M WorldCup-System/Program.cs
 M WorldCup-System/appsettings.json
 M docker-compose.yml
?? WorldCup-System/Configuration/
?? WorldCup-System.Tests/Configuration/
?? WorldCup-System.Tests/Integration/CorsIntegrationTests.cs
?? docs/changes/phase-14-cors.md
?? README.md
```

Workspace (outside nested git): root `README.md` may note CORS; VitePress `docs/` mirrors under workspace root.

---

## Files changed

| Path | Change |
| --- | --- |
| `WorldCup-System/Configuration/CorsOriginsResolver.cs` | **New** — resolve + normalize origins; policy `Spa`; reject `*` |
| `WorldCup-System/Program.cs` | Config-driven `Spa` CORS policy; `UseCors(PolicyName)` |
| `WorldCup-System/appsettings.json` | `Cors:AllowedOrigins` default array |
| `docker-compose.yml` | `Cors__AllowedOrigins` from `CORS_ALLOWED_ORIGINS` |
| `.env.example` | Local `CORS_ALLOWED_ORIGINS` + Production `Cors__AllowedOrigins` placeholders |
| `WorldCup-System.Tests/Configuration/CorsOriginsResolverTests.cs` | Unit cases (array, semicolon, comma, slash, `*`, default) |
| `WorldCup-System.Tests/Integration/CorsIntegrationTests.cs` | Allow / deny Origin on `/health` |
| Workspace `README.md` | Configuration / CORS note (if present) |
| Docs (dual) | This page + roadmap / index / changes-review / planned / secrets / VitePress |

Roadmap mapping: [Phase 14](../roadmap.md#phase-14) — `p14-cors` **Done** `[x]`.

---

## Diff snippets

### `CorsOriginsResolver.cs` (NEW)

```csharp
public static class CorsOriginsResolver
{
    public const string PolicyName = "Spa";
    public const string DefaultOrigin = "http://localhost:4200";

    public static string[] Resolve(IConfiguration configuration)
    {
        // JSON array children OR semicolon/comma string Value
        // Trim; TrimEnd('/'); reject "*"; Distinct ignore-case
        // Empty → DefaultOrigin
    }
}
```

### `Program.cs` (MOD)

```csharp
using WorldCup_System.Configuration;

string[] corsOrigins = CorsOriginsResolver.Resolve(configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsOriginsResolver.PolicyName, policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// … later …
app.UseCors(CorsOriginsResolver.PolicyName);
app.UseAuthentication();
```

### `appsettings.json` (MOD)

```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:4200"
  ]
}
```

### `docker-compose.yml` (MOD)

```yaml
Cors__AllowedOrigins: ${CORS_ALLOWED_ORIGINS:-http://localhost:4200}
```

### `.env.example` (MOD)

```bash
# Semicolon-separated SPA origins (no trailing slash). Maps to Cors__AllowedOrigins.
CORS_ALLOWED_ORIGINS=http://localhost:4200

# Production (commented placeholders):
# Cors__AllowedOrigins=https://app.REPLACE_ME.example
# Multiple: …;https://www.REPLACE_ME.example
# Or indexed: Cors__AllowedOrigins__0=…
# Origin form: scheme + host [+ port], NO trailing slash. Do not use *.
```

### Tests (NEW)

- `CorsOriginsResolverTests` — missing → default; JSON array; `;` / `,` split; trailing slash strip; case-insensitive dedupe; `*` throws; empty entries ignored; `PolicyName == "Spa"`.
- `CorsIntegrationTests` — `Origin: http://localhost:4200` → `Access-Control-Allow-Origin`; `https://evil.example` not reflected.

---

## Verify

| Check | Pass |
| --- | --- |
| Dev still works with localhost:4200 | Default preserved |
| Prod origin settable via env | `Cors__AllowedOrigins` |
| JWT issuer/audience override | `JWT__ValidIssuer` / `JWT__ValidAudience` |
| No `*` | Throws at resolve |
| Policy `Spa` wired end-to-end | `AddPolicy` + `UseCors` share `PolicyName` |

### Operator snippet (Production)

```bash
export ASPNETCORE_ENVIRONMENT=Production
export JWT__ValidIssuer=https://api.example.com
export JWT__ValidAudience=https://app.example.com
export Cors__AllowedOrigins=https://app.example.com
```

---

## Related

- [Phase 14 planned Task 3](phase-14-planned.md#task-3--cors--jwt-for-production-p14-cors)
- [Phase 14 secrets](phase-14-secrets.md)
- [Roadmap Phase 14](../roadmap.md#phase-14)
- [Changes Review](../changes-review.md)
