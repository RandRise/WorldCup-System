using System.Linq.Expressions;
using Core.DTOs.Bets;
using Core.DTOs.Matches;
using Core.Services.Bets;
using Core.Services.Knockout;
using Core.Services.Matches;
using Core.Services.MatchSync;
using Data.Entities;
using Data.Repos;
using Moq;
using MatchEntity = Data.Entities.Match;

namespace WorldCup_System.Tests.Services.MatchSync
{
    public class MatchResultSyncServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<MatchEntity>> _matchRepositoryMock;
        private readonly Mock<IRepository<Team>> _teamRepositoryMock;
        private readonly Mock<IRepository<Country>> _countryRepositoryMock;
        private readonly Mock<IRepository<TeamStats>> _teamStatsRepositoryMock;
        private readonly Mock<IRepository<Goal>> _goalRepositoryMock;
        private readonly Mock<IRepository<Player>> _playerRepositoryMock;
        private readonly Mock<IRepository<PlayerPosition>> _playerPositionRepositoryMock;
        private readonly Mock<IExternalMatchResultProvider> _resultProviderMock;
        private readonly Mock<IBetService> _betServiceMock;
        private readonly Mock<IKnockoutService> _knockoutServiceMock;
        private readonly Mock<IMatchService> _matchServiceMock;
        private readonly MatchResultSyncService _syncService;

        private readonly Group _group = new Group { Id = 1, Name = "A" };
        private readonly List<Team> _teams;
        private readonly List<Country> _countries;
        private readonly List<Player> _players;
        private readonly List<TeamStats> _teamStats;
        private readonly List<Goal> _goals;
        private readonly List<MatchEntity> _matches;

        public MatchResultSyncServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _matchRepositoryMock = new Mock<IRepository<MatchEntity>>();
            _teamRepositoryMock = new Mock<IRepository<Team>>();
            _countryRepositoryMock = new Mock<IRepository<Country>>();
            _teamStatsRepositoryMock = new Mock<IRepository<TeamStats>>();
            _goalRepositoryMock = new Mock<IRepository<Goal>>();
            _playerRepositoryMock = new Mock<IRepository<Player>>();
            _playerPositionRepositoryMock = new Mock<IRepository<PlayerPosition>>();
            _resultProviderMock = new Mock<IExternalMatchResultProvider>();
            _betServiceMock = new Mock<IBetService>();
            _knockoutServiceMock = new Mock<IKnockoutService>();
            _matchServiceMock = new Mock<IMatchService>();

