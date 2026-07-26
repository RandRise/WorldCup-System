using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Core.DTOs.Bets;
using Core.DTOs.Users;
using Data.Context;
using Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MatchEntity = Data.Entities.Match;

namespace WorldCup_System.Tests.Integration
{
    /// <summary>
    /// Phase 13 Task 6 — company-scoped leaderboard isolation (integration).
    /// DevAdmin is not seeded in Testing; seed via DI (UserManager + ApplicationDbContext).
    /// </summary>
    public class LeaderboardIsolationIntegrationTests : IClassFixture<WorldCupWebApplicationFactory>
    {
        private const string Password = "TestPass1!";

        private readonly WorldCupWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public LeaderboardIsolationIntegrationTests(WorldCupWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task GetLeaderboard_TwoCompanies_NeverLeaksUsersAcrossCompanies()
        {
            string suffix = Guid.NewGuid().ToString("N");
            string acmeAliceEmail = $"acme-alice-{suffix}@example.com";
            string acmeBobEmail = $"acme-bob-{suffix}@example.com";
            string globexEveEmail = $"globex-eve-{suffix}@example.com";
            string acmeAliceName = $"AcmeAlice-{suffix}";
            string acmeBobName = $"AcmeBob-{suffix}";
            string globexEveName = $"GlobexEve-{suffix}";

            await SeedTwoCompanyScenarioAsync(
                acmeAliceEmail,
                acmeAliceName,
                acmeBobEmail,
                acmeBobName,
                globexEveEmail,
                globexEveName);

            List<LeaderboardEntryDTO> boardA = await LoginAndGetLeaderboardAsync(acmeAliceEmail);
            List<LeaderboardEntryDTO> boardB = await LoginAndGetLeaderboardAsync(globexEveEmail);

            Assert.Contains(boardA, entry => entry.UserName == acmeAliceName);
            Assert.Contains(boardA, entry => entry.UserName == acmeBobName);
            Assert.DoesNotContain(boardA, entry => entry.UserName == globexEveName);

            Assert.Contains(boardB, entry => entry.UserName == globexEveName);
            Assert.DoesNotContain(boardB, entry => entry.UserName == acmeAliceName);
            Assert.DoesNotContain(boardB, entry => entry.UserName == acmeBobName);
        }

        [Fact]
        public async Task GetLeaderboard_WhenUserHasNoCompany_ReturnsEmptyArray()
        {
            string suffix = Guid.NewGuid().ToString("N");
            string email = $"solo-{suffix}@example.com";
            string name = $"Solo-{suffix}";

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

        private async Task SeedTwoCompanyScenarioAsync(
            string acmeAliceEmail,
            string acmeAliceName,
            string acmeBobEmail,
            string acmeBobName,
            string globexEveEmail,
            string globexEveName)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            UserManager<User> userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            Company acme = new Company
            {
                Name = $"Acme-{Guid.NewGuid():N}",
                InviteCode = $"A{Guid.NewGuid():N}"[..8].ToUpperInvariant(),
                CreatedAt = DateTime.UtcNow
            };
            Company globex = new Company
            {
                Name = $"Globex-{Guid.NewGuid():N}",
                InviteCode = $"G{Guid.NewGuid():N}"[..8].ToUpperInvariant(),
                CreatedAt = DateTime.UtcNow
            };
            dbContext.Companies.Add(acme);
            dbContext.Companies.Add(globex);
            await dbContext.SaveChangesAsync();

            User alice = await CreateUserAsync(userManager, acmeAliceEmail, acmeAliceName);
            User bob = await CreateUserAsync(userManager, acmeBobEmail, acmeBobName);
            User eve = await CreateUserAsync(userManager, globexEveEmail, globexEveName);

            alice.CompanyId = acme.Id;
            bob.CompanyId = acme.Id;
            eve.CompanyId = globex.Id;
            IdentityResult aliceUpdate = await userManager.UpdateAsync(alice);
            IdentityResult bobUpdate = await userManager.UpdateAsync(bob);
            IdentityResult eveUpdate = await userManager.UpdateAsync(eve);
            Assert.True(aliceUpdate.Succeeded);
            Assert.True(bobUpdate.Succeeded);
            Assert.True(eveUpdate.Succeeded);

            MatchEntity match = new MatchEntity
            {
                Date = DateTime.UtcNow,
                StadiumId = 1
            };
            dbContext.Matches.Add(match);
            await dbContext.SaveChangesAsync();

            Bet aliceBet = new Bet { UserId = alice.Id, MatchId = match.Id, IsDraw = true };
            Bet bobBet = new Bet { UserId = bob.Id, MatchId = match.Id, IsDraw = true };
            Bet eveBet = new Bet { UserId = eve.Id, MatchId = match.Id, IsDraw = true };
            dbContext.Bets.AddRange(aliceBet, bobBet, eveBet);
            await dbContext.SaveChangesAsync();

            dbContext.BetResults.AddRange(
                new BetResult { BetId = aliceBet.Id, Point = 3 },
                new BetResult { BetId = bobBet.Id, Point = 1 },
                new BetResult { BetId = eveBet.Id, Point = 9 });
            await dbContext.SaveChangesAsync();
        }

        private async Task SeedUserWithoutCompanyAsync(string email, string name)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            UserManager<User> userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            await CreateUserAsync(userManager, email, name);
        }

        private static async Task<User> CreateUserAsync(UserManager<User> userManager, string email, string name)
        {
            User user = new User
            {
                Email = email,
                UserName = email,
                Name = name,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            IdentityResult createResult = await userManager.CreateAsync(user, Password);
            Assert.True(createResult.Succeeded, string.Join("; ", createResult.Errors.Select(error => error.Description)));

            IdentityResult roleResult = await userManager.AddToRoleAsync(user, "User");
            Assert.True(roleResult.Succeeded, string.Join("; ", roleResult.Errors.Select(error => error.Description)));

            return user;
        }

        private async Task<List<LeaderboardEntryDTO>> LoginAndGetLeaderboardAsync(string email)
        {
            LoginModel loginModel = new LoginModel
            {
                Email = email,
                Password = Password
            };

            HttpResponseMessage loginResponse = await _client.PostAsJsonAsync("/User/Login", loginModel);
            loginResponse.EnsureSuccessStatusCode();

            JsonElement loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(loginBody.TryGetProperty("token", out JsonElement tokenElement));
            string token = tokenElement.GetString() ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(token));

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/Bet/GetLeaderboard");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            List<LeaderboardEntryDTO>? leaderboard =
                await response.Content.ReadFromJsonAsync<List<LeaderboardEntryDTO>>();
            Assert.NotNull(leaderboard);
            return leaderboard;
        }
    }
}
