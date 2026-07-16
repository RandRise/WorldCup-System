using Core.DTOs.Matches;
using Core.Services.Matches;
using Core.Services.MatchSync;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    public class MatchControllerTests
    {
        private readonly Mock<IMatchService> _matchServiceMock;
        private readonly Mock<IMatchResultSyncService> _matchResultSyncServiceMock;
        private readonly MatchController _matchController;

        public MatchControllerTests()
        {
            _matchServiceMock = new Mock<IMatchService>();
            _matchResultSyncServiceMock = new Mock<IMatchResultSyncService>();
            _matchController = new MatchController(
                _matchServiceMock.Object,
                _matchResultSyncServiceMock.Object);
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
        public async Task GetLiveSnapshot_WhenServiceSucceeds_ReturnsOkWithSnapshot()
        {
            LiveSnapshotDTO snapshot = new LiveSnapshotDTO
            {
                Matches = new List<LiveMatchSnapshotDTO>
                {
                    new LiveMatchSnapshotDTO
                    {
                        MatchId = 1,
                        Status = "Live",
                        TeamOneScore = 1,
                        TeamTwoScore = 0,
                        CurrentMinute = 32
                    },
                    new LiveMatchSnapshotDTO
                    {
                        MatchId = 2,
                        Status = "Finished",
                        TeamOneScore = 2,
                        TeamTwoScore = 1,
                        CurrentMinute = null
                    }
                },
                RecentEvents = new List<LiveEventSnapshotDTO>
                {
                    new LiveEventSnapshotDTO
                    {
                        MatchId = 1,
                        EventType = "Goal",
                        Minute = 25,
                        PlayerName = "Neymar",
                        TeamName = "Brazil"
                    }
                }
            };

            _matchServiceMock
                .Setup(matchService => matchService.GetLiveSnapshot(2026))
                .ReturnsAsync(snapshot);

            IActionResult actionResult = await _matchController.GetLiveSnapshot(2026);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            LiveSnapshotDTO result = Assert.IsType<LiveSnapshotDTO>(okResult.Value);
            Assert.Equal(2, result.Matches.Count);
            Assert.Equal("Live", result.Matches[0].Status);
            Assert.Equal(32, result.Matches[0].CurrentMinute);
            Assert.Single(result.RecentEvents);
            Assert.Equal("Goal", result.RecentEvents[0].EventType);
            _matchServiceMock.Verify(matchService => matchService.GetLiveSnapshot(2026), Times.Once);
        }

        [Fact]
        public async Task GetLiveSnapshot_WhenServiceThrows_ReturnsBadRequest()
        {
            _matchServiceMock
                .Setup(matchService => matchService.GetLiveSnapshot(999))
                .ThrowsAsync(new InvalidOperationException("World Cup not found."));

            IActionResult actionResult = await _matchController.GetLiveSnapshot(999);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("World Cup not found", badRequestResult.Value?.ToString());
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

        [Fact]
        public async Task SetExternalMatchId_WhenServiceSucceeds_ReturnsOk()
        {
            SetExternalMatchIdDTO request = new SetExternalMatchIdDTO
            {
                MatchId = 1,
                ExternalMatchId = "FIFA-100"
            };
            _matchResultSyncServiceMock
                .Setup(syncService => syncService.SetExternalMatchId(request))
                .Returns(Task.CompletedTask);

            IActionResult actionResult = await _matchController.SetExternalMatchId(request);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("External match id saved.", okResult.Value);
            _matchResultSyncServiceMock.Verify(syncService => syncService.SetExternalMatchId(request), Times.Once);
        }

        [Fact]
        public async Task SetExternalMatchId_WhenDuplicate_ReturnsBadRequest()
        {
            SetExternalMatchIdDTO request = new SetExternalMatchIdDTO
            {
                MatchId = 1,
                ExternalMatchId = "FIFA-DUPE"
            };
            _matchResultSyncServiceMock
                .Setup(syncService => syncService.SetExternalMatchId(request))
                .ThrowsAsync(new InvalidOperationException(
                    "ExternalMatchId 'FIFA-DUPE' is already mapped to match 99."));

            IActionResult actionResult = await _matchController.SetExternalMatchId(request);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("already mapped", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task SyncResult_WhenServiceSucceeds_ReturnsOkWithResult()
        {
            SyncMatchResultDTO syncResult = new SyncMatchResultDTO
            {
                MatchId = 1,
                ExternalMatchId = "FIFA-100",
                Applied = true,
                ScoreChanged = true,
                TeamOneScore = 2,
                TeamTwoScore = 1,
                BetsResolved = 4,
                Message = "Applied FT score 2-1 from external feed."
            };
            _matchResultSyncServiceMock
                .Setup(syncService => syncService.SyncResult(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(syncResult);

            IActionResult actionResult = await _matchController.SyncResult(1, CancellationToken.None);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            SyncMatchResultDTO result = Assert.IsType<SyncMatchResultDTO>(okResult.Value);
            Assert.True(result.Applied);
            Assert.Equal(2, result.TeamOneScore);
            Assert.Equal(4, result.BetsResolved);
            _matchResultSyncServiceMock.Verify(
                syncService => syncService.SyncResult(1, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SyncResult_WhenServiceThrows_ReturnsBadRequest()
        {
            _matchResultSyncServiceMock
                .Setup(syncService => syncService.SyncResult(1, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Match 1 has no ExternalMatchId."));

            IActionResult actionResult = await _matchController.SyncResult(1, CancellationToken.None);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("ExternalMatchId", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task SyncFinishedResults_WhenServiceSucceeds_ReturnsOkWithBatchResult()
        {
            SyncFinishedResultsDTO batchResult = new SyncFinishedResultsDTO
            {
                WorldCupId = 2026,
                MatchesAttempted = 2,
                MatchesApplied = 1,
                TotalBetsResolved = 3,
                Message = "Attempted 2 mapped match(es); applied 1; resolved 3 bet update(s).",
                Results = new List<SyncMatchResultDTO>
                {
                    new SyncMatchResultDTO { MatchId = 1, Applied = true, BetsResolved = 3 },
                    new SyncMatchResultDTO { MatchId = 2, Applied = false, Message = "not finished" }
                }
            };
            _matchResultSyncServiceMock
                .Setup(syncService => syncService.SyncFinishedResults(2026, It.IsAny<CancellationToken>()))
                .ReturnsAsync(batchResult);

            IActionResult actionResult =
                await _matchController.SyncFinishedResults(2026, CancellationToken.None);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            SyncFinishedResultsDTO result = Assert.IsType<SyncFinishedResultsDTO>(okResult.Value);
            Assert.Equal(2, result.MatchesAttempted);
            Assert.Equal(1, result.MatchesApplied);
            Assert.Equal(3, result.TotalBetsResolved);
            _matchResultSyncServiceMock.Verify(
                syncService => syncService.SyncFinishedResults(2026, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SyncFinishedResults_WhenServiceThrows_ReturnsBadRequest()
        {
            _matchResultSyncServiceMock
                .Setup(syncService => syncService.SyncFinishedResults(999, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("World Cup not found."));

            IActionResult actionResult =
                await _matchController.SyncFinishedResults(999, CancellationToken.None);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("World Cup not found", badRequestResult.Value?.ToString());
        }
    }
}
