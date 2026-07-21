using System.Linq.Expressions;
using Core.Services.MatchSync;
using Data.Entities;
using Data.Repos;
using Moq;
using MatchEntity = Data.Entities.Match;

namespace WorldCup_System.Tests.Services.MatchSync
{
    public class TimelineScorerApplyServiceTests
    {
        private const int MatchId = 1;
        private const int TeamOneId = 10;
        private const int TeamTwoId = 20;
        private const int TeamOneStatsId = 100;
        private const int TeamTwoStatsId = 101;

        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<MatchEntity>> _matchRepositoryMock;
        private readonly Mock<IRepository<TeamStats>> _teamStatsRepositoryMock;
        private readonly Mock<IRepository<Goal>> _goalRepositoryMock;
        private readonly Mock<ITimelinePlayerResolver> _playerResolverMock;
        private readonly TimelineScorerApplyService _service;

        private readonly DateTime _kickoff = new DateTime(2026, 6, 11, 18, 0, 0, DateTimeKind.Utc);
        private readonly List<TeamStats> _teamStats;
        private readonly List<Goal> _goals;
        private MatchEntity _match;

        public TimelineScorerApplyServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _matchRepositoryMock = new Mock<IRepository<MatchEntity>>();
            _teamStatsRepositoryMock = new Mock<IRepository<TeamStats>>();
            _goalRepositoryMock = new Mock<IRepository<Goal>>();
            _playerResolverMock = new Mock<ITimelinePlayerResolver>();

            _match = new MatchEntity
            {
                Id = MatchId,
                Date = _kickoff,
                TeamOneId = TeamOneId,
                TeamTwoId = TeamTwoId,
                ExternalMatchId = "400021443",
                ExternalStageId = "289273",
                StadiumId = 1,
                Stage = MatchStage.Group
            };

            _teamStats = new List<TeamStats>
            {
                new TeamStats
                {
                    Id = TeamOneStatsId,
                    MatchId = MatchId,
                    TeamId = TeamOneId,
                    Possession = 50,
                    Shots = 0,
                    ShotsOnTarget = 0,
                    Points = 0
                },
                new TeamStats
                {
                    Id = TeamTwoStatsId,
                    MatchId = MatchId,
                    TeamId = TeamTwoId,
                    Possession = 50,
                    Shots = 0,
                    ShotsOnTarget = 0,
                    Points = 0
                }
            };

            _goals = new List<Goal>();

            _repositoryManagerMock.Setup(manager => manager.Match).Returns(_matchRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.TeamStats).Returns(_teamStatsRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.Goal).Returns(_goalRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.SaveAsync()).Returns(Task.CompletedTask);

            _matchRepositoryMock
                .Setup(repository => repository.GetByIdAsync(MatchId))
                .ReturnsAsync(() => _match);

            SetupFind(_teamStatsRepositoryMock, _teamStats);
            SetupFind(_goalRepositoryMock, _goals);

            _goalRepositoryMock
                .Setup(repository => repository.Update(It.IsAny<Goal>()));

            _service = new TimelineScorerApplyService(
                _repositoryManagerMock.Object,
                _playerResolverMock.Object);
        }

