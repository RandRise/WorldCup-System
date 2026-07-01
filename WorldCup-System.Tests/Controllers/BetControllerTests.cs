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

            _leaderboardServiceMock.Setup(leaderboardService => leaderboardService.GetLeaderboard()).Returns(entries);

            List<LeaderboardEntryDTO> result = _betController.GetLeaderboard();

            Assert.Single(result);
            Assert.Equal(9, result[0].TotalPoints);
        }

        [Fact]
        public async Task PlaceBet_WhenAuthenticated_CallsServiceWithUserId()
        {
            User user = new User { Id = 42, Email = "user@test.com", UserName = "user@test.com", Name = "User" };
            SetUserClaims("user@test.com");
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
            SetUserClaims("user@test.com");
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
        public async Task ResolveBetsForMatch_WhenServiceSucceeds_ReturnsOkWithCount()
        {
            _betServiceMock.Setup(betService => betService.ResolveBetsForMatch(1)).ReturnsAsync(3);

            IActionResult actionResult = await _betController.ResolveBetsForMatch(1);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.NotNull(okResult.Value);
        }

        private void SetUserClaims(string email)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email)
            };
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
