using System.Linq.Expressions;
using Core.DTOs.Bets;
using Core.DTOs.Goals;
using Core.Services.Bets;
using Core.Services.Goals;
using Core.Services.Knockout;
using Data.Entities;
using Data.Repos;
using Moq;
using MatchEntity = Data.Entities.Match;

namespace WorldCup_System.Tests.Services.Goals
{
    public class GoalServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<MatchEntity>> _matchRepositoryMock;
        private readonly Mock<IRepository<TeamStats>> _teamStatsRepositoryMock;
        private readonly Mock<IRepository<Goal>> _goalRepositoryMock;
        private readonly Mock<IRepository<Player>> _playerRepositoryMock;
        private readonly Mock<IBetService> _betServiceMock;
        private readonly Mock<IKnockoutService> _knockoutServiceMock;
        private readonly GoalService _goalService;

        public GoalServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _matchRepositoryMock = new Mock<IRepository<MatchEntity>>();
            _teamStatsRepositoryMock = new Mock<IRepository<TeamStats>>();
            _goalRepositoryMock = new Mock<IRepository<Goal>>();
            _playerRepositoryMock = new Mock<IRepository<Player>>();
            _betServiceMock = new Mock<IBetService>();
            _knockoutServiceMock = new Mock<IKnockoutService>();

            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Match).Returns(_matchRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.TeamStats).Returns(_teamStatsRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Goal).Returns(_goalRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Player).Returns(_playerRepositoryMock.Object);
            _betServiceMock
                .Setup(betService => betService.ResolveBetsForMatch(It.IsAny<int>()))
                .ReturnsAsync(new ResolveBetsResultDTO { ResolvedCount = 0 });
            _knockoutServiceMock
                .Setup(knockoutService => knockoutService.TryAdvanceFromMatch(It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            _goalService = new GoalService(
                _repositoryManagerMock.Object,
                _betServiceMock.Object,
                _knockoutServiceMock.Object);
        }

        [Fact]
        public void GetGoalsByMatch_ReturnsMappedGoalsOrderedByMinute()
        {
            DateTime kickoff = new DateTime(2026, 6, 15, 18, 0, 0);
            List<MatchEntity> matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 }
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 2, TeamStatsId = 101, PlayerId = 2, TimeScored = kickoff.AddMinutes(70), IsOwnGoal = 0 },
                new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = kickoff.AddMinutes(25), IsOwnGoal = 0 }
            };
            List<Player> players = new List<Player>
            {
                new Player { Id = 1, Name = "Neymar", Number = 10, TeamId = 10, PositionId = 1 },
                new Player { Id = 2, Name = "Mbappe", Number = 7, TeamId = 20, PositionId = 1 }
            };

            SetupFind(_matchRepositoryMock, matches);
            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_goalRepositoryMock, goals);
            _playerRepositoryMock.Setup(playerRepository => playerRepository.GetAllAsync()).Returns(players.AsQueryable());

            List<GoalDTO> result = _goalService.GetGoalsByMatch(1);

            Assert.Equal(2, result.Count);
            Assert.Equal(25, result[0].Minute);
            Assert.Equal("Neymar", result[0].PlayerName);
            Assert.Equal(70, result[1].Minute);
        }

        [Fact]
        public void GetGoalsByMatch_WhenMatchNotFound_ReturnsEmptyList()
        {
            SetupFind(_matchRepositoryMock, new List<MatchEntity>());

            List<GoalDTO> result = _goalService.GetGoalsByMatch(99);

            Assert.Empty(result);
        }

        [Fact]
        public async Task AddGoal_CreatesGoalAndSaves()
        {
            DateTime kickoff = DateTime.UtcNow.AddDays(1);
            MatchEntity match = new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };
            TeamStats stats = new TeamStats { Id = 100, MatchId = 1, TeamId = 10 };
            Player player = new Player { Id = 1, Name = "Neymar", Number = 10, TeamId = 10, PositionId = 1 };
            AddGoalDTO addGoalDto = new AddGoalDTO
            {
                MatchId = 1,
                TeamId = 10,
                PlayerId = 1,
                Minute = 42,
                IsOwnGoal = false
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _playerRepositoryMock.Setup(playerRepository => playerRepository.GetByIdAsync(1)).ReturnsAsync(player);
            SetupFind(_teamStatsRepositoryMock, new List<TeamStats> { stats });

            Goal? capturedGoal = null;
            _goalRepositoryMock.Setup(goalRepository => goalRepository.Create(It.IsAny<Goal>()))
                .Callback<Goal>(goal => capturedGoal = goal);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _goalService.AddGoal(addGoalDto);

            Assert.NotNull(capturedGoal);
            Assert.Equal(100, capturedGoal.TeamStatsId);
            Assert.Equal(kickoff.AddMinutes(42), capturedGoal.TimeScored);
            Assert.Equal(0, capturedGoal.IsOwnGoal);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
            _betServiceMock.Verify(betService => betService.ResolveBetsForMatch(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task AddGoal_WhenMatchFinished_AutoResolvesBets()
        {
            DateTime kickoff = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 5));
            MatchEntity match = new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };
            TeamStats stats = new TeamStats { Id = 100, MatchId = 1, TeamId = 10 };
            Player player = new Player { Id = 1, Name = "Neymar", Number = 10, TeamId = 10, PositionId = 1 };
            AddGoalDTO addGoalDto = new AddGoalDTO
            {
                MatchId = 1,
                TeamId = 10,
                PlayerId = 1,
                Minute = 42,
                IsOwnGoal = false
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _playerRepositoryMock.Setup(playerRepository => playerRepository.GetByIdAsync(1)).ReturnsAsync(player);
            SetupFind(_teamStatsRepositoryMock, new List<TeamStats> { stats });
            _goalRepositoryMock.Setup(goalRepository => goalRepository.Create(It.IsAny<Goal>()));
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);
            _betServiceMock.Setup(betService => betService.ResolveBetsForMatch(1))
                .ReturnsAsync(new ResolveBetsResultDTO { MatchId = 1, ResolvedCount = 2 });

            string? warning = await _goalService.AddGoal(addGoalDto);

            Assert.Null(warning);
            _betServiceMock.Verify(betService => betService.ResolveBetsForMatch(1), Times.Once);
            _knockoutServiceMock.Verify(knockoutService => knockoutService.TryAdvanceFromMatch(1), Times.Once);
        }

        [Fact]
        public async Task AddGoal_WhenMatchFinishedAndAdvanceThrows_ReturnsWarning()
        {
            DateTime kickoff = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 5));
            MatchEntity match = new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };
            TeamStats stats = new TeamStats { Id = 100, MatchId = 1, TeamId = 10 };
            Player player = new Player { Id = 1, Name = "Neymar", Number = 10, TeamId = 10, PositionId = 1 };
            AddGoalDTO addGoalDto = new AddGoalDTO
            {
                MatchId = 1,
                TeamId = 10,
                PlayerId = 1,
                Minute = 42,
                IsOwnGoal = false
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _playerRepositoryMock.Setup(playerRepository => playerRepository.GetByIdAsync(1)).ReturnsAsync(player);
            SetupFind(_teamStatsRepositoryMock, new List<TeamStats> { stats });
            _goalRepositoryMock.Setup(goalRepository => goalRepository.Create(It.IsAny<Goal>()));
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);
            _knockoutServiceMock
                .Setup(knockoutService => knockoutService.TryAdvanceFromMatch(1))
                .ThrowsAsync(new InvalidOperationException(
                    "Cannot advance into match 20: goals or cards are already recorded."));

            string? warning = await _goalService.AddGoal(addGoalDto);

            Assert.Equal("Cannot advance into match 20: goals or cards are already recorded.", warning);
            _goalRepositoryMock.Verify(goalRepository => goalRepository.Create(It.IsAny<Goal>()), Times.Once);
        }

        [Fact]
        public async Task AddGoal_WhenTeamsTbd_ThrowsInvalidOperationException()
        {
            DateTime kickoff = DateTime.UtcNow.AddDays(1);
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = kickoff,
                StadiumId = 5,
                TeamOneId = null,
                TeamTwoId = null,
                Stage = MatchStage.QuarterFinal
            };
            AddGoalDTO addGoalDto = new AddGoalDTO
            {
                MatchId = 1,
                TeamId = 10,
                PlayerId = 1,
                Minute = 10,
                IsOwnGoal = false
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _goalService.AddGoal(addGoalDto));

            Assert.Contains("Cannot record goals until both teams are set", exception.Message);
            _goalRepositoryMock.Verify(goalRepository => goalRepository.Create(It.IsAny<Goal>()), Times.Never);
        }

        [Fact]
        public async Task AddGoal_WhenMatchFinishedAndResolveThrows_SwallowsInvalidOperationException()
        {
            DateTime kickoff = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 5));
            MatchEntity match = new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };
            TeamStats stats = new TeamStats { Id = 100, MatchId = 1, TeamId = 10 };
            Player player = new Player { Id = 1, Name = "Neymar", Number = 10, TeamId = 10, PositionId = 1 };
            AddGoalDTO addGoalDto = new AddGoalDTO
            {
                MatchId = 1,
                TeamId = 10,
                PlayerId = 1,
                Minute = 42,
                IsOwnGoal = false
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _playerRepositoryMock.Setup(playerRepository => playerRepository.GetByIdAsync(1)).ReturnsAsync(player);
            SetupFind(_teamStatsRepositoryMock, new List<TeamStats> { stats });
            _goalRepositoryMock.Setup(goalRepository => goalRepository.Create(It.IsAny<Goal>()));
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);
            _betServiceMock
                .Setup(betService => betService.ResolveBetsForMatch(1))
                .ThrowsAsync(new InvalidOperationException("Incomplete match stats."));

            string? warning = await _goalService.AddGoal(addGoalDto);

            Assert.Null(warning);
            _betServiceMock.Verify(betService => betService.ResolveBetsForMatch(1), Times.Once);
            _knockoutServiceMock.Verify(knockoutService => knockoutService.TryAdvanceFromMatch(1), Times.Once);
        }

        [Fact]
        public async Task AddGoal_WhenTeamNotInMatch_ThrowsInvalidOperationException()
        {
            DateTime kickoff = new DateTime(2026, 6, 15, 18, 0, 0);
            MatchEntity match = new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };
            AddGoalDTO addGoalDto = new AddGoalDTO { MatchId = 1, TeamId = 99, PlayerId = 1, Minute = 10 };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _goalService.AddGoal(addGoalDto));

            Assert.Contains("not part of this match", exception.Message);
        }

        [Fact]
        public async Task AddGoal_WhenPlayerNotOnCreditedTeam_ThrowsInvalidOperationException()
        {
            DateTime kickoff = new DateTime(2026, 6, 15, 18, 0, 0);
            MatchEntity match = new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };
            Player player = new Player { Id = 1, Name = "Neymar", Number = 10, TeamId = 20, PositionId = 1 };
            AddGoalDTO addGoalDto = new AddGoalDTO { MatchId = 1, TeamId = 10, PlayerId = 1, Minute = 10, IsOwnGoal = false };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _playerRepositoryMock.Setup(playerRepository => playerRepository.GetByIdAsync(1)).ReturnsAsync(player);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _goalService.AddGoal(addGoalDto));

            Assert.Contains("scorer must belong", exception.Message);
        }

        [Fact]
        public async Task AddGoal_WhenOwnGoal_CreditsOpponentPlayer()
        {
            DateTime kickoff = new DateTime(2026, 6, 15, 18, 0, 0);
            MatchEntity match = new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };
            TeamStats stats = new TeamStats { Id = 100, MatchId = 1, TeamId = 10 };
            Player opponentPlayer = new Player { Id = 5, Name = "Defender", Number = 4, TeamId = 20, PositionId = 1 };
            AddGoalDTO addGoalDto = new AddGoalDTO
            {
                MatchId = 1,
                TeamId = 10,
                PlayerId = 5,
                Minute = 55,
                IsOwnGoal = true
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _playerRepositoryMock.Setup(playerRepository => playerRepository.GetByIdAsync(5)).ReturnsAsync(opponentPlayer);
            SetupFind(_teamStatsRepositoryMock, new List<TeamStats> { stats });

            Goal? capturedGoal = null;
            _goalRepositoryMock.Setup(goalRepository => goalRepository.Create(It.IsAny<Goal>()))
                .Callback<Goal>(goal => capturedGoal = goal);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _goalService.AddGoal(addGoalDto);

            Assert.NotNull(capturedGoal);
            Assert.Equal(1, capturedGoal.IsOwnGoal);
        }

        [Fact]
        public async Task AddGoal_WhenOwnGoalPlayerNotOnOpponent_ThrowsInvalidOperationException()
        {
            DateTime kickoff = new DateTime(2026, 6, 15, 18, 0, 0);
            MatchEntity match = new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };
            Player sameTeamPlayer = new Player { Id = 1, Name = "Neymar", Number = 10, TeamId = 10, PositionId = 1 };
            AddGoalDTO addGoalDto = new AddGoalDTO
            {
                MatchId = 1,
                TeamId = 10,
                PlayerId = 1,
                Minute = 55,
                IsOwnGoal = true
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _playerRepositoryMock.Setup(playerRepository => playerRepository.GetByIdAsync(1)).ReturnsAsync(sameTeamPlayer);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _goalService.AddGoal(addGoalDto));

            Assert.Contains("own goal must be credited", exception.Message);
        }

        [Fact]
        public async Task DeleteGoal_DeletesAndSaves()
        {
            DateTime kickoff = DateTime.UtcNow.AddHours(-2);
            Goal goal = new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = kickoff.AddMinutes(30), IsOwnGoal = 0 };
            TeamStats stats = new TeamStats { Id = 100, MatchId = 1, TeamId = 10 };
            MatchEntity match = new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };

            _goalRepositoryMock.Setup(goalRepository => goalRepository.GetByIdAsync(1)).ReturnsAsync(goal);
            _teamStatsRepositoryMock.Setup(teamStatsRepository => teamStatsRepository.GetByIdAsync(100)).ReturnsAsync(stats);
            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _goalService.DeleteGoal(1);

            _goalRepositoryMock.Verify(goalRepository => goalRepository.Delete(goal), Times.Once);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteGoal_WhenMatchFinished_ReResolvesBets()
        {
            DateTime kickoff = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 5));
            Goal goal = new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = kickoff.AddMinutes(30), IsOwnGoal = 0 };
            TeamStats stats = new TeamStats { Id = 100, MatchId = 1, TeamId = 10 };
            MatchEntity match = new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };

            _goalRepositoryMock.Setup(goalRepository => goalRepository.GetByIdAsync(1)).ReturnsAsync(goal);
            _teamStatsRepositoryMock.Setup(teamStatsRepository => teamStatsRepository.GetByIdAsync(100)).ReturnsAsync(stats);
            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);
            _betServiceMock.Setup(betService => betService.ResolveBetsForMatch(1))
                .ReturnsAsync(new ResolveBetsResultDTO { MatchId = 1, ResolvedCount = 3 });

            string? warning = await _goalService.DeleteGoal(1);

            Assert.Null(warning);
            _betServiceMock.Verify(betService => betService.ResolveBetsForMatch(1), Times.Once);
            _knockoutServiceMock.Verify(knockoutService => knockoutService.TryAdvanceFromMatch(1), Times.Once);
        }

        [Fact]
        public async Task DeleteGoal_WhenMatchFinishedAndAdvanceThrows_ReturnsWarning()
        {
            DateTime kickoff = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 5));
            Goal goal = new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = kickoff.AddMinutes(30), IsOwnGoal = 0 };
            TeamStats stats = new TeamStats { Id = 100, MatchId = 1, TeamId = 10 };
            MatchEntity match = new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };

            _goalRepositoryMock.Setup(goalRepository => goalRepository.GetByIdAsync(1)).ReturnsAsync(goal);
            _teamStatsRepositoryMock.Setup(teamStatsRepository => teamStatsRepository.GetByIdAsync(100)).ReturnsAsync(stats);
            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);
            _knockoutServiceMock
                .Setup(knockoutService => knockoutService.TryAdvanceFromMatch(1))
                .ThrowsAsync(new InvalidOperationException(
                    "Cannot advance into match 20: goals or cards are already recorded."));

            string? warning = await _goalService.DeleteGoal(1);

            Assert.Equal("Cannot advance into match 20: goals or cards are already recorded.", warning);
            _goalRepositoryMock.Verify(goalRepository => goalRepository.Delete(goal), Times.Once);
        }

        [Fact]
        public async Task DeleteGoal_WhenMatchInProgress_DoesNotReResolveBets()
        {
            DateTime kickoff = DateTime.UtcNow.AddMinutes(-30);
            Goal goal = new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = kickoff.AddMinutes(10), IsOwnGoal = 0 };
            TeamStats stats = new TeamStats { Id = 100, MatchId = 1, TeamId = 10 };
            MatchEntity match = new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };

            _goalRepositoryMock.Setup(goalRepository => goalRepository.GetByIdAsync(1)).ReturnsAsync(goal);
            _teamStatsRepositoryMock.Setup(teamStatsRepository => teamStatsRepository.GetByIdAsync(100)).ReturnsAsync(stats);
            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _goalService.DeleteGoal(1);

            _betServiceMock.Verify(betService => betService.ResolveBetsForMatch(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteGoal_WhenMatchFinishedAndResolveThrows_SwallowsInvalidOperationException()
        {
            DateTime kickoff = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 5));
            Goal goal = new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = kickoff.AddMinutes(30), IsOwnGoal = 0 };
            TeamStats stats = new TeamStats { Id = 100, MatchId = 1, TeamId = 10 };
            MatchEntity match = new MatchEntity { Id = 1, Date = kickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 };

            _goalRepositoryMock.Setup(goalRepository => goalRepository.GetByIdAsync(1)).ReturnsAsync(goal);
            _teamStatsRepositoryMock.Setup(teamStatsRepository => teamStatsRepository.GetByIdAsync(100)).ReturnsAsync(stats);
            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);
            _betServiceMock
                .Setup(betService => betService.ResolveBetsForMatch(1))
                .ThrowsAsync(new InvalidOperationException("Incomplete match stats."));

            await _goalService.DeleteGoal(1);

            _goalRepositoryMock.Verify(goalRepository => goalRepository.Delete(goal), Times.Once);
            _betServiceMock.Verify(betService => betService.ResolveBetsForMatch(1), Times.Once);
        }

        private static void SetupFind<T>(Mock<IRepository<T>> repositoryMock, List<T> entities) where T : class
        {
            repositoryMock
                .Setup(repository => repository.Find(It.IsAny<Expression<Func<T, bool>>>()))
                .Returns((Expression<Func<T, bool>> predicate) => entities.AsQueryable().Where(predicate));
        }
    }
}
