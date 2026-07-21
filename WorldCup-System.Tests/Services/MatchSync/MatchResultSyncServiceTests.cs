using System.Linq.Expressions;
using Core.DTOs.Bets;
using Core.DTOs.Matches;
using Core.Options;
using Core.Services.Bets;
using Core.Services.Knockout;
using Core.Services.Matches;
using Core.Services.MatchSync;
using Data.Entities;
using Data.Repos;
using Microsoft.Extensions.Options;
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
        private readonly Mock<IExternalMatchEventsProvider> _eventsProviderMock;
        private readonly Mock<ITimelineScorerApplyService> _scorerApplyServiceMock;
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
            _eventsProviderMock = new Mock<IExternalMatchEventsProvider>();
            _scorerApplyServiceMock = new Mock<ITimelineScorerApplyService>();
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
                },
                new Player
                {
                    Id = 601,
                    Name = "Richarlison",
                    Number = 9,
                    TeamId = 10,
                    PositionId = 1
                },
                new Player
                {
                    Id = 602,
                    Name = "Mbappé",
                    Number = 10,
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

            MatchResultSyncOptions syncOptions = new MatchResultSyncOptions
            {
                BatchDelayMilliseconds = 0
            };

            _syncService = new MatchResultSyncService(
                _repositoryManagerMock.Object,
                _resultProviderMock.Object,
                _eventsProviderMock.Object,
                _scorerApplyServiceMock.Object,
                _betServiceMock.Object,
                _knockoutServiceMock.Object,
                _matchServiceMock.Object,
                Options.Create(syncOptions));
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
            Assert.All(_goals.Where(goal => goal.TeamStatsId == 100), goal => Assert.Equal(601, goal.PlayerId));
            Assert.All(_goals.Where(goal => goal.TeamStatsId == 101), goal => Assert.Equal(602, goal.PlayerId));
            _goalRepositoryMock.Verify(repository => repository.Create(It.IsAny<Goal>()), Times.Exactly(3));
            _betServiceMock.Verify(betService => betService.ResolveBetsForMatch(1), Times.Once);
            _knockoutServiceMock.Verify(knockoutService => knockoutService.TryAdvanceFromMatch(1), Times.Once);
        }

        [Fact]
        public async Task SyncResult_WhenOnlyPlaceholders_UsesPlaceholderScorers()
        {
            _players.RemoveAll(player => player.Id == 601 || player.Id == 602);

            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-PLACEHOLDER");
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-PLACEHOLDER", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-PLACEHOLDER",
                    IsFinished = true,
                    HomeScore = 2,
                    AwayScore = 1,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });

            SyncMatchResultDTO result = await _syncService.SyncResult(1);

            Assert.True(result.Applied);
            Assert.Equal(2, _goals.Count(goal => goal.TeamStatsId == 100 && goal.PlayerId == 501));
            Assert.Equal(1, _goals.Count(goal => goal.TeamStatsId == 101 && goal.PlayerId == 502));
        }

        [Fact]
        public async Task SyncResult_WhenRealForwardWearsJersey99_UsesThatPlayerNotPlaceholder()
        {
            // Regression: GetSquadScorers must exclude only by name "Tournament Scorer",
            // not by jersey Number == 99 (real squad players may wear 99).
            Player teamOneForward = _players.Single(player => player.Id == 601);
            Player teamTwoForward = _players.Single(player => player.Id == 602);
            teamOneForward.Number = 99;
            teamTwoForward.Number = 99;

            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-JERSEY-99");
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-JERSEY-99", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-JERSEY-99",
                    IsFinished = true,
                    HomeScore = 2,
                    AwayScore = 1,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });

            SyncMatchResultDTO result = await _syncService.SyncResult(1);

            Assert.True(result.Applied);
            Assert.Equal(2, _goals.Count(goal => goal.TeamStatsId == 100 && goal.PlayerId == 601));
            Assert.Equal(1, _goals.Count(goal => goal.TeamStatsId == 101 && goal.PlayerId == 602));
            Assert.DoesNotContain(_goals, goal => goal.PlayerId == 501 || goal.PlayerId == 502);
        }

        [Fact]
        public async Task SyncResult_WhenNoForwardsButSquadExists_UsesNonPlaceholderPlayers()
        {
            _players.RemoveAll(player => player.Id == 601 || player.Id == 602);
            SetupFind(_playerPositionRepositoryMock, new List<PlayerPosition>
            {
                new PlayerPosition { Id = 1, Name = "Forward" },
                new PlayerPosition { Id = 2, Name = "Midfielder" }
            });
            _players.Add(new Player { Id = 701, Name = "Casemiro", Number = 5, TeamId = 10, PositionId = 2 });
            _players.Add(new Player { Id = 702, Name = "Tchouameni", Number = 8, TeamId = 20, PositionId = 2 });

            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-MID");
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-MID", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-MID",
                    IsFinished = true,
                    HomeScore = 1,
                    AwayScore = 1,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });

            SyncMatchResultDTO result = await _syncService.SyncResult(1);

            Assert.True(result.Applied);
            Assert.Equal(1, _goals.Count(goal => goal.TeamStatsId == 100 && goal.PlayerId == 701));
            Assert.Equal(1, _goals.Count(goal => goal.TeamStatsId == 101 && goal.PlayerId == 702));
            Assert.DoesNotContain(_goals, goal => goal.PlayerId == 501 || goal.PlayerId == 502);
        }

        [Fact]
        public async Task SyncResult_WhenMultipleGoalsExceedForwardCount_CyclesSquadScorers()
        {
            _players.Add(new Player { Id = 803, Name = "Raphinha", Number = 11, TeamId = 10, PositionId = 1 });

            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-CYCLE");
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-CYCLE", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-CYCLE",
                    IsFinished = true,
                    HomeScore = 3,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });

            SyncMatchResultDTO result = await _syncService.SyncResult(1);

            Assert.True(result.Applied);
            List<int> homeScorerIds = _goals
                .Where(goal => goal.TeamStatsId == 100)
                .Select(goal => goal.PlayerId)
                .ToList();
            Assert.Equal(new List<int> { 601, 803, 601 }, homeScorerIds);
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
        public async Task SyncResult_WhenExternalNameIsSubstringSimilarButNotEqual_ThrowsInvalidOperationException()
        {
            // Equality-only matching: "Niger" must not match "Nigeria" via substring Contains.
            _countries[0].Name = "Niger";
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-SUBSTR");
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-SUBSTR", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-SUBSTR",
                    IsFinished = true,
                    HomeScore = 2,
                    AwayScore = 0,
                    HomeTeamName = "Nigeria",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _syncService.SyncResult(1));

            Assert.Contains("External sides do not match", exception.Message);
            Assert.Contains("Niger", exception.Message);
            Assert.Contains("Nigeria", exception.Message);
            _goalRepositoryMock.Verify(repository => repository.Create(It.IsAny<Goal>()), Times.Never);
            _goalRepositoryMock.Verify(repository => repository.Delete(It.IsAny<Goal>()), Times.Never);
        }

        [Fact]
        public async Task SyncResult_WhenExternalUsesUsaAlias_MatchesUnitedStates()
        {
            _countries[0].Name = "United States";
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-USA");
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-USA", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-USA",
                    IsFinished = true,
                    HomeScore = 1,
                    AwayScore = 0,
                    HomeTeamName = "USA",
                    AwayTeamName = "France",
                    HomeTeamCountryCode = "USA",
                    StatusLabel = "MatchStatus=0"
                });

            SyncMatchResultDTO result = await _syncService.SyncResult(1);

            Assert.True(result.Applied);
            Assert.True(result.ScoreChanged);
            Assert.Equal(1, result.TeamOneScore);
            Assert.Equal(0, result.TeamTwoScore);
            Assert.Equal(1, _goals.Count(goal => goal.TeamStatsId == 100));
            Assert.Equal(0, _goals.Count(goal => goal.TeamStatsId == 101));
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
            Assert.Null(result.ScorerStatus);
            Assert.Contains("not finished", result.Message);
            _goalRepositoryMock.Verify(repository => repository.Create(It.IsAny<Goal>()), Times.Never);
            _betServiceMock.Verify(betService => betService.ResolveBetsForMatch(It.IsAny<int>()), Times.Never);
            _eventsProviderMock.Verify(
                provider => provider.FetchGoalEventsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
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
        public async Task SetExternalMatchId_WhenExternalStageIdProvided_SetsTrimmedStageId()
        {
            MatchEntity target = CreateFinishedMatch(externalMatchId: null);
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(target);

            SetExternalMatchIdDTO request = new SetExternalMatchIdDTO
            {
                MatchId = 1,
                ExternalMatchId = "FIFA-NEW",
                ExternalStageId = "  STAGE-100  "
            };

            await _syncService.SetExternalMatchId(request);

            Assert.Equal("FIFA-NEW", target.ExternalMatchId);
            Assert.Equal("STAGE-100", target.ExternalStageId);
            _matchRepositoryMock.Verify(repository => repository.Update(target), Times.Once);
            _repositoryManagerMock.Verify(manager => manager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task SetExternalMatchId_WhenExternalStageIdOmitted_KeepsExistingStageId()
        {
            MatchEntity target = CreateFinishedMatch(externalMatchId: null);
            target.ExternalStageId = "STAGE-EXISTING";
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(target);

            SetExternalMatchIdDTO request = new SetExternalMatchIdDTO
            {
                MatchId = 1,
                ExternalMatchId = "FIFA-NEW",
                ExternalStageId = null
            };

            await _syncService.SetExternalMatchId(request);

            Assert.Equal("FIFA-NEW", target.ExternalMatchId);
            Assert.Equal("STAGE-EXISTING", target.ExternalStageId);
        }

        [Fact]
        public async Task SetExternalMatchId_WhenExternalStageIdBlank_ClearsExistingStageId()
        {
            MatchEntity target = CreateFinishedMatch(externalMatchId: null);
            target.ExternalStageId = "STAGE-EXISTING";
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(target);

            SetExternalMatchIdDTO request = new SetExternalMatchIdDTO
            {
                MatchId = 1,
                ExternalMatchId = "FIFA-NEW",
                ExternalStageId = "   "
            };

            await _syncService.SetExternalMatchId(request);

            Assert.Equal("FIFA-NEW", target.ExternalMatchId);
            Assert.Null(target.ExternalStageId);
        }

        [Fact]
        public async Task SyncResult_WhenProviderReturnsExternalStageIdAndMatchLacksIt_PersistsStageId()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-STAGE");
            match.ExternalStageId = null;
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-STAGE", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-STAGE",
                    ExternalStageId = "  STAGE-55  ",
                    IsFinished = false,
                    HomeScore = 0,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=3"
                });

            SyncMatchResultDTO result = await _syncService.SyncResult(1);

            Assert.False(result.Applied);
            Assert.Equal("STAGE-55", match.ExternalStageId);
            Assert.Equal("STAGE-55", result.ExternalStageId);
            _matchRepositoryMock.Verify(repository => repository.Update(match), Times.Once);
            _repositoryManagerMock.Verify(manager => manager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task SyncResult_WhenExternalStageIdAlreadyMatches_DoesNotPersistAgain()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-STAGE-SAME");
            match.ExternalStageId = "STAGE-55";
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-STAGE-SAME", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-STAGE-SAME",
                    ExternalStageId = "STAGE-55",
                    IsFinished = false,
                    HomeScore = 0,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=3"
                });

            SyncMatchResultDTO result = await _syncService.SyncResult(1);

            Assert.False(result.Applied);
            Assert.Equal("STAGE-55", result.ExternalStageId);
            _matchRepositoryMock.Verify(repository => repository.Update(It.IsAny<MatchEntity>()), Times.Never);
            _repositoryManagerMock.Verify(manager => manager.SaveAsync(), Times.Never);
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

        [Fact]
        public async Task SyncFinishedResults_WhenCanceled_RethrowsOperationCanceledException()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-BATCH-CANCEL");
            match.ExternalStageId = "STAGE-BATCH-CANCEL";
            _matches.Add(match);

            _matchServiceMock
                .Setup(matchService => matchService.GetFixturesByWorldCup(2026))
                .Returns(new List<MatchDTO>
                {
                    new MatchDTO { Id = 1 }
                });
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync(
                    "FIFA-BATCH-CANCEL",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-BATCH-CANCEL",
                    ExternalStageId = "STAGE-BATCH-CANCEL",
                    IsFinished = true,
                    HomeScore = 1,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            _eventsProviderMock
                .Setup(provider => provider.FetchGoalEventsAsync(
                    "FIFA-BATCH-CANCEL",
                    "STAGE-BATCH-CANCEL",
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException(cancellationTokenSource.Token));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => _syncService.SyncFinishedResults(2026, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task SyncFinishedResults_WhenFailureAfterStagePersisted_ErrorDtoUsesRefreshedExternalStageId()
        {
            MatchEntity staleMapped = new MatchEntity
            {
                Id = 2,
                Date = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 10)),
                StadiumId = 5,
                TeamOneId = 10,
                TeamTwoId = 20,
                ExternalMatchId = "FIFA-STAGE-FAIL",
                ExternalStageId = null
            };
            MatchEntity liveMatch = new MatchEntity
            {
                Id = 2,
                Date = staleMapped.Date,
                StadiumId = 5,
                TeamOneId = 10,
                TeamTwoId = 20,
                ExternalMatchId = "FIFA-STAGE-FAIL",
                ExternalStageId = null
            };
            _matches.Add(staleMapped);

            _teamStats.Add(new TeamStats { Id = 200, MatchId = 2, TeamId = 10, Possession = 50, Shots = 0, ShotsOnTarget = 0, Points = 0 });
            _teamStats.Add(new TeamStats { Id = 201, MatchId = 2, TeamId = 20, Possession = 50, Shots = 0, ShotsOnTarget = 0, Points = 0 });

            _matchServiceMock
                .Setup(matchService => matchService.GetFixturesByWorldCup(2026))
                .Returns(new List<MatchDTO>
                {
                    new MatchDTO { Id = 2 }
                });

            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(2)).ReturnsAsync(liveMatch);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-STAGE-FAIL", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-STAGE-FAIL",
                    ExternalStageId = "STAGE-REFRESH",
                    IsFinished = true,
                    HomeScore = 1,
                    AwayScore = 1,
                    HomeTeamName = "Germany",
                    AwayTeamName = "Spain",
                    StatusLabel = "MatchStatus=0"
                });

            SyncFinishedResultsDTO result = await _syncService.SyncFinishedResults(2026);

            Assert.Equal(1, result.MatchesAttempted);
            Assert.Equal(0, result.MatchesApplied);
            Assert.Null(staleMapped.ExternalStageId);
            Assert.Equal("STAGE-REFRESH", liveMatch.ExternalStageId);
            SyncMatchResultDTO failure = Assert.Single(result.Results);
            Assert.Equal(2, failure.MatchId);
            Assert.False(failure.Applied);
            Assert.Equal("STAGE-REFRESH", failure.ExternalStageId);
            Assert.Contains("External sides do not match", failure.Message);
            _matchRepositoryMock.Verify(repository => repository.Update(liveMatch), Times.Once);
        }

        [Fact]
        public async Task SyncResult_WhenNoExternalStageId_SkipsScorersWithoutCallingTimeline()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-NO-STAGE");
            match.ExternalStageId = null;
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-NO-STAGE", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-NO-STAGE",
                    IsFinished = true,
                    HomeScore = 1,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });

            SyncMatchResultDTO result = await _syncService.SyncResult(1);

            Assert.True(result.Applied);
            Assert.Equal(SyncScorerStatuses.Skipped, result.ScorerStatus);
            Assert.Contains("no ExternalStageId", result.ScorerMessage);
            Assert.Contains("Scorers:", result.Message);
            _eventsProviderMock.Verify(
                provider => provider.FetchGoalEventsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
            _scorerApplyServiceMock.Verify(
                service => service.ApplyAsync(
                    It.IsAny<int>(),
                    It.IsAny<ExternalMatchEvents>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SyncResult_WhenStagePresent_AppliesTimelineScorersAfterScore()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-SCORERS");
            match.ExternalStageId = "STAGE-1";
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-SCORERS", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-SCORERS",
                    ExternalStageId = "STAGE-1",
                    IsFinished = true,
                    HomeScore = 2,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });

            ExternalMatchEvents timelineEvents = new ExternalMatchEvents
            {
                ExternalMatchId = "FIFA-SCORERS",
                ExternalStageId = "STAGE-1",
                Goals = Array.Empty<ExternalMatchGoalEvent>()
            };
            _eventsProviderMock
                .Setup(provider => provider.FetchGoalEventsAsync(
                    "FIFA-SCORERS",
                    "STAGE-1",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(timelineEvents);
            _scorerApplyServiceMock
                .Setup(service => service.ApplyAsync(
                    1,
                    timelineEvents,
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TimelineScorerApplyResult
                {
                    MatchId = 1,
                    Applied = true,
                    AlreadyMatched = false,
                    GoalsUpdated = 2,
                    Message = "Updated 2 goal scorer(s)."
                });

            SyncMatchResultDTO result = await _syncService.SyncResult(1);

            Assert.True(result.Applied);
            Assert.Equal(SyncScorerStatuses.Applied, result.ScorerStatus);
            Assert.Equal(2, result.ScorerGoalsUpdated);
            Assert.Equal("Updated 2 goal scorer(s).", result.ScorerMessage);
            Assert.Contains("Updated 2 goal scorer(s).", result.Message);
            _scorerApplyServiceMock.Verify(
                service => service.ApplyAsync(1, timelineEvents, true, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SyncResult_WhenTimelineFails_StillAppliesScoreAndReportsScorerWarning()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-SCORER-FAIL");
            match.ExternalStageId = "STAGE-FAIL";
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-SCORER-FAIL", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-SCORER-FAIL",
                    ExternalStageId = "STAGE-FAIL",
                    IsFinished = true,
                    HomeScore = 1,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });
            _eventsProviderMock
                .Setup(provider => provider.FetchGoalEventsAsync(
                    "FIFA-SCORER-FAIL",
                    "STAGE-FAIL",
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("FIFA timeline request failed with status 502."));

            SyncMatchResultDTO result = await _syncService.SyncResult(1);

            Assert.True(result.Applied);
            Assert.Equal(1, result.TeamOneScore);
            Assert.Equal(0, result.TeamTwoScore);
            Assert.Equal(SyncScorerStatuses.Warning, result.ScorerStatus);
            Assert.Contains("Scorer sync warning: FIFA timeline request failed", result.Warning);
            Assert.Contains("not applied", result.ScorerMessage);
            _scorerApplyServiceMock.Verify(
                service => service.ApplyAsync(
                    It.IsAny<int>(),
                    It.IsAny<ExternalMatchEvents>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SyncResult_WhenFifaHomeAwayReversed_PassesHomeMapsToTeamOneFalse()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-FLIP");
            match.ExternalStageId = "STAGE-FLIP";
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-FLIP", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-FLIP",
                    ExternalStageId = "STAGE-FLIP",
                    IsFinished = true,
                    HomeScore = 1,
                    AwayScore = 2,
                    HomeTeamName = "France",
                    AwayTeamName = "Brazil",
                    StatusLabel = "MatchStatus=0"
                });

            ExternalMatchEvents timelineEvents = new ExternalMatchEvents
            {
                ExternalMatchId = "FIFA-FLIP",
                Goals = Array.Empty<ExternalMatchGoalEvent>()
            };
            _eventsProviderMock
                .Setup(provider => provider.FetchGoalEventsAsync(
                    "FIFA-FLIP",
                    "STAGE-FLIP",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(timelineEvents);
            _scorerApplyServiceMock
                .Setup(service => service.ApplyAsync(
                    1,
                    timelineEvents,
                    false,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TimelineScorerApplyResult
                {
                    MatchId = 1,
                    Applied = false,
                    AlreadyMatched = true,
                    GoalsUpdated = 0,
                    Message = "Scorers already matched timeline."
                });

            SyncMatchResultDTO result = await _syncService.SyncResult(1);

            Assert.Equal(2, result.TeamOneScore);
            Assert.Equal(1, result.TeamTwoScore);
            Assert.Equal(SyncScorerStatuses.AlreadyMatched, result.ScorerStatus);
            _scorerApplyServiceMock.Verify(
                service => service.ApplyAsync(1, timelineEvents, false, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SyncFinishedResults_CountsScorerAppliedAndWarningsWithoutAbortingBatch()
        {
            MatchEntity successMatch = CreateFinishedMatch(externalMatchId: "FIFA-OK");
            successMatch.ExternalStageId = "STAGE-OK";
            MatchEntity warnMatch = new MatchEntity
            {
                Id = 2,
                Date = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 5)),
                StadiumId = 5,
                TeamOneId = 10,
                TeamTwoId = 20,
                ExternalMatchId = "FIFA-WARN",
                ExternalStageId = "STAGE-WARN"
            };
            _matches.Add(successMatch);
            _matches.Add(warnMatch);

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
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(2)).ReturnsAsync(warnMatch);

            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-OK", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-OK",
                    ExternalStageId = "STAGE-OK",
                    IsFinished = true,
                    HomeScore = 1,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France"
                });
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-WARN", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-WARN",
                    ExternalStageId = "STAGE-WARN",
                    IsFinished = true,
                    HomeScore = 0,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France"
                });

            ExternalMatchEvents okEvents = new ExternalMatchEvents { ExternalMatchId = "FIFA-OK" };
            _eventsProviderMock
                .Setup(provider => provider.FetchGoalEventsAsync(
                    "FIFA-OK",
                    "STAGE-OK",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(okEvents);
            _eventsProviderMock
                .Setup(provider => provider.FetchGoalEventsAsync(
                    "FIFA-WARN",
                    "STAGE-WARN",
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("count mismatch"));

            _scorerApplyServiceMock
                .Setup(service => service.ApplyAsync(
                    1,
                    okEvents,
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TimelineScorerApplyResult
                {
                    MatchId = 1,
                    Applied = true,
                    GoalsUpdated = 1,
                    Message = "Updated 1 goal scorer(s)."
                });

            SyncFinishedResultsDTO result = await _syncService.SyncFinishedResults(2026);

            Assert.Equal(2, result.MatchesAttempted);
            Assert.Equal(2, result.MatchesApplied);
            Assert.Equal(1, result.ScorersApplied);
            Assert.Equal(1, result.ScorerWarnings);
            Assert.Contains("scorers applied 1", result.Message);
            Assert.Contains("scorer warnings 1", result.Message);
            Assert.Contains(result.Results, syncResult =>
                syncResult.MatchId == 1 && syncResult.ScorerStatus == SyncScorerStatuses.Applied);
            Assert.Contains(result.Results, syncResult =>
                syncResult.MatchId == 2
                && syncResult.Applied
                && syncResult.ScorerStatus == SyncScorerStatuses.Warning);
        }

        [Fact]
        public async Task SyncResult_WhenApplyThrows_StillAppliesScoreAndReportsScorerWarning()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-APPLY-FAIL");
            match.ExternalStageId = "STAGE-APPLY-FAIL";
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-APPLY-FAIL", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-APPLY-FAIL",
                    ExternalStageId = "STAGE-APPLY-FAIL",
                    IsFinished = true,
                    HomeScore = 2,
                    AwayScore = 1,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });

            ExternalMatchEvents timelineEvents = new ExternalMatchEvents
            {
                ExternalMatchId = "FIFA-APPLY-FAIL",
                ExternalStageId = "STAGE-APPLY-FAIL",
                Goals = Array.Empty<ExternalMatchGoalEvent>()
            };
            _eventsProviderMock
                .Setup(provider => provider.FetchGoalEventsAsync(
                    "FIFA-APPLY-FAIL",
                    "STAGE-APPLY-FAIL",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(timelineEvents);
            _scorerApplyServiceMock
                .Setup(service => service.ApplyAsync(
                    1,
                    timelineEvents,
                    true,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Timeline goal counts do not match local Goal rows."));

            SyncMatchResultDTO result = await _syncService.SyncResult(1);

            Assert.True(result.Applied);
            Assert.Equal(2, result.TeamOneScore);
            Assert.Equal(1, result.TeamTwoScore);
            Assert.Equal(SyncScorerStatuses.Warning, result.ScorerStatus);
            Assert.Equal(0, result.ScorerGoalsUpdated);
            Assert.Contains("not applied", result.ScorerMessage);
            Assert.Contains("Timeline goal counts do not match", result.Warning);
            Assert.Equal(3, _goals.Count);
        }

        [Fact]
        public async Task SyncResult_WhenScorerApplyCanceled_RethrowsOperationCanceledException()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-CANCEL");
            match.ExternalStageId = "STAGE-CANCEL";
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-CANCEL", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-CANCEL",
                    ExternalStageId = "STAGE-CANCEL",
                    IsFinished = true,
                    HomeScore = 1,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            _eventsProviderMock
                .Setup(provider => provider.FetchGoalEventsAsync(
                    "FIFA-CANCEL",
                    "STAGE-CANCEL",
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException(cancellationTokenSource.Token));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => _syncService.SyncResult(1, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task SyncResult_WhenApplyReturnsNeitherAppliedNorMatched_ReportsSkipped()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-SKIP-APPLY");
            match.ExternalStageId = "STAGE-SKIP";
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-SKIP-APPLY", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-SKIP-APPLY",
                    ExternalStageId = "STAGE-SKIP",
                    IsFinished = true,
                    HomeScore = 0,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });

            ExternalMatchEvents timelineEvents = new ExternalMatchEvents
            {
                ExternalMatchId = "FIFA-SKIP-APPLY",
                ExternalStageId = "STAGE-SKIP",
                Goals = Array.Empty<ExternalMatchGoalEvent>()
            };
            _eventsProviderMock
                .Setup(provider => provider.FetchGoalEventsAsync(
                    "FIFA-SKIP-APPLY",
                    "STAGE-SKIP",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(timelineEvents);
            _scorerApplyServiceMock
                .Setup(service => service.ApplyAsync(
                    1,
                    timelineEvents,
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TimelineScorerApplyResult
                {
                    MatchId = 1,
                    Applied = false,
                    AlreadyMatched = false,
                    GoalsUpdated = 0,
                    Message = "No timeline Goal! events to apply."
                });

            SyncMatchResultDTO result = await _syncService.SyncResult(1);

            Assert.True(result.Applied);
            Assert.Equal(SyncScorerStatuses.Skipped, result.ScorerStatus);
            Assert.Equal(0, result.ScorerGoalsUpdated);
            Assert.Equal("No timeline Goal! events to apply.", result.ScorerMessage);
            Assert.Contains("No timeline Goal! events to apply.", result.Message);
        }

        [Fact]
        public async Task SyncScorers_WhenNoExternalMatchId_ThrowsInvalidOperationException()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: null);
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _syncService.SyncScorers(1));

            Assert.Contains("no ExternalMatchId", exception.Message);
            _resultProviderMock.Verify(
                provider => provider.FetchResultAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SyncScorers_WhenExternalNotFinished_ReturnsAppliedFalseWithoutCallingTimeline()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-LIVE");
            match.ExternalStageId = "STAGE-LIVE";
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-LIVE", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-LIVE",
                    ExternalStageId = "STAGE-LIVE",
                    IsFinished = false,
                    HomeScore = 1,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=3"
                });

            SyncMatchResultDTO result = await _syncService.SyncScorers(1);

            Assert.False(result.Applied);
            Assert.False(result.ScoreChanged);
            Assert.Null(result.ScorerStatus);
            Assert.Contains("not finished", result.Message);
            _eventsProviderMock.Verify(
                provider => provider.FetchGoalEventsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
            _scorerApplyServiceMock.Verify(
                service => service.ApplyAsync(
                    It.IsAny<int>(),
                    It.IsAny<ExternalMatchEvents>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
            _betServiceMock.Verify(betService => betService.ResolveBetsForMatch(It.IsAny<int>()), Times.Never);
            _knockoutServiceMock.Verify(
                knockoutService => knockoutService.TryAdvanceFromMatch(It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task SyncScorers_WhenNoExternalStageId_SkipsWithoutCallingTimeline()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-BF-NO-STAGE");
            match.ExternalStageId = null;
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-BF-NO-STAGE", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-BF-NO-STAGE",
                    IsFinished = true,
                    HomeScore = 2,
                    AwayScore = 1,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });

            SyncMatchResultDTO result = await _syncService.SyncScorers(1);

            Assert.False(result.Applied);
            Assert.False(result.ScoreChanged);
            Assert.Equal(0, result.BetsResolved);
            Assert.Equal(SyncScorerStatuses.Skipped, result.ScorerStatus);
            Assert.Contains("no ExternalStageId", result.ScorerMessage);
            _eventsProviderMock.Verify(
                provider => provider.FetchGoalEventsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
            _scorerApplyServiceMock.Verify(
                service => service.ApplyAsync(
                    It.IsAny<int>(),
                    It.IsAny<ExternalMatchEvents>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SyncScorers_WhenStagePresent_AppliesTimelineScorersWithoutChangingScoreOrResolvingBets()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-BF-APPLY");
            match.ExternalStageId = "STAGE-BF";
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);

            _goals.Add(new Goal
            {
                Id = 1,
                TeamStatsId = 100,
                PlayerId = 501,
                TimeScored = match.Date.AddMinutes(12),
                IsOwnGoal = 0
            });
            _goals.Add(new Goal
            {
                Id = 2,
                TeamStatsId = 100,
                PlayerId = 501,
                TimeScored = match.Date.AddMinutes(40),
                IsOwnGoal = 0
            });
            _goals.Add(new Goal
            {
                Id = 3,
                TeamStatsId = 101,
                PlayerId = 502,
                TimeScored = match.Date.AddMinutes(70),
                IsOwnGoal = 0
            });
            int goalCountBefore = _goals.Count;

            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-BF-APPLY", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-BF-APPLY",
                    ExternalStageId = "STAGE-BF",
                    IsFinished = true,
                    HomeScore = 9,
                    AwayScore = 9,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });

            ExternalMatchEvents timelineEvents = new ExternalMatchEvents
            {
                ExternalMatchId = "FIFA-BF-APPLY",
                ExternalStageId = "STAGE-BF",
                Goals = new List<ExternalMatchGoalEvent>
                {
                    new ExternalMatchGoalEvent
                    {
                        ExternalMatchId = "FIFA-BF-APPLY",
                        PlayerDisplayName = "Richarlison",
                        Minute = 12,
                        IsHomeSide = true,
                        EventTypeLabel = "Goal!"
                    },
                    new ExternalMatchGoalEvent
                    {
                        ExternalMatchId = "FIFA-BF-APPLY",
                        PlayerDisplayName = "Richarlison",
                        Minute = 40,
                        IsHomeSide = true,
                        EventTypeLabel = "Goal!"
                    },
                    new ExternalMatchGoalEvent
                    {
                        ExternalMatchId = "FIFA-BF-APPLY",
                        PlayerDisplayName = "Mbappé",
                        Minute = 70,
                        IsHomeSide = false,
                        EventTypeLabel = "Goal!"
                    }
                }
            };
            _eventsProviderMock
                .Setup(provider => provider.FetchGoalEventsAsync(
                    "FIFA-BF-APPLY",
                    "STAGE-BF",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(timelineEvents);
            _scorerApplyServiceMock
                .Setup(service => service.ApplyAsync(
                    1,
                    timelineEvents,
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TimelineScorerApplyResult
                {
                    MatchId = 1,
                    Applied = true,
                    AlreadyMatched = false,
                    GoalsUpdated = 3,
                    Message = "Updated 3 goal scorer(s)."
                });

            SyncMatchResultDTO result = await _syncService.SyncScorers(1);

            Assert.True(result.Applied);
            Assert.False(result.ScoreChanged);
            Assert.Equal(0, result.BetsResolved);
            Assert.Equal(2, result.TeamOneScore);
            Assert.Equal(1, result.TeamTwoScore);
            Assert.Equal(SyncScorerStatuses.Applied, result.ScorerStatus);
            Assert.Equal(3, result.ScorerGoalsUpdated);
            Assert.Equal(goalCountBefore, _goals.Count);
            Assert.Contains("FT unchanged 2-1", result.Message);
            _goalRepositoryMock.Verify(repository => repository.Create(It.IsAny<Goal>()), Times.Never);
            _goalRepositoryMock.Verify(repository => repository.Delete(It.IsAny<Goal>()), Times.Never);
            _betServiceMock.Verify(betService => betService.ResolveBetsForMatch(It.IsAny<int>()), Times.Never);
            _knockoutServiceMock.Verify(
                knockoutService => knockoutService.TryAdvanceFromMatch(It.IsAny<int>()),
                Times.Never);
            _scorerApplyServiceMock.Verify(
                service => service.ApplyAsync(1, timelineEvents, true, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SyncScorers_WhenTimelineHasNoGoals_SkipsWithoutCallingApply()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-BF-EMPTY");
            match.ExternalStageId = "STAGE-EMPTY";
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-BF-EMPTY", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-BF-EMPTY",
                    ExternalStageId = "STAGE-EMPTY",
                    IsFinished = true,
                    HomeScore = 1,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });

            ExternalMatchEvents emptyTimeline = new ExternalMatchEvents
            {
                ExternalMatchId = "FIFA-BF-EMPTY",
                ExternalStageId = "STAGE-EMPTY",
                Goals = Array.Empty<ExternalMatchGoalEvent>()
            };
            _eventsProviderMock
                .Setup(provider => provider.FetchGoalEventsAsync(
                    "FIFA-BF-EMPTY",
                    "STAGE-EMPTY",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyTimeline);

            SyncMatchResultDTO result = await _syncService.SyncScorers(1);

            Assert.False(result.Applied);
            Assert.Equal(SyncScorerStatuses.Skipped, result.ScorerStatus);
            Assert.Contains("no timeline Goal!", result.ScorerMessage);
            _eventsProviderMock.Verify(
                provider => provider.FetchGoalEventsAsync(
                    "FIFA-BF-EMPTY",
                    "STAGE-EMPTY",
                    It.IsAny<CancellationToken>()),
                Times.Once);
            _scorerApplyServiceMock.Verify(
                service => service.ApplyAsync(
                    It.IsAny<int>(),
                    It.IsAny<ExternalMatchEvents>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
            _betServiceMock.Verify(betService => betService.ResolveBetsForMatch(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SyncScorers_WhenTimelineFails_ReportsScorerWarningWithoutTouchingBets()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-BF-WARN");
            match.ExternalStageId = "STAGE-WARN";
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-BF-WARN", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-BF-WARN",
                    ExternalStageId = "STAGE-WARN",
                    IsFinished = true,
                    HomeScore = 1,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });
            _eventsProviderMock
                .Setup(provider => provider.FetchGoalEventsAsync(
                    "FIFA-BF-WARN",
                    "STAGE-WARN",
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("FIFA timeline request failed with status 502."));

            SyncMatchResultDTO result = await _syncService.SyncScorers(1);

            Assert.False(result.Applied);
            Assert.False(result.ScoreChanged);
            Assert.Equal(0, result.BetsResolved);
            Assert.Equal(SyncScorerStatuses.Warning, result.ScorerStatus);
            Assert.Contains("Scorer sync warning: FIFA timeline request failed", result.Warning);
            Assert.Contains("not applied", result.ScorerMessage);
            _betServiceMock.Verify(betService => betService.ResolveBetsForMatch(It.IsAny<int>()), Times.Never);
            _knockoutServiceMock.Verify(
                knockoutService => knockoutService.TryAdvanceFromMatch(It.IsAny<int>()),
                Times.Never);
            _scorerApplyServiceMock.Verify(
                service => service.ApplyAsync(
                    It.IsAny<int>(),
                    It.IsAny<ExternalMatchEvents>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SyncScorers_WhenFifaHomeAwayReversed_PassesHomeMapsToTeamOneFalse()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-BF-FLIP");
            match.ExternalStageId = "STAGE-BF-FLIP";
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _goals.Add(new Goal
            {
                Id = 11,
                TeamStatsId = 100,
                PlayerId = 501,
                TimeScored = match.Date.AddMinutes(20),
                IsOwnGoal = 0
            });
            _goals.Add(new Goal
            {
                Id = 12,
                TeamStatsId = 100,
                PlayerId = 501,
                TimeScored = match.Date.AddMinutes(55),
                IsOwnGoal = 0
            });
            _goals.Add(new Goal
            {
                Id = 13,
                TeamStatsId = 101,
                PlayerId = 502,
                TimeScored = match.Date.AddMinutes(10),
                IsOwnGoal = 0
            });

            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-BF-FLIP", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-BF-FLIP",
                    ExternalStageId = "STAGE-BF-FLIP",
                    IsFinished = true,
                    HomeScore = 1,
                    AwayScore = 2,
                    HomeTeamName = "France",
                    AwayTeamName = "Brazil",
                    StatusLabel = "MatchStatus=0"
                });

            ExternalMatchEvents timelineEvents = new ExternalMatchEvents
            {
                ExternalMatchId = "FIFA-BF-FLIP",
                ExternalStageId = "STAGE-BF-FLIP",
                Goals = new List<ExternalMatchGoalEvent>
                {
                    new ExternalMatchGoalEvent
                    {
                        ExternalMatchId = "FIFA-BF-FLIP",
                        PlayerDisplayName = "Mbappé",
                        Minute = 10,
                        IsHomeSide = true,
                        EventTypeLabel = "Goal!"
                    }
                }
            };
            _eventsProviderMock
                .Setup(provider => provider.FetchGoalEventsAsync(
                    "FIFA-BF-FLIP",
                    "STAGE-BF-FLIP",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(timelineEvents);
            _scorerApplyServiceMock
                .Setup(service => service.ApplyAsync(
                    1,
                    timelineEvents,
                    false,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TimelineScorerApplyResult
                {
                    MatchId = 1,
                    Applied = true,
                    AlreadyMatched = false,
                    GoalsUpdated = 1,
                    Message = "Updated 1 goal scorer(s)."
                });

            SyncMatchResultDTO result = await _syncService.SyncScorers(1);

            Assert.True(result.Applied);
            Assert.Equal(2, result.TeamOneScore);
            Assert.Equal(1, result.TeamTwoScore);
            Assert.Equal(SyncScorerStatuses.Applied, result.ScorerStatus);
            _scorerApplyServiceMock.Verify(
                service => service.ApplyAsync(1, timelineEvents, false, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SyncScorers_WhenCanceled_RethrowsOperationCanceledException()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-BF-CANCEL");
            match.ExternalStageId = "STAGE-BF-CANCEL";
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-BF-CANCEL", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-BF-CANCEL",
                    ExternalStageId = "STAGE-BF-CANCEL",
                    IsFinished = true,
                    HomeScore = 1,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            _eventsProviderMock
                .Setup(provider => provider.FetchGoalEventsAsync(
                    "FIFA-BF-CANCEL",
                    "STAGE-BF-CANCEL",
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException(cancellationTokenSource.Token));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => _syncService.SyncScorers(1, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task SyncScorersForWorldCup_CountsAppliedAndWarningsWithoutAbortingBatch()
        {
            MatchEntity successMatch = CreateFinishedMatch(externalMatchId: "FIFA-BF-OK");
            successMatch.ExternalStageId = "STAGE-BF-OK";
            MatchEntity warnMatch = new MatchEntity
            {
                Id = 2,
                Date = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 5)),
                StadiumId = 5,
                TeamOneId = 10,
                TeamTwoId = 20,
                ExternalMatchId = "FIFA-BF-WARN-BATCH",
                ExternalStageId = "STAGE-BF-WARN"
            };
            _matches.Add(successMatch);
            _matches.Add(warnMatch);

            _teamStats.Add(new TeamStats
            {
                Id = 200,
                MatchId = 2,
                TeamId = 10,
                Possession = 50,
                Shots = 0,
                ShotsOnTarget = 0,
                Points = 0
            });
            _teamStats.Add(new TeamStats
            {
                Id = 201,
                MatchId = 2,
                TeamId = 20,
                Possession = 50,
                Shots = 0,
                ShotsOnTarget = 0,
                Points = 0
            });

            _matchServiceMock
                .Setup(matchService => matchService.GetFixturesByWorldCup(2026))
                .Returns(new List<MatchDTO>
                {
                    new MatchDTO { Id = 1 },
                    new MatchDTO { Id = 2 }
                });

            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(successMatch);
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(2)).ReturnsAsync(warnMatch);

            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-BF-OK", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-BF-OK",
                    ExternalStageId = "STAGE-BF-OK",
                    IsFinished = true,
                    HomeScore = 1,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France"
                });
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync("FIFA-BF-WARN-BATCH", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-BF-WARN-BATCH",
                    ExternalStageId = "STAGE-BF-WARN",
                    IsFinished = true,
                    HomeScore = 0,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France"
                });

            ExternalMatchEvents okEvents = new ExternalMatchEvents
            {
                ExternalMatchId = "FIFA-BF-OK",
                ExternalStageId = "STAGE-BF-OK",
                Goals = new List<ExternalMatchGoalEvent>
                {
                    new ExternalMatchGoalEvent
                    {
                        ExternalMatchId = "FIFA-BF-OK",
                        PlayerDisplayName = "Richarlison",
                        Minute = 15,
                        IsHomeSide = true,
                        EventTypeLabel = "Goal!"
                    }
                }
            };
            _eventsProviderMock
                .Setup(provider => provider.FetchGoalEventsAsync(
                    "FIFA-BF-OK",
                    "STAGE-BF-OK",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(okEvents);
            _eventsProviderMock
                .Setup(provider => provider.FetchGoalEventsAsync(
                    "FIFA-BF-WARN-BATCH",
                    "STAGE-BF-WARN",
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("count mismatch"));

            _scorerApplyServiceMock
                .Setup(service => service.ApplyAsync(
                    1,
                    okEvents,
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TimelineScorerApplyResult
                {
                    MatchId = 1,
                    Applied = true,
                    GoalsUpdated = 1,
                    Message = "Updated 1 goal scorer(s)."
                });

            SyncFinishedResultsDTO result = await _syncService.SyncScorersForWorldCup(2026);

            Assert.Equal(2, result.MatchesAttempted);
            Assert.Equal(1, result.MatchesApplied);
            Assert.Equal(0, result.TotalBetsResolved);
            Assert.Equal(1, result.ScorersApplied);
            Assert.Equal(1, result.ScorerWarnings);
            Assert.Contains("Scorers-only", result.Message);
            Assert.Contains("scorer warnings 1", result.Message);
            Assert.Contains(result.Results, syncResult =>
                syncResult.MatchId == 1
                && syncResult.Applied
                && syncResult.ScorerStatus == SyncScorerStatuses.Applied);
            Assert.Contains(result.Results, syncResult =>
                syncResult.MatchId == 2
                && !syncResult.Applied
                && syncResult.ScorerStatus == SyncScorerStatuses.Warning);
            _betServiceMock.Verify(betService => betService.ResolveBetsForMatch(It.IsAny<int>()), Times.Never);
            _knockoutServiceMock.Verify(
                knockoutService => knockoutService.TryAdvanceFromMatch(It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task SyncScorersForWorldCup_WhenCanceled_RethrowsOperationCanceledException()
        {
            MatchEntity match = CreateFinishedMatch(externalMatchId: "FIFA-BF-BATCH-CANCEL");
            match.ExternalStageId = "STAGE-BF-BATCH-CANCEL";
            _matches.Add(match);

            _matchServiceMock
                .Setup(matchService => matchService.GetFixturesByWorldCup(2026))
                .Returns(new List<MatchDTO>
                {
                    new MatchDTO { Id = 1 }
                });
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(match);
            _resultProviderMock
                .Setup(provider => provider.FetchResultAsync(
                    "FIFA-BF-BATCH-CANCEL",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExternalMatchResult
                {
                    ExternalMatchId = "FIFA-BF-BATCH-CANCEL",
                    ExternalStageId = "STAGE-BF-BATCH-CANCEL",
                    IsFinished = true,
                    HomeScore = 1,
                    AwayScore = 0,
                    HomeTeamName = "Brazil",
                    AwayTeamName = "France",
                    StatusLabel = "MatchStatus=0"
                });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            _eventsProviderMock
                .Setup(provider => provider.FetchGoalEventsAsync(
                    "FIFA-BF-BATCH-CANCEL",
                    "STAGE-BF-BATCH-CANCEL",
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException(cancellationTokenSource.Token));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => _syncService.SyncScorersForWorldCup(2026, cancellationTokenSource.Token));
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