        [Fact]
        public async Task ApplyAsync_WhenCountsAlignWithWrongScorers_UpdatesPlayerIdAndMinute()
        {
            _goals.Add(new Goal
            {
                Id = 1,
                TeamStatsId = TeamOneStatsId,
                PlayerId = 501,
                TimeScored = _kickoff.AddMinutes(1),
                IsOwnGoal = 0
            });
            _goals.Add(new Goal
            {
                Id = 2,
                TeamStatsId = TeamOneStatsId,
                PlayerId = 502,
                TimeScored = _kickoff.AddMinutes(2),
                IsOwnGoal = 0
            });

            SetupResolver(TeamOneId, "FIFA-Q", "Julian QUINONES", playerId: 601);
            SetupResolver(TeamOneId, "FIFA-J", "Raul JIMENEZ", playerId: 602);

            ExternalMatchEvents events = BuildEvents(
                GoalEvent(home: true, minute: 9, playerId: "FIFA-Q", name: "Julian QUINONES"),
                GoalEvent(home: true, minute: 67, playerId: "FIFA-J", name: "Raul JIMENEZ"));

            TimelineScorerApplyResult result = await _service.ApplyAsync(
                MatchId,
                events,
                homeMapsToTeamOne: true);

            Assert.True(result.Applied);
            Assert.False(result.AlreadyMatched);
            Assert.Equal(2, result.GoalsUpdated);
            Assert.Equal(601, _goals[0].PlayerId);
            Assert.Equal(_kickoff.AddMinutes(9), _goals[0].TimeScored);
            Assert.Equal(602, _goals[1].PlayerId);
            Assert.Equal(_kickoff.AddMinutes(67), _goals[1].TimeScored);
            _repositoryManagerMock.Verify(manager => manager.SaveAsync(), Times.Once);
            _goalRepositoryMock.Verify(repository => repository.Update(It.IsAny<Goal>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ApplyAsync_WhenAlreadyMatched_IsIdempotent()
        {
            _goals.Add(new Goal
            {
                Id = 1,
                TeamStatsId = TeamOneStatsId,
                PlayerId = 501,
                TimeScored = _kickoff.AddMinutes(1),
                IsOwnGoal = 0
            });
            _goals.Add(new Goal
            {
                Id = 2,
                TeamStatsId = TeamOneStatsId,
                PlayerId = 502,
                TimeScored = _kickoff.AddMinutes(2),
                IsOwnGoal = 0
            });

            SetupResolver(TeamOneId, "FIFA-Q", "Julian QUINONES", playerId: 601);
            SetupResolver(TeamOneId, "FIFA-J", "Raul JIMENEZ", playerId: 602);

            ExternalMatchEvents events = BuildEvents(
                GoalEvent(home: true, minute: 9, playerId: "FIFA-Q", name: "Julian QUINONES"),
                GoalEvent(home: true, minute: 67, playerId: "FIFA-J", name: "Raul JIMENEZ"));

            TimelineScorerApplyResult first = await _service.ApplyAsync(MatchId, events, homeMapsToTeamOne: true);
            TimelineScorerApplyResult second = await _service.ApplyAsync(MatchId, events, homeMapsToTeamOne: true);

            Assert.True(first.Applied);
            Assert.Equal(2, first.GoalsUpdated);
            Assert.True(second.AlreadyMatched);
            Assert.False(second.Applied);
            Assert.Equal(0, second.GoalsUpdated);
            Assert.Equal(601, _goals[0].PlayerId);
            Assert.Equal(602, _goals[1].PlayerId);
            _repositoryManagerMock.Verify(manager => manager.SaveAsync(), Times.Once);
            _goalRepositoryMock.Verify(repository => repository.Update(It.IsAny<Goal>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ApplyAsync_WhenCountsDisagree_ThrowsAndDoesNotUpdate()
        {
            _goals.Add(new Goal
            {
                Id = 1,
                TeamStatsId = TeamOneStatsId,
                PlayerId = 501,
                TimeScored = _kickoff.AddMinutes(1),
                IsOwnGoal = 0
            });

            ExternalMatchEvents events = BuildEvents(
                GoalEvent(home: true, minute: 9, playerId: "FIFA-Q", name: "Julian QUINONES"),
                GoalEvent(home: true, minute: 67, playerId: "FIFA-J", name: "Raul JIMENEZ"));

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ApplyAsync(MatchId, events, homeMapsToTeamOne: true));

            Assert.Contains("do not match", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(501, _goals[0].PlayerId);
            _repositoryManagerMock.Verify(manager => manager.SaveAsync(), Times.Never);
            _playerResolverMock.Verify(
                resolver => resolver.ResolveAsync(
                    It.IsAny<int>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ApplyAsync_WhenHomeMapsToTeamTwo_OrientsEventsCorrectly()
        {
            _goals.Add(new Goal
            {
                Id = 1,
                TeamStatsId = TeamTwoStatsId,
                PlayerId = 501,
                TimeScored = _kickoff.AddMinutes(1),
                IsOwnGoal = 0
            });

            SetupResolver(TeamTwoId, "FIFA-Q", "Julian QUINONES", playerId: 701);

            ExternalMatchEvents events = BuildEvents(
                GoalEvent(home: true, minute: 9, playerId: "FIFA-Q", name: "Julian QUINONES"));

            TimelineScorerApplyResult result = await _service.ApplyAsync(
                MatchId,
                events,
                homeMapsToTeamOne: false);

            Assert.True(result.Applied);
            Assert.Equal(701, _goals[0].PlayerId);
            Assert.Equal(TeamTwoStatsId, _goals[0].TeamStatsId);
            _playerResolverMock.Verify(
                resolver => resolver.ResolveAsync(
                    TeamTwoId,
                    "FIFA-Q",
                    "Julian QUINONES",
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ApplyAsync_OwnGoal_ResolvesPlayerOnConcedingTeam()
        {
            _goals.Add(new Goal
            {
                Id = 1,
                TeamStatsId = TeamOneStatsId,
                PlayerId = 501,
                TimeScored = _kickoff.AddMinutes(1),
                IsOwnGoal = 0
            });

            SetupResolver(TeamTwoId, "FIFA-OG", "Own Goal Guy", playerId: 802);

            ExternalMatchEvents events = BuildEvents(
                new ExternalMatchGoalEvent
                {
                    ExternalMatchId = "400021443",
                    ExternalPlayerId = "FIFA-OG",
                    PlayerDisplayName = "Own Goal Guy",
                    Minute = 44,
                    IsHomeSide = true,
                    IsOwnGoal = true,
                    IsPenalty = false
                });

            TimelineScorerApplyResult result = await _service.ApplyAsync(
                MatchId,
                events,
                homeMapsToTeamOne: true);

            Assert.True(result.Applied);
            Assert.Equal(802, _goals[0].PlayerId);
            Assert.Equal(1, _goals[0].IsOwnGoal);
            Assert.Equal(_kickoff.AddMinutes(44), _goals[0].TimeScored);
            _playerResolverMock.Verify(
                resolver => resolver.ResolveAsync(
                    TeamTwoId,
                    "FIFA-OG",
                    "Own Goal Guy",
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ApplyAsync_WhenSameMinuteGoals_PreservesTimelineOrder()
        {
            _goals.Add(new Goal
            {
                Id = 1,
                TeamStatsId = TeamOneStatsId,
                PlayerId = 501,
                TimeScored = _kickoff.AddMinutes(1),
                IsOwnGoal = 0
            });
            _goals.Add(new Goal
            {
                Id = 2,
                TeamStatsId = TeamOneStatsId,
                PlayerId = 502,
                TimeScored = _kickoff.AddMinutes(2),
                IsOwnGoal = 0
            });

            // Alphabetically ExternalPlayerId "Z" before "A" — wrong order if we sort by id.
            SetupResolver(TeamOneId, "FIFA-Z", "Second Scorer", playerId: 702);
            SetupResolver(TeamOneId, "FIFA-A", "First Scorer", playerId: 701);

            ExternalMatchEvents events = BuildEvents(
                GoalEvent(home: true, minute: 45, playerId: "FIFA-Z", name: "Second Scorer"),
                GoalEvent(home: true, minute: 45, playerId: "FIFA-A", name: "First Scorer"));

            TimelineScorerApplyResult first = await _service.ApplyAsync(MatchId, events, homeMapsToTeamOne: true);
            TimelineScorerApplyResult second = await _service.ApplyAsync(MatchId, events, homeMapsToTeamOne: true);

            Assert.True(first.Applied);
            Assert.Equal(702, _goals[0].PlayerId);
            Assert.Equal(701, _goals[1].PlayerId);
            Assert.Equal(_kickoff.AddMinutes(45), _goals[0].TimeScored);
            Assert.Equal(_kickoff.AddMinutes(45), _goals[1].TimeScored);
            Assert.True(second.AlreadyMatched);
            Assert.False(second.Applied);
            _repositoryManagerMock.Verify(manager => manager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task ApplyAsync_WhenZeroZero_ReturnsAlreadyMatchedWithoutSave()
        {
            ExternalMatchEvents events = BuildEvents();

            TimelineScorerApplyResult result = await _service.ApplyAsync(
                MatchId,
                events,
                homeMapsToTeamOne: true);

            Assert.True(result.AlreadyMatched);
            Assert.False(result.Applied);
            Assert.Equal(0, result.GoalsUpdated);
            _repositoryManagerMock.Verify(manager => manager.SaveAsync(), Times.Never);
            _playerResolverMock.Verify(
                resolver => resolver.ResolveAsync(
                    It.IsAny<int>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ApplyAsync_WhenExternalMatchIdMismatch_Throws()
        {
            ExternalMatchEvents events = new ExternalMatchEvents
            {
                ExternalMatchId = "999999",
                Goals = Array.Empty<ExternalMatchGoalEvent>()
            };

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ApplyAsync(MatchId, events, homeMapsToTeamOne: true));

            Assert.Contains("does not match", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ApplyAsync_WhenTeamStatsMissing_Throws()
        {
            _teamStats.Clear();

            ExternalMatchEvents events = BuildEvents();

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ApplyAsync(MatchId, events, homeMapsToTeamOne: true));

            Assert.Contains("TeamStats missing", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ApplyAsync_DoesNotInvokeBetOrKnockoutDependencies()
        {
            // Apply service has no bet/knockout deps; this test documents the contract via composition.
            _goals.Add(new Goal
            {
                Id = 1,
                TeamStatsId = TeamOneStatsId,
                PlayerId = 501,
                TimeScored = _kickoff.AddMinutes(1),
                IsOwnGoal = 0
            });
            SetupResolver(TeamOneId, "FIFA-Q", "Julian QUINONES", playerId: 601);

            ExternalMatchEvents events = BuildEvents(
                GoalEvent(home: true, minute: 9, playerId: "FIFA-Q", name: "Julian QUINONES"));

            TimelineScorerApplyResult result = await _service.ApplyAsync(MatchId, events, homeMapsToTeamOne: true);

            Assert.True(result.Applied);
            Assert.IsType<TimelineScorerApplyService>(_service);
        }

        private void SetupResolver(int teamId, string? externalPlayerId, string? displayName, int playerId)
        {
            _playerResolverMock
                .Setup(resolver => resolver.ResolveAsync(
                    teamId,
                    externalPlayerId,
                    displayName,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TimelinePlayerResolveResult
                {
                    Player = new Player
                    {
                        Id = playerId,
                        Name = displayName ?? "Player",
                        Number = 9,
                        TeamId = teamId,
                        PositionId = 1,
                        ExternalPlayerId = externalPlayerId
                    },
                    MatchMethod = TimelinePlayerMatchMethod.ExternalPlayerId
                });
        }

        private ExternalMatchEvents BuildEvents(params ExternalMatchGoalEvent[] goals)
        {
            return new ExternalMatchEvents
            {
                ExternalMatchId = "400021443",
                ExternalStageId = "289273",
                Goals = goals
            };
        }

        private static ExternalMatchGoalEvent GoalEvent(
            bool home,
            int minute,
            string? playerId,
            string? name,
            bool ownGoal = false)
        {
            return new ExternalMatchGoalEvent
            {
                ExternalMatchId = "400021443",
                ExternalPlayerId = playerId,
                PlayerDisplayName = name,
                Minute = minute,
                IsHomeSide = home,
                IsOwnGoal = ownGoal,
                IsPenalty = false
            };
        }

        private static void SetupFind<T>(Mock<IRepository<T>> repositoryMock, List<T> source)
            where T : class
        {
            repositoryMock
                .Setup(repository => repository.Find(It.IsAny<Expression<Func<T, bool>>>()))
                .Returns((Expression<Func<T, bool>> predicate) =>
                {
                    Func<T, bool> compiled = predicate.Compile();
                    return source.Where(compiled).AsQueryable();
                });
        }
    }
}
