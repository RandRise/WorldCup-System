using Core.DTOs.Matches;
using Core.Services.Knockout;
using Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    public class KnockoutControllerTests
    {
        private readonly Mock<IKnockoutService> _knockoutServiceMock;
        private readonly KnockoutController _knockoutController;

        public KnockoutControllerTests()
        {
            _knockoutServiceMock = new Mock<IKnockoutService>();
            _knockoutController = new KnockoutController(_knockoutServiceMock.Object);
        }

        [Fact]
        public void GetBracket_WhenServiceSucceeds_ReturnsOkWithBracket()
        {
            BracketDTO bracket = new BracketDTO
            {
                WorldCupId = 2026,
                Rounds = new List<BracketRoundDTO>
                {
                    new BracketRoundDTO
                    {
                        Stage = MatchStage.SemiFinal,
                        StageName = "Semi-final",
                        Matches = new List<MatchDTO>
                        {
                            new MatchDTO
                            {
                                Id = 50,
                                TeamOneId = null,
                                TeamOneName = "TBD",
                                TeamTwoId = null,
                                TeamTwoName = "TBD",
                                Stage = MatchStage.SemiFinal
                            }
                        }
                    }
                }
            };
            _knockoutServiceMock.Setup(knockoutService => knockoutService.GetBracket(2026)).Returns(bracket);

            IActionResult actionResult = _knockoutController.GetBracket(2026);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            BracketDTO result = Assert.IsType<BracketDTO>(okResult.Value);
            Assert.Equal(2026, result.WorldCupId);
            Assert.Single(result.Rounds);
            Assert.Equal(MatchStage.SemiFinal, result.Rounds[0].Stage);
            Assert.Equal("TBD", result.Rounds[0].Matches[0].TeamOneName);
            _knockoutServiceMock.Verify(knockoutService => knockoutService.GetBracket(2026), Times.Once);
        }

        [Fact]
        public void GetBracket_WhenServiceThrows_ReturnsBadRequest()
        {
            _knockoutServiceMock
                .Setup(knockoutService => knockoutService.GetBracket(999))
                .Throws(new InvalidOperationException("World Cup not found."));

            IActionResult actionResult = _knockoutController.GetBracket(999);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("World Cup not found", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task GenerateBracket_WhenServiceSucceeds_ReturnsOkWithResult()
        {
            GenerateBracketDTO request = new GenerateBracketDTO
            {
                WorldCupId = 2026,
                StadiumId = 5,
                FirstKickoff = new DateTime(2026, 7, 1, 18, 0, 0, DateTimeKind.Utc)
            };
            GenerateBracketResultDTO generateResult = new GenerateBracketResultDTO
            {
                MatchesCreated = 15,
                Message = "Created 15 knockout matches (Round of 16 through Final).",
                Bracket = new BracketDTO { WorldCupId = 2026, Rounds = new List<BracketRoundDTO>() }
            };
            _knockoutServiceMock
                .Setup(knockoutService => knockoutService.GenerateBracket(request))
                .ReturnsAsync(generateResult);

            IActionResult actionResult = await _knockoutController.GenerateBracket(request);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            GenerateBracketResultDTO result = Assert.IsType<GenerateBracketResultDTO>(okResult.Value);
            Assert.Equal(15, result.MatchesCreated);
            Assert.Equal(2026, result.Bracket.WorldCupId);
            _knockoutServiceMock.Verify(knockoutService => knockoutService.GenerateBracket(request), Times.Once);
        }

        [Fact]
        public async Task GenerateBracket_WhenServiceThrows_ReturnsBadRequest()
        {
            GenerateBracketDTO request = new GenerateBracketDTO
            {
                WorldCupId = 2026,
                StadiumId = 5,
                FirstKickoff = DateTime.UtcNow.AddDays(1)
            };
            _knockoutServiceMock
                .Setup(knockoutService => knockoutService.GenerateBracket(request))
                .ThrowsAsync(new InvalidOperationException("Classic knockout generation requires exactly 8 groups (A–H)."));

            IActionResult actionResult = await _knockoutController.GenerateBracket(request);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("exactly 8 groups", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task AdvanceFromMatch_WhenServiceSucceeds_ReturnsOk()
        {
            _knockoutServiceMock
                .Setup(knockoutService => knockoutService.TryAdvanceFromMatch(10))
                .Returns(Task.CompletedTask);

            IActionResult actionResult = await _knockoutController.AdvanceFromMatch(10);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Contains("Winner advanced", okResult.Value?.ToString());
            _knockoutServiceMock.Verify(knockoutService => knockoutService.TryAdvanceFromMatch(10), Times.Once);
        }

        [Fact]
        public async Task AdvanceFromMatch_WhenServiceThrows_ReturnsBadRequest()
        {
            _knockoutServiceMock
                .Setup(knockoutService => knockoutService.TryAdvanceFromMatch(10))
                .ThrowsAsync(new InvalidOperationException("Cannot advance into match 20: goals or cards are already recorded."));

            IActionResult actionResult = await _knockoutController.AdvanceFromMatch(10);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("Cannot advance", badRequestResult.Value?.ToString());
        }
    }
}
