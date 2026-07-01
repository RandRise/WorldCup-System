using Core.DTOs.Stats;
using Core.Services.Stats;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    public class TeamStatsControllerTests
    {
        private readonly Mock<ITeamStatsService> _teamStatsServiceMock;
        private readonly TeamStatsController _teamStatsController;

        public TeamStatsControllerTests()
        {
            _teamStatsServiceMock = new Mock<ITeamStatsService>();
            _teamStatsController = new TeamStatsController(_teamStatsServiceMock.Object);
        }

        [Fact]
        public void GetTeamStatsByMatch_ReturnsStatsFromService()
        {
            List<TeamStatsDTO> stats = new List<TeamStatsDTO>
            {
                new TeamStatsDTO { TeamStatsId = 100, MatchId = 1, TeamId = 10, TeamName = "Brazil", Possession = 55, Score = 2 }
            };

            _teamStatsServiceMock.Setup(teamStatsService => teamStatsService.GetTeamStatsByMatch(1)).Returns(stats);

            List<TeamStatsDTO> result = _teamStatsController.GetTeamStatsByMatch(1);

            Assert.Single(result);
            Assert.Equal("Brazil", result[0].TeamName);
            _teamStatsServiceMock.Verify(teamStatsService => teamStatsService.GetTeamStatsByMatch(1), Times.Once);
        }

        [Fact]
        public async Task UpdateTeamStats_WhenServiceSucceeds_ReturnsOk()
        {
            UpdateTeamStatsDTO updateDto = new UpdateTeamStatsDTO
            {
                MatchId = 1,
                TeamId = 10,
                Possession = 60,
                Shots = 12,
                ShotsOnTarget = 5
            };

            _teamStatsServiceMock.Setup(teamStatsService => teamStatsService.UpdateTeamStats(updateDto)).Returns(Task.CompletedTask);

            IActionResult actionResult = await _teamStatsController.UpdateTeamStats(updateDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Team stats updated successfully.", okResult.Value);
        }

        [Fact]
        public async Task UpdateTeamStats_WhenPossessionInvalid_ReturnsBadRequest()
        {
            UpdateTeamStatsDTO updateDto = new UpdateTeamStatsDTO
            {
                MatchId = 1,
                TeamId = 10,
                Possession = 55,
                Shots = 10,
                ShotsOnTarget = 4
            };

            _teamStatsServiceMock
                .Setup(teamStatsService => teamStatsService.UpdateTeamStats(updateDto))
                .ThrowsAsync(new InvalidOperationException(
                    "Possession for both teams in a match must sum to 100%."));

            IActionResult actionResult = await _teamStatsController.UpdateTeamStats(updateDto);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("must sum to 100%", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task UpdateTeamStats_WhenMatchNotFound_ReturnsNotFound()
        {
            UpdateTeamStatsDTO updateDto = new UpdateTeamStatsDTO
            {
                MatchId = 99,
                TeamId = 10,
                Possession = 50,
                Shots = 0,
                ShotsOnTarget = 0
            };

            _teamStatsServiceMock
                .Setup(teamStatsService => teamStatsService.UpdateTeamStats(updateDto))
                .ThrowsAsync(new KeyNotFoundException("Entity of type Match with id 99 was not found."));

            IActionResult actionResult = await _teamStatsController.UpdateTeamStats(updateDto);

            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
            Assert.Contains("99", notFoundResult.Value?.ToString());
        }
    }
}
