using Core.DTOs.Standings;
using Core.Services.Standings;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    public class StandingControllerTests
    {
        private readonly Mock<IStandingsService> _standingsServiceMock;
        private readonly StandingController _standingController;

        public StandingControllerTests()
        {
            _standingsServiceMock = new Mock<IStandingsService>();
            _standingController = new StandingController(_standingsServiceMock.Object);
        }

        [Fact]
        public void GetGroupStandings_ReturnsStandingsFromService()
        {
            List<StandingDTO> standings = new List<StandingDTO>
            {
                new StandingDTO
                {
                    Rank = 1,
                    TeamId = 10,
                    TeamName = "Brazil",
                    Played = 2,
                    Won = 2,
                    Points = 6,
                    GoalsFor = 5,
                    GoalsAgainst = 1,
                    GoalDifference = 4
                },
                new StandingDTO
                {
                    Rank = 2,
                    TeamId = 20,
                    TeamName = "France",
                    Played = 2,
                    Won = 0,
                    Lost = 2,
                    Points = 0
                }
            };

            _standingsServiceMock.Setup(standingsService => standingsService.GetGroupStandings(1)).Returns(standings);

            List<StandingDTO> result = _standingController.GetGroupStandings(1);

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].Rank);
            Assert.Equal("Brazil", result[0].TeamName);
            _standingsServiceMock.Verify(standingsService => standingsService.GetGroupStandings(1), Times.Once);
        }

        [Fact]
        public void GetGroupStandings_WhenEmptyGroup_ReturnsEmptyList()
        {
            _standingsServiceMock.Setup(standingsService => standingsService.GetGroupStandings(99)).Returns(new List<StandingDTO>());

            List<StandingDTO> result = _standingController.GetGroupStandings(99);

            Assert.Empty(result);
        }
    }
}