            _countries = new List<Country>
            {
                new Country { Id = 1, Name = "Brazil" },
                new Country { Id = 2, Name = "France" }
            };
            _teams = new List<Team>
            {
                new Team
                {
                    Id = 10,
                    CountryId = 1,
                    GroupId = 1,
                    Country = _countries[0],
                    Group = _group,
                    Coach = new List<Coach>()
                },
                new Team
                {
                    Id = 20,
                    CountryId = 2,
                    GroupId = 1,
                    Country = _countries[1],
                    Group = _group,
                    Coach = new List<Coach>()
                }
            };
            _players = new List<Player>
            {
                new Player
                {
                    Id = 501,
                    Name = MatchResultSyncService.PlaceholderScorerName,
                    Number = 99,
                    TeamId = 10,
                    PositionId = 1
                },
                new Player
                {
                    Id = 502,
                    Name = MatchResultSyncService.PlaceholderScorerName,
                    Number = 99,
                    TeamId = 20,
                    PositionId = 1
                }
            };
            _teamStats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10, Possession = 50, Shots = 0, ShotsOnTarget = 0, Points = 0 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20, Possession = 50, Shots = 0, ShotsOnTarget = 0, Points = 0 }
            };
            _goals = new List<Goal>();
            _matches = new List<MatchEntity>();

            _repositoryManagerMock.Setup(manager => manager.Match).Returns(_matchRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.Team).Returns(_teamRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.Country).Returns(_countryRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.TeamStats).Returns(_teamStatsRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.Goal).Returns(_goalRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.Player).Returns(_playerRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.PlayerPosition).Returns(_playerPositionRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.SaveAsync()).Returns(Task.CompletedTask);

            SetupFind(_teamRepositoryMock, _teams);
            SetupFind(_countryRepositoryMock, _countries);
            SetupFind(_teamStatsRepositoryMock, _teamStats);
            SetupFind(_goalRepositoryMock, _goals);
            SetupFind(_playerRepositoryMock, _players);
            SetupFind(_matchRepositoryMock, _matches);
            SetupFind(_playerPositionRepositoryMock, new List<PlayerPosition>
            {
                new PlayerPosition { Id = 1, Name = "Forward" }
            });

            _goalRepositoryMock
                .Setup(repository => repository.Create(It.IsAny<Goal>()))
                .Callback<Goal>(goal => _goals.Add(goal));
            _goalRepositoryMock
                .Setup(repository => repository.Delete(It.IsAny<Goal>()))
                .Callback<Goal>(goal => _goals.Remove(goal));

            _betServiceMock
                .Setup(betService => betService.ResolveBetsForMatch(It.IsAny<int>()))
                .ReturnsAsync(new ResolveBetsResultDTO { MatchId = 1, ResolvedCount = 0 });
            _knockoutServiceMock
                .Setup(knockoutService => knockoutService.TryAdvanceFromMatch(It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            _syncService = new MatchResultSyncService(
                _repositoryManagerMock.Object,
                _resultProviderMock.Object,
                _betServiceMock.Object,
                _knockoutServiceMock.Object,
                _matchServiceMock.Object);
        }

        [Fact]
        public async Task SyncResult_WhenFinishedAndSidesMatch_AppliesScoresAndResolvesBets()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-100");
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-100", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-100",
                    IsFinished = true,
                    HomeScore = 2,
                    AwayScore = 1,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });
            _betServiceMock
                .Setup(betService => betService.ResolveBetsForMatch(1))
                .ReturnsAsync(new ResolveBetsResultDTO { MatchId = 1, ResolvedCount = 3, Message = "Resolved." });

            SyncMatchResultDTO result = await _syncService.SyncResult(1);

            Assert.True(result.Applied);
            Assert.True(result.ScoreChanged);
            Assert.Equal(2, result.TeamOneScore);
            Assert.Equal(1, result.TeamTwoScore);
            Assert.Equal(3, result.BetsResolved);
            Assert.Equal(2, _goals.Count(goal => goal.TeamStatsId == 100));
            Assert.Equal(1, _goals.Count(goal => goal.TeamStatsId == 101));
            _goalRepositoryMock.Verify(repository => repository.Create(It.IsAny<Goal>()), Times.Exactly(3));
            _betServiceMock.Verify(betService => betService.ResolveBetsForMatch(1), Times.Once);
            _knockoutServiceMock.Verify(knockoutService => knockoutService.TryAdvanceFromMatch(1), Times.Once);
        }

        [Fact]
        public async Task SyncResult_WhenFifaHomeAwayReversed_SwapsScoresToLocalSides()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-200");
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-200", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-200",
                    IsFinished = true,
                    HomeScore = 3,
                    AwayScore = 0,
                    HomeTeamName = "France",
                    AwayTeamName = "Brazil",
                    StatusLabel = "MatchStatus=0"
                });

            SyncMatchResultDTO result = await _syncService.SyncResult(1);

            Assert.True(result.Applied);
            Assert.True(result.ScoreChanged);
            Assert.Equal(0, result.TeamOneScore);
            Assert.Equal(3, result.TeamTwoScore);
            Assert.Equal(0, _goals.Count(goal => goal.TeamStatsId == 100));
            Assert.Equal(3, _goals.Count(goal => goal.TeamStatsId == 101));
        }

        [Fact]
        public async Task SyncResult_WhenSidesDoNotMatch_ThrowsInvalidOperationException()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-300");
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-300", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-300",
                    IsFinished = true,
                    HomeScore = 1,
                    AwayScore = 1,
                    HomeTeamName = "Germany",
                    AwayTeamName = "Spain",
                    StatusLabel = "MatchStatus=0"
                });

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _syncService.SyncResult(1));

            Assert.Contains("External sides do not match", exception.Message);
            _goalRepositoryMock.Verify(repository => repository.Create(It.IsAny<Goal>()), Times.Never);
            _goalRepositoryMock.Verify(repository => repository.Delete(It.IsAny<Goal>()), Times.Never);
        }

        [Fact]
        public async Task SyncResult_WhenScoreAlreadyMatches_IsIdempotentAndDoesNotDeleteGoals()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-400");
            _goals.Add(new Goal { Id = 1, TeamStatsId = 100, PlayerId = 501, TimeScored = match.Date.AddMinutes(10), IsOwnGoal = 0 });
            _goals.Add(new Goal { Id = 2, TeamStatsId = 100, PlayerId = 501, TimeScored = match.Date.AddMinutes(20), IsOwnGoal = 0 });
            _goals.Add(new Goal { Id = 3, TeamStatsId = 101, PlayerId = 502, TimeScored = match.Date.AddMinutes(55), IsOwnGoal = 0 });

            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-400", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-400",
                    IsFinished = true,
                    HomeScore = 2,
                    AwayScore = 1,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });

            SyncMatchResultDTO result = await _syncService.SyncResult(1);

            Assert.True(result.Applied);
            Assert.False(result.ScoreChanged);
            Assert.Equal(2, result.TeamOneScore);
            Assert.Equal(1, result.TeamTwoScore);
            Assert.Equal(3, _goals.Count);
            _goalRepositoryMock.Verify(repository => repository.Delete(It.IsAny<Goal>()), Times.Never);
            _goalRepositoryMock.Verify(repository => repository.Create(It.IsAny<Goal>()), Times.Never);
            Assert.Contains("Score already matched", result.Message);
        }

        [Fact]
        public async Task SyncResult_WhenExternalNotFinished_ReturnsAppliedFalse()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-500");
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-500", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-500",
                    IsFinished = false,
                    HomeScore = 1,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=3"
                });

            SyncMatchResultDTO result = await _syncService.SyncResult(1);

            Assert.False(result.Applied);
            Assert.False(result.ScoreChanged);
            Assert.Contains("not finished", result.Message);
            _goalRepositoryMock.Verify(repository => repository.Create(It.IsAny<Goal>()), Times.Never);
            _betServiceMock.Verify(betService => betService.ResolveBetsForMatch(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SyncResult_WhenNoExternalMatchId_ThrowsInvalidOperationException()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: null);
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _syncService.SyncResult(1));

            Assert.Contains("no ExternalMatchId", exception.Message);
            _resultProviderMock.Verify(
                provider => provider.FetchResultAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SetExternalMatchId_WhenDuplicateExists_ThrowsInvalidOperationException()
        {
            MatchEntity target = CreateFinishedMatch(externalMatchId: null);
            MatchEntity conflicting = new MatchEntity
            {
                Id = 99,
                Date = DateTime.UtcNow.AddDays(-2),
                StadiumId = 5,
                TeamOneId = 10,
                TeamTwoId = 20,
                ExternalMatchId = "FIFA-DUPE"
            };
            _matches.Add(conflicting);
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(target);

            SetExternalMatchIdDTO request = new SetExternalMatchIdDTO
            {
                MatchId = 1,
                ExternalMatchId = "FIFA-DUPE"
            };

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _syncService.SetExternalMatchId(request));

            Assert.Contains("already mapped to match 99", exception.Message);
            _matchRepositoryMock.Verify(repository => repository.Update(It.IsAny<MatchEntity>()), Times.Never);
        }

        [Fact]
        public async Task SetExternalMatchId_WhenUnique_UpdatesMatchAndSaves()
        {
            MatchEntity target = CreateFinishedMatch(externalMatchId: null);
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(target);

            SetExternalMatchIdDTO request = new SetExternalMatchIdDTO
            {
                MatchId = 1,
                ExternalMatchId = "  FIFA-NEW  "
            };

            await _syncService.SetExternalMatchId(request);

            Assert.Equal("FIFA-NEW", target.ExternalMatchId);
            _matchRepositoryMock.Verify(repository => repository.Update(target), Times.Once);
            _repositoryManagerMock.Verify(manager => manager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task SyncFinishedResults_SyncsMappedFixturesAndCapturesFailures()
        {
            MatchEntity successMatch = CreateFinishedMatch(externalMatchId: "FIFA-OK");
            MatchEntity failMatch = new MatchEntity
            {
                Id = 2,
                Date = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 10)),
                StadiumId = 5,
                TeamOneId = 10,
                TeamTwoId = 20,
                ExternalMatchId = "FIFA-FAIL"
            };
            _matches.Add(successMatch);
            _matches.Add(failMatch);

            _teamStats.Add(new TeamStats { Id = 200, MatchId = 2, TeamId = 10, Possession = 50, Shots = 0, ShotsOnTarget = 0, Points = 0 });
            _teamStats.Add(new TeamStats { Id = 201, MatchId = 2, TeamId = 20, Possession = 50, Shots = 0, ShotsOnTarget = 0, Points = 0 });

            _matchServiceMock
                .Setup(matchService => matchService.GetFixturesByWorldCup(2026))
                .Returns(new List<MatchDTO>
                {
                    new MatchDTO { Id = 1 },
                    new MatchDTO { Id = 2 }
                });

            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(successMatch);
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(2)).ReturnsAsync(failMatch);

            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-OK", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-OK",
                    IsFinished = true,
                    HomeScore = 1,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France"
                });
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-FAIL", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Provider unavailable."));

            SyncFinishedResultsDTO result = await _syncService.SyncFinishedResults(2026);

            Assert.Equal(2026, result.WorldCupId);
            Assert.Equal(2, result.MatchesAttempted);
            Assert.Equal(1, result.MatchesApplied);
            Assert.Equal(2, result.Results.Count);
            Assert.Contains(result.Results, syncResult => syncResult.MatchId == 1 && syncResult.Applied);
            Assert.Contains(result.Results, syncResult =>
                syncResult.MatchId == 2
                && !syncResult.Applied
                && syncResult.Message.Contains("Provider unavailable"));
        }

        private MatchEntity CreateFinishedMatch(string? externalMatchId)
        {
            return new MatchEntity
            {
                Id = 1,
                Date = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 10)),
                StadiumId = 5,
                TeamOneId = 10,
                TeamTwoId = 20,
                ExternalMatchId = externalMatchId
            };
        }

        private static void SetupFind<T>(Mock<IRepository<T>> repositoryMock, List<T> entities) where T : class
        {
            repositoryMock
                .Setup(repository => repository.Find(It.IsAny<Expression<Func<T, bool>>>()))
                .Returns((Expression<Func<T, bool>> predicate) => entities.AsQueryable().Where(predicate));
        }
    }
}
