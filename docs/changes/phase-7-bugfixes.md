# WorldCup System — Phase 7 Bugfixes & Live Data

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md)

## Phase 7 — Post-Launch Bugfixes & Live Data

Auth session, live refresh, leaderboard auto-resolve (8 Jul), ApiHttp text mutations (9 Jul), knockout bracket + H2H (13–16 Jul), offline WC 2026 data (15–17 Jul). [← Back to Changes Review](../changes-review.md) · [Roadmap Phase 7](../roadmap.md#phase-7) · [WC 2026 data](wc2026-live-data.md) · [Phase 8 (same tree)](phase-8-dashboards.md)

> **Success:** **Phase 7 complete (16 Jul 2026).** Knockout bracket + H2H standings + offline WC 2026 import are implemented (still uncommitted with Phase 8). External API cancelled. Combined Phase 7+8 gate: `dotnet test` — **245 passed** (16 Jul). Earlier auth/live suite alone was **227 passed** (8 Jul).

> **Info:** **Historical (8 Jul 2026).** Restart the API so any remaining demo-seed backfill can run on an existing database. Demo CSV seed was later removed — prefer offline scripts under `scripts/`.

> **Success:** **Follow-up fix (9 Jul 2026).** Admin mutations (add/delete match, team, bet, etc.) showed UI errors while the API returned `200 OK` with `text/plain` bodies like `Match added successfully.` — fixed via `ApiHttpService` (`responseType: 'text'`).

### Review Gate — Cleared

| Blocker | Severity | Status | Resolution |
| --- | --- | --- | --- |
| Fixtures poll missed Scheduled→Live | High | Cleared | `shouldAutoRefresh` polls Live or due Scheduled kickoffs |
| Seed AlreadySeeded skipped goals/resolve | Medium | Cleared | `BackfillGoalsForFinishedMatchesAsync` + resolve on re-seed |
| `dotnet test` (auth/live, 8 Jul) | High | Cleared | 227 passed |
| Combined Phase 7+8 `dotnet test` | High | Cleared | 245 passed (16 Jul) |
| Uncommitted Phase 7 paths | Medium | Ops | Commit when you ask |

### Root causes

| Symptom | Cause | Fix |
| --- | --- | --- |
| Logged in but navbar shows “Log in”; My Bets hits login guard | `POST /User/Login` OK but `GET /User/GetMe` failed — JWT claims / Identity cookie defaults overwrote Bearer scheme; email claim mapping incomplete under .NET 8 | `CurrentUserResolver`; login emits `sub`, `NameIdentifier`, email claims; JWT registered **after** `AddIdentity`; `MapInboundClaims = true` |
| Standings / fixtures empty for World Cup 2026 | Demo seed had teams only; client could prefer wrong 2026 (or 2022) tournament | `ImportGroupStageFixturesAsync` (72 matches + goals); prefer June 2026 in `worldcup-context.service.ts` |
| Leaderboard empty / only after manual resolve | No auto-resolve when match finished; leaderboard skipped when no `BetResult` rows | `GoalService` + seed call `ResolveBetsForMatch`; include active bettors at 0 pts |
| Fixtures bet map N+1 API calls | One `GetMyBetForMatch` per fixture | `GetMyBetsForWorldCup` batch endpoint + client single call; 30s live poll |
| Admin add/delete match (and other mutations) shows “Failed to save/delete” but network tab shows success | API returns `200 OK` with `Content-Type: text/plain` (e.g. `Match added successfully.`); Angular `HttpClient` defaults to JSON parsing and throws `Http failure during parsing` | `ApiHttpService.postCommand` / `deleteCommand` with `responseType: 'text'`; all mutation API services updated (match, team, coach, player, stadium, bet, register) |

### Phase 7 Roadmap Mapping

| Task | Status | Evidence |
| --- | --- | --- |
| `p7-auth-session` — JWT session + GetMe | Done | `CurrentUserResolver.cs`, `UserController` claims, `Program.cs` JWT after Identity, `AuthIntegrationTests` Bearer → GetMe |
| `p7-my-bets-guard` — My Bets redirect to login | Done | Same auth root cause; GetMe/resolver fix unblocks `authGuard` |
| `p7-wc2026-select` — Prefer June 2026 | Done | `worldcup-context.service.ts` — UTC year 2026 + month 5 |
| `p7-fixtures-seed` — WC 2026 groups + finished schedule | Done | Offline `scripts/seed-wc2026-groups.sql` + `import_wc2026_finished_matches.py` (groups A–L, finished through both SFs, `RoundOf32`). Earlier DemoSeed CSV path removed. |
| `p7-live-refresh` — Auto-refresh live fixtures | Done | `fixtures.component.ts` — 30s interval while any match `Live`; cleaned up on destroy |
| `p7-leaderboard-auto` — Auto-resolve + show bettors | Done | `GoalService`, `ResolveFinishedBetsAsync`, `LeaderboardService` zero-point inclusion |
| `p7-external-api` — External football API | Cancelled | Manual offline import + Admin live console (16 Jul 2026) |
| `p7-knockout` — Knockout bracket | Done | `KnockoutService` / `KnockoutController`; `MatchStage` + feeders; `StandingsService` H2H; SPA `/bracket` + hub GenerateBracket |

### Git status snapshot

**Repo:** `WorldCup-System/` — shared uncommitted tree with Phase 8 (audit 17 Jul 2026): **64 tracked** changes + **45 untracked** · tracked **+2716 / −6205** (HTML docs → Markdown).

**Phase 7 knockout / data paths in that tree:**

Earlier 8 Jul auth-only snapshot (18 modified / CurrentUserResolver) is historical — those pieces are committed or superseded. Full Phase 8 listing: [phase-8-dashboards](phase-8-dashboards.md).

### Review Gate — Knockout / live-data follow-up

| Blocker | Severity | Status | Action |
| --- | --- | --- | --- |
| Combined Phase 7+8 `dotnet test` | High | Cleared | 245 passed (16 Jul) — includes knockout + H2H tests |
| `AddMatchStage` migration + Feeder* repair | Medium | Ops | Apply on next API start; `Program.cs` IF NOT EXISTS covers stuck local DBs |
| Final result (19 Jul) | Low | Ops | Schedule via `add_sf2_and_final.py`; record FT after match (both SFs already in import script) |

### Architecture — Auth & seed flow

### File-by-file detail

#### Core/Helpers/CurrentUserResolver.cs


**[p7-auth-session · NEW]**

Shared identity resolution for controllers. Prefers `NameIdentifier` / JWT `sub`; falls back to email claim + `UserManager.FindByEmailAsync`.

#### WorldCup-System/Controllers/UserController.cs


**[p7-auth-session]**

Login token now includes `sub`, JWT email, and `NameIdentifier`. `ResolveCurrentUserIdAsync` delegates to `CurrentUserResolver`.

#### WorldCup-System/Program.cs


**[p7-auth-session]**

JWT Bearer registration moved **after** `AddIdentity` so cookie defaults do not overwrite API schemes. Enables `MapInboundClaims`. Skips HTTPS redirection in Testing env (fixes integration redirect noise).

#### WorldCup-System/Controllers/BetController.cs


**[p7-leaderboard-auto · auth]**

New authorized `GET GetMyBetsForWorldCup?worldCupId=`. Uses shared resolver + `ApiErrorHelper`.

#### Core/Services/Bets/BetService.cs + IBetService.cs


**[p7-leaderboard-auto]**

Batch user bets scoped to tournament: groups → teams → matches where both sides are in the cup.

#### Core/Services/Bets/LeaderboardService.cs


**[p7-leaderboard-auto]**

Removed early empty return when no results. Appends scoped bettors with 0 points / 0 resolved, then re-ranks.

#### Core/Services/Goals/GoalService.cs


**[p7-leaderboard-auto]**

Injects `IBetService`. After creating a goal, if match is past full-time duration, attempts `ResolveBetsForMatch` (swallows incomplete-stats failures).

#### Core/Services/Seeding/DemoSeedService.cs + DemoSeedResultDTO.cs


**[p7-fixtures-seed · p7-leaderboard-auto]**

Full group-stage calendar (6 pairings × 12 groups), placeholder scorers, deterministic demo scores for finished matches, then resolve bets. “Already seeded” now also requires expected match count. DTO gains `MatchesAdded`, `GoalsAdded`, `BetsResolved`.

#### worldcup-client — bet-api / context / fixtures / standings


**[p7-wc2026-select · p7-live-refresh]**

Batch bets API; prefer June 2026 tournament; 30s live refresh with `OnDestroy` cleanup; standings empty copy clarified.

#### worldcup-client — api-http.service.ts


**[mutation response fix · NEW (9 Jul 2026)]**

Centralizes POST/DELETE commands that return plain-text success strings from the API. Prevents false UI errors when mutations succeed but Angular cannot JSON-parse `text/plain` bodies.

#### Tests (Bet / Goal / DemoSeed / Auth / BetController)


**[coverage]**

Mocks updated for new DI (`IBetService`, Group/Match repos). New facts for world-cup bets + controller. Auth integration disables auto-redirect and asserts Bearer → GetMe profile.

#### scripts/run-api.ps1


**[ops · NEW]**

Dev helper: ensure local Postgres service, free port 5055 (force-stop listeners), then `dotnet run` on the API project.

### Knockout / standings detail (uncommitted)

#### KnockoutService + KnockoutController (NEW)


**[p7-knockout]**

Get bracket by world cup; admin generate bracket from standings; advance winner/loser into feeder slots after FT.

#### MatchStage + AddMatchStage migration (NEW)


**[p7-knockout · p7-fixtures-seed]**

Stage enum including `RoundOf32`; nullable team FKs; feeder match IDs and loser flags; indexes.

#### StandingsService — head-to-head


**[p7-knockout]**

After points / GD / GF, apply FIFA-style H2H among tied clusters.

#### SPA bracket + knockout-api (NEW)


**[p7-knockout]**

Public `/bracket` view; admin hub can call GenerateBracket.

### Still to do (ops only)

- Schedule / record Final (Argentina vs Spain, 19 Jul) via `add_sf2_and_final.py` or Admin live console
- Apply `AddMatchStage` + Feeder* repair on next API start
- Commit knockout + WC 2026 scripts + Phase 8 SPA + Markdown docs when asked

Phase 8 product tasks are implemented — see [phase-8-dashboards](phase-8-dashboards.md). Roadmap `p7-knockout` is **done** (no longer deferred).
