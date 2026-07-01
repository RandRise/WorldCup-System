using Core.DTOs.Matches;
using Core.Services.Matches;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    public class MatchControllerTests
    {
        private readonly Mock<IMatchService> _matchServiceMock;
        private readonly MatchController _matchController;

        public MatchControllerTests()
        {
            _matchServiceMock = new Mock<IMatchService>();
            _matchController = new MatchController(_matchServiceMock.Object);
        }

        [Fact]
        public void GetMatches_ReturnsMatchesFromService()
        {
            List<MatchDTO> matches = new List<MatchDTO>
            {
                new MatchDTO { Id = 1, TeamOneId = 10, TeamOneName = "Brazil", TeamTwoId = 20, TeamTwoName = "France" }
            };

            _matchServiceMock.Setup(matchService => matchService.GetMatches()).Returns(matches);

            List<MatchDTO> result = _matchController.GetMatches();

            Assert.Single(result);
            Assert.Equal("Brazil", result[0].TeamOneName);
            _matchServiceMock.Verify(matchService => matchService.GetMatches(), Times.Once);
        }

        [Fact]
        public async Task GetMatchById_WhenServiceSucceeds_ReturnsOk()
        {
            MatchDetailDTO matchDetail = new MatchDetailDTO
            {
                Id = 1,
                TeamOneId = 10,
                TeamOneName = "Brazil",
                TeamTwoId = 20,
                TeamTwoName = "France"
            };

            _matchServiceMock.Setup(matchService => matchService.GetMatchById(1)).ReturnsAsync(matchDetail);

            IActionResult actionResult = await _matchController.GetMatchById(1);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            MatchDetailDTO result = Assert.IsType<MatchDetailDTO>(okResult.Value);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task GetMatchById_WhenMatchNotFound_ReturnsNotFound()
        {
            _matchServiceMock
                .Setup(matchService => matchService.GetMatchById(99))
                .ThrowsAsync(new KeyNotFoundException("Entity of type Match with id 99 was not found."));

            IActionResult actionResult = await _matchController.GetMatchById(99);

            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
            Assert.Contains("99", notFoundResult.Value?.ToString());
        }

        [Fact]
        public void GetFixturesByWorldCup_ReturnsFixturesFromService()
        {
            List<MatchDTO> fixtures = new List<MatchDTO>
            {
                new MatchDTO { Id = 1, TeamOneId = 10, TeamTwoId = 20 }
            };

            _matchServiceMock.Setup(matchService => matchService.GetFixturesByWorldCup(2026)).Returns(fixtures);

            List<MatchDTO> result = _matchController.GetFixturesByWorldCup(2026);

            Assert.Single(result);
            _matchServiceMock.Verify(matchService => matchService.GetFixturesByWorldCup(2026), Times.Once);
        }

        [Fact]
        public void GetFixturesByGroup_ReturnsFixturesFromService()
        {
            List<MatchDTO> fixtures = new List<MatchDTO>
            {
                new MatchDTO { Id = 2, TeamOneId = 10, TeamTwoId = 30 }
            };

            _matchServiceMock.Setup(matchService => matchService.GetFixturesByGroup(1)).Returns(fixtures);

            List<MatchDTO> result = _matchController.GetFixturesByGroup(1);

            Assert.Single(result);
        }

        [Fact]
        public void GetFixturesByDate_ReturnsFixturesFromService()
        {
            DateTime date = new DateTime(2026, 6, 15);
            List<MatchDTO> fixtures = new List<MatchDTO> { new MatchDTO { Id = 3, Date = date } };

            _matchServiceMock.Setup(matchService => matchService.GetFixturesByDate(date)).Returns(fixtures);

            List<MatchDTO> result = _matchController.GetFixturesByDate(date);

            Assert.Single(result);
        }

        [Fact]
        public async Task AddMatch_WhenServiceSucceeds_ReturnsOk()
        {
            AddMatchDTO addMatchDto = new AddMatchDTO
            {
                TeamOneId = 10,
                TeamTwoId = 20,
                StadiumId = 5,
                Date = DateTime.UtcNow
            };

            _matchServiceMock.Setup(matchService => matchService.AddMatch(addMatchDto)).Returns(Task.CompletedTask);

            IActionResult actionResult = await _matchController.AddMatch(addMatchDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Match added successfully.", okResult.Value);
        }

        [Fact]
        public async Task AddMatch_WhenSameTeam_ReturnsBadRequest()
        {
            AddMatchDTO addMatchDto = new AddMatchDTO
            {
                TeamOneId = 10,
                TeamTwoId = 10,
                StadiumId = 5,
                Date = DateTime.UtcNow
            };

            _matchServiceMock
                .Setup(matchService => matchService.AddMatch(addMatchDto))
                .ThrowsAsync(new InvalidOperationException("A match must be between two different teams."));

            IActionResult actionResult = await _matchController.AddMatch(addMatchDto);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("two different teams", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task UpdateMatch_WhenServiceSucceeds_ReturnsOk()
        {
            UpdateMatchDTO updateMatchDto = new UpdateMatchDTO
            {
                Id = 1,
                TeamOneId = 10,
                TeamTwoId = 20,
                StadiumId = 5,
                Date = DateTime.UtcNow
            };

            _matchServiceMock.Setup(matchService => matchService.UpdateMatch(updateMatchDto)).Returns(Task.CompletedTask);

            IActionResult actionResult = await _matchController.UpdateMatch(updateMatchDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Match updated successfully.", okResult.Value);
        }

        [Fact]
        public async Task UpdateMatch_WhenRescheduleBlocked_ReturnsBadRequest()
        {
            UpdateMatchDTO updateMatchDto = new UpdateMatchDTO
            {
                Id = 1,
                TeamOneId = 10,
                TeamTwoId = 20,
                StadiumId = 5,
                Date = DateTime.UtcNow.AddDays(1)
            };

            _matchServiceMock
                .Setup(matchService => matchService.UpdateMatch(updateMatchDto))
                .ThrowsAsync(new InvalidOperationException(
                    "Cannot reschedule a match after goals or cards have been recorded."));

            IActionResult actionResult = await _matchController.UpdateMatch(updateMatchDto);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("Cannot reschedule", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task DeleteMatch_WhenServiceSucceeds_ReturnsOk()
        {
            _matchServiceMock.Setup(matchService => matchService.DeleteMatch(1)).Returns(Task.CompletedTask);

            IActionResult actionResult = await _matchController.DeleteMatch(1);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Match deleted successfully.", okResult.Value);
        }

        [Fact]
        public async Task DeleteMatch_WhenBetsExist_ReturnsBadRequest()
        {
            _matchServiceMock
                .Setup(matchService => matchService.DeleteMatch(1))
                .ThrowsAsync(new InvalidOperationException(
                    "Cannot delete a match while bets are placed on it. Remove bets first."));

            IActionResult actionResult = await _matchController.DeleteMatch(1);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("bets are placed", badRequestResult.Value?.ToString());
        }
    }
}
