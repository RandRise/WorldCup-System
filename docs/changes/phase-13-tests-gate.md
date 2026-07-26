# WorldCup System — Phase 13 Task 6: Tests & Gate

## Docs navigation

[Dashboard](../index.md) | [System Overview](../overview.md) | [Completion Roadmap](../roadmap.md) | [Dev Workflow](../workflow.md) | [API Status](../api-status.md) | [Data Model](../data-model.md) | [Changes Review](../changes-review.md)

---
[← Changes Review](../changes-review.md) · [Phase 13 plan](phase-13-planned.md) · [Task 5 SPA](phase-13-spa.md) · [Task 4 leaderboard](phase-13-leaderboard-scope.md)

## Phase 13 Task 6 — Tests & gate (`p13-tests-gate`)

Opened **23 Jul 2026**. Checklist: [Roadmap Phase 13](../roadmap.md#phase-13).

> **Success:** **Phase 13 Done — Task 6 gate closed (23 Jul 2026).** Isolation unit + integration + CompanyController coverage; `dotnet test` suite **438** passed. Bugbot clean after SPA Join JWT session fix (manage actions gated on JWT roles). Checklist: [Roadmap Phase 13](../roadmap.md#phase-13).

### Review gate

High/critical items that must clear **before** declaring Phase 13 Done:

| Item | Severity | Status | Action |
| --- | --- | --- | --- |
| Two-company leaderboard leak (integration) | High (gate) | **Cleared** | `GetLeaderboard_TwoCompanies_NeverLeaksUsersAcrossCompanies` |
| Null company → empty board (integration) | High (gate) | **Cleared** | `GetLeaderboard_WhenUserHasNoCompany_ReturnsEmptyArray` |
| Anonymous GetLeaderboard → 401 | High (gate) | **Cleared** | `GetLeaderboard_WithoutJwt_ReturnsUnauthorized` |
| Bidirectional unit isolation | High (gate) | **Cleared** | `GetLeaderboard_Bidirectional_NeitherCompanyIncludesTheOther` |
| Invite uniqueness (service) | High (gate) | **Cleared** | `CreateAsync_GeneratesDistinctInviteCodes_AndDoesNotReuseExisting` |
| `dotnet test` green | High (gate) | **Cleared** | Suite **438** passed (0 failed) |
| Bugbot formal Task 6 review | High (gate) | **Cleared** | No bugs after Join JWT / `canManageCompany` fix |
| Markdown docs (this page + changes-review) | High (gate) | **Cleared** | Dual-docs mirror VitePress + git |
| Task 5 SPA interim Bugbot | High (phase) | **Cleared** | [phase-13-spa](phase-13-spa.md) |

### Locked decisions

| Choice | Decision |
| --- | --- |
| Isolation proof | HTTP integration via `WorldCupWebApplicationFactory` + DI seed (Testing has no DevAdmin) |
| Seed path | `UserManager` + `ApplicationDbContext` — companies, membership, Match/Bet/BetResult |
| Leak assertion | Board A names ⊆ Acme; Board B names ⊆ Globex; no cross names |
| Null company | Empty JSON array (not 400) — matches Task 4 API contract |
| Full phase close | Bugbot highs cleared + this suite green |

### Deliverables

| Item | Detail | Status |
| --- | --- | --- |
| Integration isolation | `LeaderboardIsolationIntegrationTests` (**3**) | **Done** |
| Unit bidirectional leak | `LeaderboardServiceTests` (+1 Task 6 case) | **Done** |
| Invite uniqueness unit | `CompanyServiceTests` (+1; **16** facts total file) | **Done** |
| Suite | **438** passed | **Done** |
| CompanyController unit | **17** facts | **Done** |
| Bugbot formal | Task 6 review | **Cleared** |
| Markdown docs | This page + changes-review dual mirror | **Cleared** |

### Acceptance (tests portion)

```text
cd WorldCup-System
dotnet test WorldCup-System.Tests\WorldCup-System.Tests.csproj
# expect: Passed!  … Total: 438
```

- [x] Two companies with resolved bets → each caller’s `/Bet/GetLeaderboard` omits the other company.
- [x] User with null `CompanyId` → empty array.
- [x] No JWT → 401.
- [x] Bugbot formal Task 6 + Task 5 interim → Phase 13 Done.

### Git file list (Task 6 tests)

Repo: `WorldCup-System` (branch `master`, uncommitted). From `git status` / working tree:

| Change | Path | Notes |
| --- | --- | --- |
| **New** (untracked) | `WorldCup-System.Tests/Integration/LeaderboardIsolationIntegrationTests.cs` | ~173 LOC; **3** Facts |
| Modified | `WorldCup-System.Tests/Services/Bets/LeaderboardServiceTests.cs` | **+201/−19** overall (Task 4 + Task 6 bidirectional) |
| **New** (untracked) | `WorldCup-System.Tests/Services/Companies/CompanyServiceTests.cs` | ~400 LOC; **16** Facts (includes invite uniqueness) |
| Docs | `docs/changes/phase-13-tests-gate.md` (this page) + roadmap / planned / changes-review / index | Dual mirror |

> **Note:** Broader Phase 13 working tree (Company entity/API, leaderboard scope, SPA) is documented on sibling pages. This page covers the **isolation / gate test slice** only.

### Diff snippets (working tree)

#### Integration — two-company never leaks

```csharp
[Fact]
public async Task GetLeaderboard_TwoCompanies_NeverLeaksUsersAcrossCompanies()
{
    // Seed Acme (Alice, Bob) + Globex (Eve) with BetResults…
    List<LeaderboardEntryDTO> boardA = await LoginAndGetLeaderboardAsync(acmeAliceEmail);
    List<LeaderboardEntryDTO> boardB = await LoginAndGetLeaderboardAsync(globexEveEmail);

    Assert.Contains(boardA, entry => entry.UserName == acmeAliceName);
    Assert.Contains(boardA, entry => entry.UserName == acmeBobName);
    Assert.DoesNotContain(boardA, entry => entry.UserName == globexEveName);

    Assert.Contains(boardB, entry => entry.UserName == globexEveName);
    Assert.DoesNotContain(boardB, entry => entry.UserName == acmeAliceName);
    Assert.DoesNotContain(boardB, entry => entry.UserName == acmeBobName);
}
```

#### Integration — null company + anonymous

```csharp
[Fact]
public async Task GetLeaderboard_WhenUserHasNoCompany_ReturnsEmptyArray()
{
    await SeedUserWithoutCompanyAsync(email, name);
    List<LeaderboardEntryDTO> leaderboard = await LoginAndGetLeaderboardAsync(email);
    Assert.Empty(leaderboard);
}

[Fact]
public async Task GetLeaderboard_WithoutJwt_ReturnsUnauthorized()
{
    HttpResponseMessage response = await _client.GetAsync("/Bet/GetLeaderboard");
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

#### Unit — bidirectional isolation

```csharp
[Fact]
public void GetLeaderboard_Bidirectional_NeitherCompanyIncludesTheOther()
{
    // Alice CompanyId=10 (3 pts), Eve CompanyId=99 (9 pts)
    List<LeaderboardEntryDTO> boardCompany10 = _leaderboardService.GetLeaderboard(10);
    List<LeaderboardEntryDTO> boardCompany99 = _leaderboardService.GetLeaderboard(99);

    Assert.Single(boardCompany10);
    Assert.Equal("Alice", boardCompany10[0].UserName);
    Assert.DoesNotContain(boardCompany10, entry => entry.UserName == "Eve");

    Assert.Single(boardCompany99);
    Assert.Equal("Eve", boardCompany99[0].UserName);
    Assert.DoesNotContain(boardCompany99, entry => entry.UserName == "Alice");
}
```

#### Unit — invite uniqueness

```csharp
[Fact]
public async Task CreateAsync_GeneratesDistinctInviteCodes_AndDoesNotReuseExisting()
{
    const string existingInvite = "EXISTING";
    SeedCompany(id: 1, inviteCode: existingInvite);
    // Create two companies → neither reuses EXISTING; codes differ
}
```

#### Seed path (integration)

```text
WorldCupWebApplicationFactory
  → UserManager.CreateAsync + AddToRoleAsync("User")
  → ApplicationDbContext.Companies + User.CompanyId
  → Match + Bet + BetResult
  → POST /User/Login → Bearer GET /Bet/GetLeaderboard
```

### Phase 13 status map

| Task id | Status | Detail |
| --- | --- | --- |
| docs | **Done** | [phase-13-planned](phase-13-planned.md) |
| `p13-company-model` | **Done** (interim closed) | [phase-13-company-model](phase-13-company-model.md) |
| `p13-company-api` | **Done** (interim closed) | [phase-13-company-api](phase-13-company-api.md) |
| `p13-leaderboard-scope` | **Done** (interim closed) | [phase-13-leaderboard-scope](phase-13-leaderboard-scope.md) |
| `p13-spa` | **Done** (interim Bugbot/Jasmine pending) | [phase-13-spa](phase-13-spa.md) |
| `p13-tests-gate` | **Tests cleared; Bugbot open** | This file |

### Related

- Next: formal Bugbot close of Task 6 (and Task 5 interim if still pending) → Phase 13 Done
- [Roadmap Phase 13](../roadmap.md#phase-13)
- [Changes Review](../changes-review.md)
- [Phase 13 plan](phase-13-planned.md)
