using System.Security.Claims;
using Core.DTOs.Bets;
using Core.Services.Bets;
using Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    public class BetControllerTests
    {
        private readonly Mock<IBetService> _betServiceMock;
        private readonly Mock<ILeaderboardService> _leaderboardServiceMock;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly BetController _betController;

        public BetControllerTests()
        {
            _betServiceMock = new Mock<IBetService>();
            _leaderboardServiceMock = new Mock<ILeaderboardService>();
            _userManagerMock = CreateUserManagerMock();

            _betController = new BetController(
                _betServiceMock.Object,
                _leaderboardServiceMock.Object,
                _userManagerMock.Object);

            _betController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public void GetScoringRules_ReturnsRulesFromService()
        {
            BettingRulesResponseDTO rules = new BettingRulesResponseDTO
            {
                Summary = "Test summary",
                Rules = new List<BettingRuleDTO>()
            };

            _betServiceMock.Setup(betService => betService.GetScoringRules()).Returns(rules);

            BettingRulesResponseDTO result = _betController.GetScoringRules();

            Assert.Equal("Test summary", result.Summary);
        }

        [Fact]
        public void GetLeaderboard_ReturnsLeaderboardFromService()
        {
            List<LeaderboardEntryDTO> entries = new List<LeaderboardEntryDTO>
            {
                new LeaderboardEntryDTO { Rank = 1, UserId = 1, TotalPoints = 9 }
            };

            _leaderboardServiceMock.Setup(leaderboardService => leaderboardService.GetLeaderboard(null)).Returns(entries);

            List<LeaderboardEntryDTO> result = _betController.GetLeaderboard();

            Assert.Single(result);
            Assert.Equal(9, result[0].TotalPoints);
        }

        [Fact]
        public async Task PlaceBet_WhenAuthenticated_CallsServiceWithUserId()
        {
            User user = new User { Id = 42, Email = "user@test.com", UserName = "user@test.com", Name = "User" };
            SetUserClaims(new Claim(ClaimTypes.Email, "user@test.com"));
            _userManagerMock.Setup(userManager => userManager.FindByEmailAsync("user@test.com")).ReturnsAsync(user);

            PlaceBetDTO placeBetDto = new PlaceBetDTO { MatchId = 1, IsDraw = false, TeamId = 10 };

            IActionResult actionResult = await _betController.PlaceBet(placeBetDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            _betServiceMock.Verify(betService => betService.PlaceBet(42, placeBetDto), Times.Once);
            Assert.Equal("Bet placed successfully.", okResult.Value);
        }

        [Fact]
        public async Task GetMyBets_WhenAuthenticated_ReturnsUserBets()
        {
            User user = new User { Id = 42, Email = "user@test.com", UserName = "user@test.com", Name = "User" };
            SetUserClaims(new Claim(ClaimTypes.Email, "user@test.com"));
            _userManagerMock.Setup(userManager => userManager.FindByEmailAsync("user@test.com")).ReturnsAsync(user);

            List<BetDTO> bets = new List<BetDTO>
            {
                new BetDTO { Id = 1, UserId = 42, MatchId = 1, IsDraw = false, PredictedTeamId = 10, PredictedOutcome = "Brazil" }
            };
            _betServiceMock.Setup(betService => betService.GetUserBets(42)).Returns(bets);

            IActionResult actionResult = await _betController.GetMyBets();

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            List<BetDTO> result = Assert.IsType<List<BetDTO>>(okResult.Value);
            Assert.Single(result);
        }

        [Fact]
        public async Task ResolveBetsForMatch_WhenServiceSucceeds_ReturnsResolveResultDto()
        {
            ResolveBetsResultDTO resolveResult = new ResolveBetsResultDTO
            {
                MatchId = 1,
                ResolvedCount = 3,
                Message = "3 bet(s) resolved.",
                UserBreakdown = new List<BetResolveUserDTO>
                {
                    new BetResolveUserDTO
                    {
                        UserId = 42,
                        UserName = "Alice",
                        PredictedOutcome = "Brazil",
                        PointsAwarded = 3,
                        PreviousPoints = null
                    }
                }
            };
            _betServiceMock.Setup(betService => betService.ResolveBetsForMatch(1))
                .ReturnsAsync(resolveResult);

            IActionResult actionResult = await _betController.ResolveBetsForMatch(1);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            ResolveBetsResultDTO result = Assert.IsType<ResolveBetsResultDTO>(okResult.Value);
            Assert.Equal(1, result.MatchId);
            Assert.Equal(3, result.ResolvedCount);
            Assert.Equal("3 bet(s) resolved.", result.Message);
            Assert.Single(result.UserBreakdown);
            Assert.Equal(42, result.UserBreakdown[0].UserId);
            Assert.Equal(3, result.UserBreakdown[0].PointsAwarded);
        }

        [Fact]
        public async Task ResolveBetsForMatch_WhenServiceThrows_ReturnsBadRequest()
        {
            _betServiceMock
                .Setup(betService => betService.ResolveBetsForMatch(1))
                .ThrowsAsync(new InvalidOperationException("Match has not finished yet."));

            IActionResult actionResult = await _betController.ResolveBetsForMatch(1);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("not finished", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task ResolveBetsForWorldCup_WhenServiceSucceeds_ReturnsWorldCupResultDto()
        {
            ResolveBetsWorldCupResultDTO worldCupResult = new ResolveBetsWorldCupResultDTO
            {
                WorldCupId = 2026,
                MatchesProcessed = 2,
                TotalResolvedCount = 5,
                Message = "5 bet(s) resolved across 2 match(es).",
                MatchResults = new List<ResolveBetsResultDTO>
                {
                    new ResolveBetsResultDTO { MatchId = 1, ResolvedCount = 3, Message = "3 bet(s) resolved." },
                    new ResolveBetsResultDTO { MatchId = 2, ResolvedCount = 2, Message = "2 bet(s) resolved." }
                }
            };
            _betServiceMock.Setup(betService => betService.ResolveBetsForWorldCup(2026))
                .ReturnsAsync(worldCupResult);

            IActionResult actionResult = await _betController.ResolveBetsForWorldCup(2026);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            ResolveBetsWorldCupResultDTO result = Assert.IsType<ResolveBetsWorldCupResultDTO>(okResult.Value);
            Assert.Equal(2026, result.WorldCupId);
            Assert.Equal(2, result.MatchesProcessed);
            Assert.Equal(5, result.TotalResolvedCount);
            Assert.Equal(2, result.MatchResults.Count);
            Assert.Equal(1, result.MatchResults[0].MatchId);
            Assert.Equal(3, result.MatchResults[0].ResolvedCount);
        }

        [Fact]
        public async Task ResolveBetsForWorldCup_WhenServiceThrows_ReturnsBadRequest()
        {
            _betServiceMock
                .Setup(betService => betService.ResolveBetsForWorldCup(2026))
                .ThrowsAsync(new InvalidOperationException("No finished matches found."));

            IActionResult actionResult = await _betController.ResolveBetsForWorldCup(2026);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("No finished matches", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task GetMyActiveBets_WhenAuthenticated_ReturnsActiveBetsFromService()
        {
            User user = new User { Id = 42, Email = "user@test.com", UserName = "user@test.com", Name = "User" };
            SetUserClaims(new Claim(ClaimTypes.Email, "user@test.com"));
            _userManagerMock.Setup(userManager => userManager.FindByEmailAsync("user@test.com")).ReturnsAsync(user);

            List<BetDTO> activeBets = new List<BetDTO>
            {
                new BetDTO { Id = 1, UserId = 42, MatchId = 1, IsDraw = false, PredictedTeamId = 10, IsActive = true }
            };
            _betServiceMock.Setup(betService => betService.GetActiveBets(42)).Returns(activeBets);

            IActionResult actionResult = await _betController.GetMyActiveBets();

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            List<BetDTO> result = Assert.IsType<List<BetDTO>>(okResult.Value);
            Assert.Single(result);
            Assert.True(result[0].IsActive);
        }

        [Fact]
        public async Task GetMySummary_WhenAuthenticated_ReturnsSummaryFromService()
        {
            User user = new User { Id = 42, Email = "user@test.com", UserName = "user@test.com", Name = "User" };
            SetUserClaims(new Claim(ClaimTypes.Email, "user@test.com"));
            _userManagerMock.Setup(userManager => userManager.FindByEmailAsync("user@test.com")).ReturnsAsync(user);

            LeaderboardSummaryDTO summary = new LeaderboardSummaryDTO
            {
                Rank = 3,
                UserId = 42,
                UserName = "User",
                TotalPoints = 6,
                ResolvedBets = 2,
                ActiveBets = 1
            };
            _leaderboardServiceMock.Setup(leaderboardService => leaderboardService.GetMySummary(42, null)).Returns(summary);

            IActionResult actionResult = await _betController.GetMySummary();

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            LeaderboardSummaryDTO result = Assert.IsType<LeaderboardSummaryDTO>(okResult.Value);
            Assert.Equal(3, result.Rank);
            Assert.Equal(6, result.TotalPoints);
        }

        [Fact]
        public async Task GetMyBetForMatch_WhenAuthenticated_ReturnsBetFromService()
        {
            User user = new User { Id = 42, Email = "user@test.com", UserName = "user@test.com", Name = "User" };
            SetUserClaims(new Claim(ClaimTypes.Email, "user@test.com"));
            _userManagerMock.Setup(userManager => userManager.FindByEmailAsync("user@test.com")).ReturnsAsync(user);

            BetDTO bet = new BetDTO { Id = 9, UserId = 42, MatchId = 5, IsDraw = true, PredictedOutcome = "Draw" };
            _betServiceMock.Setup(betService => betService.GetMyBetForMatch(42, 5)).Returns(bet);

            IActionResult actionResult = await _betController.GetMyBetForMatch(5);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            BetDTO result = Assert.IsType<BetDTO>(okResult.Value);
            Assert.Equal(9, result.Id);
        }

        [Fact]
        public async Task GetMyBetsForWorldCup_WhenAuthenticated_ReturnsBetsFromService()
        {
            User user = new User { Id = 42, Email = "user@test.com", UserName = "user@test.com", Name = "User" };
            SetUserClaims(new Claim(ClaimTypes.Email, "user@test.com"));
            _userManagerMock.Setup(userManager => userManager.FindByEmailAsync("user@test.com")).ReturnsAsync(user);

            List<BetDTO> bets = new List<BetDTO>
            {
                new BetDTO { Id = 9, UserId = 42, MatchId = 5, IsDraw = true, PredictedOutcome = "Draw" },
                new BetDTO { Id = 10, UserId = 42, MatchId = 6, IsDraw = false, PredictedOutcome = "Brazil" }
            };
            _betServiceMock.Setup(betService => betService.GetMyBetsForWorldCup(42, 2026)).Returns(bets);

            IActionResult actionResult = await _betController.GetMyBetsForWorldCup(2026);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            List<BetDTO> result = Assert.IsType<List<BetDTO>>(okResult.Value);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task PlaceBet_WhenAuthenticatedWithNameIdentifier_UsesClaimWithoutEmailLookup()
        {
            SetUserClaims(new Claim(ClaimTypes.NameIdentifier, "42"));

            PlaceBetDTO placeBetDto = new PlaceBetDTO { MatchId = 1, IsDraw = false, TeamId = 10 };

            IActionResult actionResult = await _betController.PlaceBet(placeBetDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            _betServiceMock.Verify(betService => betService.PlaceBet(42, placeBetDto), Times.Once);
            _userManagerMock.Verify(userManager => userManager.FindByEmailAsync(It.IsAny<string>()), Times.Never);
            Assert.Equal("Bet placed successfully.", okResult.Value);
        }

        private void SetUserClaims(params Claim[] claims)
        {
            ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuth");
            _betController.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
        }

        private static Mock<UserManager<User>> CreateUserManagerMock()
        {
            Mock<IUserStore<User>> storeMock = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(
                storeMock.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);
        }
    }
}
