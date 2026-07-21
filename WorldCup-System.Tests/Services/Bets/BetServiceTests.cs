using System.Linq.Expressions;
using Core.DTOs.Bets;
using Core.Services.Bets;
using Data.Entities;
using Data.Repos;
using Moq;
using MatchEntity = Data.Entities.Match;

namespace WorldCup_System.Tests.Services.Bets
{
    public class BetServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<MatchEntity>> _matchRepositoryMock;
        private readonly Mock<IRepository<Bet>> _betRepositoryMock;
        private readonly Mock<IRepository<BetResult>> _betResultRepositoryMock;
        private readonly Mock<IRepository<Team>> _teamRepositoryMock;
        private readonly Mock<IRepository<Country>> _countryRepositoryMock;
        private readonly Mock<IRepository<TeamStats>> _teamStatsRepositoryMock;
        private readonly Mock<IRepository<Goal>> _goalRepositoryMock;
        private readonly Mock<IRepository<Group>> _groupRepositoryMock;
        private readonly Mock<IRepository<User>> _userRepositoryMock;
        private readonly BetService _betService;

        public BetServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _matchRepositoryMock = new Mock<IRepository<MatchEntity>>();
            _betRepositoryMock = new Mock<IRepository<Bet>>();
            _betResultRepositoryMock = new Mock<IRepository<BetResult>>();
            _teamRepositoryMock = new Mock<IRepository<Team>>();
            _countryRepositoryMock = new Mock<IRepository<Country>>();
            _teamStatsRepositoryMock = new Mock<IRepository<TeamStats>>();
            _goalRepositoryMock = new Mock<IRepository<Goal>>();
            _groupRepositoryMock = new Mock<IRepository<Group>>();
            _userRepositoryMock = new Mock<IRepository<User>>();

            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Match).Returns(_matchRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Bet).Returns(_betRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.BetResult).Returns(_betResultRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Team).Returns(_teamRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Country).Returns(_countryRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.TeamStats).Returns(_teamStatsRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Goal).Returns(_goalRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Group).Returns(_groupRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.User).Returns(_userRepositoryMock.Object);
            _userRepositoryMock.Setup(userRepository => userRepository.GetAllAsync()).Returns(new List<User>().AsQueryable());
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetAllAsync()).Returns(new List<Team>().AsQueryable());
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(new List<Country>().AsQueryable());

            _betService = new BetService(_repositoryManagerMock.Object);
        }

        [Fact]
        public async Task PlaceBet_WhenMatchNotStarted_CreatesBet()
        {
            DateTime futureKickoff = DateTime.UtcNow.AddDays(1);
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = futureKickoff,
                StadiumId = 1,
                TeamOneId = 10,
                TeamTwoId = 20
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns(new List<Bet>().AsQueryable());

            PlaceBetDTO placeBetDto = new PlaceBetDTO { MatchId = 1, IsDraw = false, TeamId = 10 };

            await _betService.PlaceBet(5, placeBetDto);

            _betRepositoryMock.Verify(betRepository => betRepository.Create(It.Is<Bet>(bet =>
                bet.UserId == 5 && bet.MatchId == 1 && bet.TeamId == 10 && !bet.IsDraw)), Times.Once);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task PlaceBet_WhenTeamsTbd_ThrowsInvalidOperationException()
        {
            DateTime futureKickoff = DateTime.UtcNow.AddDays(1);
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = futureKickoff,
                StadiumId = 1,
                TeamOneId = null,
                TeamTwoId = null,
                Stage = MatchStage.SemiFinal
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);

            PlaceBetDTO placeBetDto = new PlaceBetDTO { MatchId = 1, IsDraw = false, TeamId = 10 };

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _betService.PlaceBet(5, placeBetDto));

            Assert.Contains("Cannot place bets until both teams are set", exception.Message);
            _betRepositoryMock.Verify(betRepository => betRepository.Create(It.IsAny<Bet>()), Times.Never);
        }

        [Fact]
        public async Task PlaceBet_WhenMatchAlreadyStarted_ThrowsInvalidOperationException()
        {
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = DateTime.UtcNow.AddHours(-1),
                StadiumId = 1,
                TeamOneId = 10,
                TeamTwoId = 20
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);

            PlaceBetDTO placeBetDto = new PlaceBetDTO { MatchId = 1, IsDraw = false, TeamId = 10 };

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _betService.PlaceBet(5, placeBetDto));

            Assert.Contains("after the match has started", exception.Message);
        }

        [Fact]
        public async Task PlaceBet_WhenUnspecifiedKickoffInFuture_CreatesBet()
        {
            DateTime futureKickoffUnspecified = DateTime.SpecifyKind(
                DateTime.UtcNow.AddHours(2),
                DateTimeKind.Unspecified);
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = futureKickoffUnspecified,
                StadiumId = 1,
                TeamOneId = 10,
                TeamTwoId = 20
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns(new List<Bet>().AsQueryable());

            PlaceBetDTO placeBetDto = new PlaceBetDTO { MatchId = 1, IsDraw = false, TeamId = 10 };

            await _betService.PlaceBet(5, placeBetDto);

            _betRepositoryMock.Verify(betRepository => betRepository.Create(It.Is<Bet>(bet =>
                bet.UserId == 5 && bet.MatchId == 1 && bet.TeamId == 10 && !bet.IsDraw)), Times.Once);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task PlaceBet_WhenUnspecifiedKickoffInPast_ThrowsInvalidOperationException()
        {
            DateTime pastKickoffUnspecified = DateTime.SpecifyKind(
                DateTime.UtcNow.AddHours(-1),
                DateTimeKind.Unspecified);
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = pastKickoffUnspecified,
                StadiumId = 1,
                TeamOneId = 10,
                TeamTwoId = 20
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);

            PlaceBetDTO placeBetDto = new PlaceBetDTO { MatchId = 1, IsDraw = false, TeamId = 10 };

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _betService.PlaceBet(5, placeBetDto));

            Assert.Contains("after the match has started", exception.Message);
            _betRepositoryMock.Verify(betRepository => betRepository.Create(It.IsAny<Bet>()), Times.Never);
        }

        [Fact]
        public async Task PlaceBet_WhenTeamNotInMatch_ThrowsInvalidOperationException()
        {
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = DateTime.UtcNow.AddDays(1),
                StadiumId = 1,
                TeamOneId = 10,
                TeamTwoId = 20
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);

            PlaceBetDTO placeBetDto = new PlaceBetDTO { MatchId = 1, IsDraw = false, TeamId = 99 };

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _betService.PlaceBet(5, placeBetDto));

            Assert.Contains("teams playing", exception.Message);
        }

        [Fact]
        public async Task PlaceBet_WhenUserAlreadyBet_ThrowsInvalidOperationException()
        {
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = DateTime.UtcNow.AddDays(1),
                StadiumId = 1,
                TeamOneId = 10,
                TeamTwoId = 20
            };
            List<Bet> existingBets = new List<Bet>
            {
                new Bet { Id = 1, UserId = 5, MatchId = 1, TeamId = 10 }
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns((Expression<Func<Bet, bool>> predicate) => existingBets.AsQueryable().Where(predicate));

            PlaceBetDTO placeBetDto = new PlaceBetDTO { MatchId = 1, IsDraw = false, TeamId = 20 };

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _betService.PlaceBet(5, placeBetDto));

            Assert.Contains("already placed", exception.Message);
        }

        [Fact]
        public async Task ResolveBetsForMatch_WhenTeamOneWins_AwardsThreePointsToCorrectBets()
        {
            DateTime pastKickoff = DateTime.UtcNow.AddHours(-2);
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = pastKickoff,
                StadiumId = 1,
                TeamOneId = 10,
                TeamTwoId = 20
            };
            List<Bet> bets = new List<Bet>
            {
                new Bet { Id = 1, UserId = 1, MatchId = 1, TeamId = 10, IsDraw = false },
                new Bet { Id = 2, UserId = 2, MatchId = 1, TeamId = 20, IsDraw = false }
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = pastKickoff.AddMinutes(30), IsOwnGoal = 0 },
                new Goal { Id = 2, TeamStatsId = 100, PlayerId = 2, TimeScored = pastKickoff.AddMinutes(60), IsOwnGoal = 0 }
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns((Expression<Func<Bet, bool>> predicate) => bets.AsQueryable().Where(predicate));
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.Find(It.IsAny<Expression<Func<BetResult, bool>>>()))
                .Returns(new List<BetResult>().AsQueryable());
            _teamStatsRepositoryMock.Setup(teamStatsRepository => teamStatsRepository.Find(It.IsAny<Expression<Func<TeamStats, bool>>>()))
                .Returns((Expression<Func<TeamStats, bool>> predicate) => stats.AsQueryable().Where(predicate));
            _goalRepositoryMock.Setup(goalRepository => goalRepository.Find(It.IsAny<Expression<Func<Goal, bool>>>()))
                .Returns((Expression<Func<Goal, bool>> predicate) => goals.AsQueryable().Where(predicate));

            int resolvedCount = (await _betService.ResolveBetsForMatch(1)).ResolvedCount;

            Assert.Equal(2, resolvedCount);
            _betResultRepositoryMock.Verify(betResultRepository => betResultRepository.Create(It.Is<BetResult>(result =>
                result.BetId == 1 && result.Point == BetScoringRules.CorrectOutcomePoints)), Times.Once);
            _betResultRepositoryMock.Verify(betResultRepository => betResultRepository.Create(It.Is<BetResult>(result =>
                result.BetId == 2 && result.Point == BetScoringRules.IncorrectPredictionPoints)), Times.Once);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task ResolveBetsForMatch_WhenMatchNotFinished_ThrowsInvalidOperationException()
        {
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = DateTime.UtcNow.AddMinutes(-30),
                StadiumId = 1,
                TeamOneId = 10,
                TeamTwoId = 20
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _betService.ResolveBetsForMatch(1));

            Assert.Contains("90 minutes", exception.Message);
        }

        [Fact]
        public async Task ResolveBetsForMatch_WhenMatchNotStarted_ThrowsInvalidOperationException()
        {
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = DateTime.UtcNow.AddDays(1),
                StadiumId = 1,
                TeamOneId = 10,
                TeamTwoId = 20
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _betService.ResolveBetsForMatch(1));

            Assert.Contains("90 minutes", exception.Message);
        }

        [Fact]
        public void GetActiveBets_ReturnsOnlyUnresolvedBets()
        {
            List<Bet> bets = new List<Bet>
            {
                new Bet { Id = 1, UserId = 5, MatchId = 1, TeamId = 10, IsDraw = false },
                new Bet { Id = 2, UserId = 5, MatchId = 2, TeamId = 20, IsDraw = false }
            };
            List<BetResult> results = new List<BetResult>
            {
                new BetResult { Id = 1, BetId = 1, Point = 3 }
            };
            List<MatchEntity> matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, Date = DateTime.UtcNow.AddDays(1), StadiumId = 1, TeamOneId = 10, TeamTwoId = 20 },
                new MatchEntity { Id = 2, Date = DateTime.UtcNow.AddDays(2), StadiumId = 1, TeamOneId = 20, TeamTwoId = 30 }
            };

            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns((Expression<Func<Bet, bool>> predicate) => bets.AsQueryable().Where(predicate));
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.Find(It.IsAny<Expression<Func<BetResult, bool>>>()))
                .Returns((Expression<Func<BetResult, bool>> predicate) => results.AsQueryable().Where(predicate));
            _matchRepositoryMock.Setup(matchRepository => matchRepository.Find(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .Returns((Expression<Func<MatchEntity, bool>> predicate) => matches.AsQueryable().Where(predicate));
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetAllAsync()).Returns(new List<Team>().AsQueryable());
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(new List<Country>().AsQueryable());

            List<BetDTO> activeBets = _betService.GetActiveBets(5);

            Assert.Single(activeBets);
            Assert.Equal(2, activeBets[0].Id);
            Assert.True(activeBets[0].IsActive);
        }

        [Fact]
        public async Task PlaceBet_WhenPredictingDraw_CreatesDrawBetWithoutTeam()
        {
            DateTime futureKickoff = DateTime.UtcNow.AddDays(1);
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = futureKickoff,
                StadiumId = 1,
                TeamOneId = 10,
                TeamTwoId = 20
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns(new List<Bet>().AsQueryable());

            PlaceBetDTO placeBetDto = new PlaceBetDTO { MatchId = 1, IsDraw = true };

            await _betService.PlaceBet(5, placeBetDto);

            _betRepositoryMock.Verify(betRepository => betRepository.Create(It.Is<Bet>(bet =>
                bet.UserId == 5 && bet.MatchId == 1 && bet.IsDraw && bet.TeamId == null)), Times.Once);
        }

        [Fact]
        public async Task PlaceBet_WhenDrawWithTeamId_ThrowsInvalidOperationException()
        {
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = DateTime.UtcNow.AddDays(1),
                StadiumId = 1,
                TeamOneId = 10,
                TeamTwoId = 20
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);

            PlaceBetDTO placeBetDto = new PlaceBetDTO { MatchId = 1, IsDraw = true, TeamId = 10 };

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _betService.PlaceBet(5, placeBetDto));

            Assert.Contains("predicting a draw", exception.Message);
        }

        [Fact]
        public async Task ResolveBetsForMatch_WhenMatchIsDraw_AwardsPointsToDrawBets()
        {
            DateTime pastKickoff = DateTime.UtcNow.AddHours(-2);
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = pastKickoff,
                StadiumId = 1,
                TeamOneId = 10,
                TeamTwoId = 20
            };
            List<Bet> bets = new List<Bet>
            {
                new Bet { Id = 1, UserId = 1, MatchId = 1, IsDraw = true, TeamId = null },
                new Bet { Id = 2, UserId = 2, MatchId = 1, IsDraw = false, TeamId = 10 }
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = pastKickoff.AddMinutes(30), IsOwnGoal = 0 },
                new Goal { Id = 2, TeamStatsId = 101, PlayerId = 2, TimeScored = pastKickoff.AddMinutes(60), IsOwnGoal = 0 }
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns((Expression<Func<Bet, bool>> predicate) => bets.AsQueryable().Where(predicate));
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.Find(It.IsAny<Expression<Func<BetResult, bool>>>()))
                .Returns(new List<BetResult>().AsQueryable());
            _teamStatsRepositoryMock.Setup(teamStatsRepository => teamStatsRepository.Find(It.IsAny<Expression<Func<TeamStats, bool>>>()))
                .Returns((Expression<Func<TeamStats, bool>> predicate) => stats.AsQueryable().Where(predicate));
            _goalRepositoryMock.Setup(goalRepository => goalRepository.Find(It.IsAny<Expression<Func<Goal, bool>>>()))
                .Returns((Expression<Func<Goal, bool>> predicate) => goals.AsQueryable().Where(predicate));

            int resolvedCount = (await _betService.ResolveBetsForMatch(1)).ResolvedCount;

            Assert.Equal(2, resolvedCount);
            _betResultRepositoryMock.Verify(betResultRepository => betResultRepository.Create(It.Is<BetResult>(result =>
                result.BetId == 1 && result.Point == BetScoringRules.CorrectOutcomePoints)), Times.Once);
            _betResultRepositoryMock.Verify(betResultRepository => betResultRepository.Create(It.Is<BetResult>(result =>
                result.BetId == 2 && result.Point == BetScoringRules.IncorrectPredictionPoints)), Times.Once);
        }

        [Fact]
        public async Task ResolveBetsForMatch_WhenOnlyExtraTimeGoal_ResolvesAsDraw()
        {
            // 0-0 after regulation; ET winner goal at minute 106 must not decide the bet outcome.
            DateTime pastKickoff = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 30));
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = pastKickoff,
                StadiumId = 1,
                TeamOneId = 10,
                TeamTwoId = 20
            };
            List<Bet> bets = new List<Bet>
            {
                new Bet { Id = 1, UserId = 1, MatchId = 1, IsDraw = true, TeamId = null },
                new Bet { Id = 2, UserId = 2, MatchId = 1, IsDraw = false, TeamId = 10 }
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal
                {
                    Id = 1,
                    TeamStatsId = 100,
                    PlayerId = 1,
                    TimeScored = pastKickoff.AddMinutes(106),
                    IsOwnGoal = 0
                }
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns((Expression<Func<Bet, bool>> predicate) => bets.AsQueryable().Where(predicate));
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.Find(It.IsAny<Expression<Func<BetResult, bool>>>()))
                .Returns(new List<BetResult>().AsQueryable());
            _teamStatsRepositoryMock.Setup(teamStatsRepository => teamStatsRepository.Find(It.IsAny<Expression<Func<TeamStats, bool>>>()))
                .Returns((Expression<Func<TeamStats, bool>> predicate) => stats.AsQueryable().Where(predicate));
            _goalRepositoryMock.Setup(goalRepository => goalRepository.Find(It.IsAny<Expression<Func<Goal, bool>>>()))
                .Returns((Expression<Func<Goal, bool>> predicate) => goals.AsQueryable().Where(predicate));

            int resolvedCount = (await _betService.ResolveBetsForMatch(1)).ResolvedCount;

            Assert.Equal(2, resolvedCount);
            _betResultRepositoryMock.Verify(betResultRepository => betResultRepository.Create(It.Is<BetResult>(result =>
                result.BetId == 1 && result.Point == BetScoringRules.CorrectOutcomePoints)), Times.Once);
            _betResultRepositoryMock.Verify(betResultRepository => betResultRepository.Create(It.Is<BetResult>(result =>
                result.BetId == 2 && result.Point == BetScoringRules.IncorrectPredictionPoints)), Times.Once);
        }

        [Fact]
        public void GetScoringRules_ReturnsDocumentedRules()
        {
            BettingRulesResponseDTO rules = _betService.GetScoringRules();

            Assert.Equal(BetScoringRules.Summary, rules.Summary);
            Assert.Equal(6, rules.Rules.Count);
            Assert.Contains(rules.Rules, rule => rule.Rule.Contains("3 points"));
        }

        [Fact]
        public void GetMyBetForMatch_WhenBetExists_ReturnsBetDto()
        {
            List<Bet> bets = new List<Bet>
            {
                new Bet { Id = 7, UserId = 5, MatchId = 1, IsDraw = false, TeamId = 10 }
            };
            List<MatchEntity> matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, Date = DateTime.UtcNow.AddDays(1), StadiumId = 1, TeamOneId = 10, TeamTwoId = 20 }
            };

            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns((Expression<Func<Bet, bool>> predicate) => bets.AsQueryable().Where(predicate));
            _matchRepositoryMock.Setup(matchRepository => matchRepository.Find(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .Returns((Expression<Func<MatchEntity, bool>> predicate) => matches.AsQueryable().Where(predicate));
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetAllAsync()).Returns(new List<Team>().AsQueryable());
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(new List<Country>().AsQueryable());
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.Find(It.IsAny<Expression<Func<BetResult, bool>>>()))
                .Returns(new List<BetResult>().AsQueryable());

            BetDTO? result = _betService.GetMyBetForMatch(5, 1);

            Assert.NotNull(result);
            Assert.Equal(7, result.Id);
            Assert.Equal(1, result.MatchId);
            Assert.True(result.IsActive);
        }

        [Fact]
        public void GetMyBetForMatch_WhenNoBet_ReturnsNull()
        {
            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns(new List<Bet>().AsQueryable());

            BetDTO? result = _betService.GetMyBetForMatch(5, 99);

            Assert.Null(result);
        }

        [Fact]
        public void GetMyBetsForWorldCup_WhenUserHasBets_ReturnsBetsForTournamentMatches()
        {
            List<Group> groups = new List<Group>
            {
                new Group { Id = 1, WorldCupId = 2026, Name = "A" }
            };
            Group group = groups[0];
            List<Team> teams = new List<Team>
            {
                new Team { Id = 10, GroupId = 1, CountryId = 1, Country = new Country { Id = 1, Name = "Brazil" }, Group = group, Coach = new List<Coach>() },
                new Team { Id = 20, GroupId = 1, CountryId = 2, Country = new Country { Id = 2, Name = "France" }, Group = group, Coach = new List<Coach>() }
            };
            List<MatchEntity> matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, Date = DateTime.UtcNow.AddDays(1), StadiumId = 1, TeamOneId = 10, TeamTwoId = 20 },
                new MatchEntity { Id = 2, Date = DateTime.UtcNow.AddDays(2), StadiumId = 1, TeamOneId = 10, TeamTwoId = 20 }
            };
            List<Bet> bets = new List<Bet>
            {
                new Bet { Id = 7, UserId = 5, MatchId = 1, IsDraw = true },
                new Bet { Id = 8, UserId = 5, MatchId = 2, IsDraw = false, TeamId = 10 },
                new Bet { Id = 9, UserId = 99, MatchId = 1, IsDraw = false, TeamId = 20 }
            };

            _groupRepositoryMock.Setup(groupRepository => groupRepository.Find(It.IsAny<Expression<Func<Group, bool>>>()))
                .Returns((Expression<Func<Group, bool>> predicate) => groups.AsQueryable().Where(predicate));
            _teamRepositoryMock.Setup(teamRepository => teamRepository.Find(It.IsAny<Expression<Func<Team, bool>>>()))
                .Returns((Expression<Func<Team, bool>> predicate) => teams.AsQueryable().Where(predicate));
            _matchRepositoryMock.Setup(matchRepository => matchRepository.Find(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .Returns((Expression<Func<MatchEntity, bool>> predicate) => matches.AsQueryable().Where(predicate));
            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns((Expression<Func<Bet, bool>> predicate) => bets.AsQueryable().Where(predicate));
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetAllAsync()).Returns(teams.AsQueryable());
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(new List<Country>().AsQueryable());
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.Find(It.IsAny<Expression<Func<BetResult, bool>>>()))
                .Returns(new List<BetResult>().AsQueryable());

            List<BetDTO> result = _betService.GetMyBetsForWorldCup(5, 2026);

            Assert.Equal(2, result.Count);
            Assert.All(result, betDto => Assert.Equal(5, betDto.UserId));
            Assert.Contains(result, betDto => betDto.MatchId == 1);
            Assert.Contains(result, betDto => betDto.MatchId == 2);
        }

        [Fact]
        public void GetMyBetsForWorldCup_WhenNoTournamentMatches_ReturnsEmptyList()
        {
            _groupRepositoryMock.Setup(groupRepository => groupRepository.Find(It.IsAny<Expression<Func<Group, bool>>>()))
                .Returns(new List<Group>().AsQueryable());

            List<BetDTO> result = _betService.GetMyBetsForWorldCup(5, 2026);

            Assert.Empty(result);
        }

        [Fact]
        public async Task ResolveBetsForMatch_ReturnsUserBreakdownWithPoints()
        {
            DateTime pastKickoff = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 10));
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = pastKickoff,
                StadiumId = 1,
                TeamOneId = 10,
                TeamTwoId = 20
            };
            List<Bet> bets = new List<Bet>
            {
                new Bet { Id = 1, UserId = 1, MatchId = 1, TeamId = 10, IsDraw = false },
                new Bet { Id = 2, UserId = 2, MatchId = 1, TeamId = 20, IsDraw = false }
            };
            List<User> users = new List<User>
            {
                new User { Id = 1, Name = "Alice" },
                new User { Id = 2, Name = "Bob" }
            };
            List<Country> countries = new List<Country>
            {
                new Country { Id = 1, Name = "Brazil" },
                new Country { Id = 2, Name = "France" }
            };
            Group group = new Group { Id = 1, Name = "A" };
            List<Team> teams = new List<Team>
            {
                new Team { Id = 10, CountryId = 1, GroupId = 1, Country = countries[0], Group = group, Coach = new List<Coach>() },
                new Team { Id = 20, CountryId = 2, GroupId = 1, Country = countries[1], Group = group, Coach = new List<Coach>() }
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = pastKickoff.AddMinutes(30), IsOwnGoal = 0 },
                new Goal { Id = 2, TeamStatsId = 100, PlayerId = 2, TimeScored = pastKickoff.AddMinutes(60), IsOwnGoal = 0 }
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns((Expression<Func<Bet, bool>> predicate) => bets.AsQueryable().Where(predicate));
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.Find(It.IsAny<Expression<Func<BetResult, bool>>>()))
                .Returns(new List<BetResult>().AsQueryable());
            _teamStatsRepositoryMock.Setup(teamStatsRepository => teamStatsRepository.Find(It.IsAny<Expression<Func<TeamStats, bool>>>()))
                .Returns((Expression<Func<TeamStats, bool>> predicate) => stats.AsQueryable().Where(predicate));
            _goalRepositoryMock.Setup(goalRepository => goalRepository.Find(It.IsAny<Expression<Func<Goal, bool>>>()))
                .Returns((Expression<Func<Goal, bool>> predicate) => goals.AsQueryable().Where(predicate));
            _userRepositoryMock.Setup(userRepository => userRepository.GetAllAsync()).Returns(users.AsQueryable());
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetAllAsync()).Returns(teams.AsQueryable());
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(countries.AsQueryable());

            ResolveBetsResultDTO result = await _betService.ResolveBetsForMatch(1);

            Assert.Equal(2, result.ResolvedCount);
            Assert.Equal(2, result.UserBreakdown.Count);

            BetResolveUserDTO winnerBreakdown = result.UserBreakdown.Single(entry => entry.UserId == 1);
            Assert.Equal("Alice", winnerBreakdown.UserName);
            Assert.Equal("Brazil", winnerBreakdown.PredictedOutcome);
            Assert.Equal(BetScoringRules.CorrectOutcomePoints, winnerBreakdown.PointsAwarded);
            Assert.Null(winnerBreakdown.PreviousPoints);

            BetResolveUserDTO loserBreakdown = result.UserBreakdown.Single(entry => entry.UserId == 2);
            Assert.Equal("Bob", loserBreakdown.UserName);
            Assert.Equal("France", loserBreakdown.PredictedOutcome);
            Assert.Equal(BetScoringRules.IncorrectPredictionPoints, loserBreakdown.PointsAwarded);
            Assert.Null(loserBreakdown.PreviousPoints);
        }

        [Fact]
        public async Task ResolveBetsForWorldCup_ProcessesFinishedMatchesOnly()
        {
            DateTime finishedKickoff = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 10));
            DateTime liveKickoff = DateTime.UtcNow.AddMinutes(-30);
            DateTime scheduledKickoff = DateTime.UtcNow.AddDays(1);
            List<Group> groups = new List<Group> { new Group { Id = 1, WorldCupId = 2026, Name = "A" } };
            List<Country> countries = new List<Country>
            {
                new Country { Id = 1, Name = "Brazil" },
                new Country { Id = 2, Name = "France" }
            };
            List<Team> teams = new List<Team>
            {
                new Team { Id = 10, GroupId = 1, CountryId = 1, Country = countries[0], Group = groups[0], Coach = new List<Coach>() },
                new Team { Id = 20, GroupId = 1, CountryId = 2, Country = countries[1], Group = groups[0], Coach = new List<Coach>() }
            };
            List<MatchEntity> matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, Date = finishedKickoff, StadiumId = 1, TeamOneId = 10, TeamTwoId = 20 },
                new MatchEntity { Id = 2, Date = liveKickoff, StadiumId = 1, TeamOneId = 10, TeamTwoId = 20 },
                new MatchEntity { Id = 3, Date = scheduledKickoff, StadiumId = 1, TeamOneId = 10, TeamTwoId = 20 }
            };
            List<Bet> bets = new List<Bet>
            {
                new Bet { Id = 1, UserId = 1, MatchId = 1, TeamId = 10, IsDraw = false },
                new Bet { Id = 2, UserId = 2, MatchId = 1, TeamId = 20, IsDraw = false }
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = finishedKickoff.AddMinutes(45), IsOwnGoal = 0 }
            };

            _groupRepositoryMock.Setup(groupRepository => groupRepository.Find(It.IsAny<Expression<Func<Group, bool>>>()))
                .Returns((Expression<Func<Group, bool>> predicate) => groups.AsQueryable().Where(predicate));
            _teamRepositoryMock.Setup(teamRepository => teamRepository.Find(It.IsAny<Expression<Func<Team, bool>>>()))
                .Returns((Expression<Func<Team, bool>> predicate) => teams.AsQueryable().Where(predicate));
            _matchRepositoryMock.Setup(matchRepository => matchRepository.Find(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .Returns((Expression<Func<MatchEntity, bool>> predicate) => matches.AsQueryable().Where(predicate));
            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(matches[0]);
            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(2)).ReturnsAsync(matches[1]);
            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(3)).ReturnsAsync(matches[2]);
            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns((Expression<Func<Bet, bool>> predicate) => bets.AsQueryable().Where(predicate));
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.Find(It.IsAny<Expression<Func<BetResult, bool>>>()))
                .Returns(new List<BetResult>().AsQueryable());
            _teamStatsRepositoryMock.Setup(teamStatsRepository => teamStatsRepository.Find(It.IsAny<Expression<Func<TeamStats, bool>>>()))
                .Returns((Expression<Func<TeamStats, bool>> predicate) => stats.AsQueryable().Where(predicate));
            _goalRepositoryMock.Setup(goalRepository => goalRepository.Find(It.IsAny<Expression<Func<Goal, bool>>>()))
                .Returns((Expression<Func<Goal, bool>> predicate) => goals.AsQueryable().Where(predicate));
            _userRepositoryMock.Setup(userRepository => userRepository.GetAllAsync()).Returns(new List<User>().AsQueryable());
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetAllAsync()).Returns(teams.AsQueryable());
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(new List<Country>().AsQueryable());

            ResolveBetsWorldCupResultDTO result = await _betService.ResolveBetsForWorldCup(2026);

            Assert.Equal(2026, result.WorldCupId);
            Assert.Equal(1, result.MatchesProcessed);
            Assert.Equal(2, result.TotalResolvedCount);
            Assert.Single(result.MatchResults);
            Assert.Equal(1, result.MatchResults[0].MatchId);
            Assert.Equal(2, result.MatchResults[0].ResolvedCount);
            _betResultRepositoryMock.Verify(betResultRepository => betResultRepository.Create(It.IsAny<BetResult>()), Times.Exactly(2));
        }

        [Fact]
        public async Task TryAutoResolveFinishedMatch_SkipsInProgressMatches()
        {
            DateTime liveKickoff = DateTime.UtcNow.AddMinutes(-30);
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = liveKickoff,
                StadiumId = 1,
                TeamOneId = 10,
                TeamTwoId = 20
            };
            List<Bet> bets = new List<Bet>
            {
                new Bet { Id = 1, UserId = 1, MatchId = 1, TeamId = 10, IsDraw = false }
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns((Expression<Func<Bet, bool>> predicate) => bets.AsQueryable().Where(predicate));

            await _betService.TryAutoResolveFinishedMatch(1);

            _betResultRepositoryMock.Verify(betResultRepository => betResultRepository.Create(It.IsAny<BetResult>()), Times.Never);
            _betResultRepositoryMock.Verify(betResultRepository => betResultRepository.Update(It.IsAny<BetResult>()), Times.Never);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Never);
        }

        [Fact]
        public async Task TryAutoResolveFinishedMatch_WhenFinished_ResolvesUnresolvedBets()
        {
            DateTime pastKickoff = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 10));
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = pastKickoff,
                StadiumId = 1,
                TeamOneId = 10,
                TeamTwoId = 20
            };
            List<Bet> bets = new List<Bet>
            {
                new Bet { Id = 1, UserId = 1, MatchId = 1, TeamId = 10, IsDraw = false }
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = pastKickoff.AddMinutes(40), IsOwnGoal = 0 }
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns((Expression<Func<Bet, bool>> predicate) => bets.AsQueryable().Where(predicate));
            _betResultRepositoryMock.Setup(betResultRepository => betResultRepository.Find(It.IsAny<Expression<Func<BetResult, bool>>>()))
                .Returns(new List<BetResult>().AsQueryable());
            _teamStatsRepositoryMock.Setup(teamStatsRepository => teamStatsRepository.Find(It.IsAny<Expression<Func<TeamStats, bool>>>()))
                .Returns((Expression<Func<TeamStats, bool>> predicate) => stats.AsQueryable().Where(predicate));
            _goalRepositoryMock.Setup(goalRepository => goalRepository.Find(It.IsAny<Expression<Func<Goal, bool>>>()))
                .Returns((Expression<Func<Goal, bool>> predicate) => goals.AsQueryable().Where(predicate));

            await _betService.TryAutoResolveFinishedMatch(1);

            _betResultRepositoryMock.Verify(betResultRepository => betResultRepository.Create(It.Is<BetResult>(result =>
                result.BetId == 1 && result.Point == BetScoringRules.CorrectOutcomePoints)), Times.Once);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task TryAutoResolveFinishedMatch_WhenResolveThrows_SwallowsInvalidOperationException()
        {
            DateTime pastKickoff = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 10));
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = pastKickoff,
                StadiumId = 1,
                TeamOneId = 10,
                TeamTwoId = 20
            };
            List<Bet> bets = new List<Bet>
            {
                new Bet { Id = 1, UserId = 1, MatchId = 1, TeamId = 10, IsDraw = false }
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            _betRepositoryMock.Setup(betRepository => betRepository.Find(It.IsAny<Expression<Func<Bet, bool>>>()))
                .Returns((Expression<Func<Bet, bool>> predicate) => bets.AsQueryable().Where(predicate));
            // Incomplete stats: GetMatchWinnerTeamId throws InvalidOperationException, which TryAutoResolve swallows.
            _teamStatsRepositoryMock.Setup(teamStatsRepository => teamStatsRepository.Find(It.IsAny<Expression<Func<TeamStats, bool>>>()))
                .Returns(new List<TeamStats>().AsQueryable());

            await _betService.TryAutoResolveFinishedMatch(1);

            _betResultRepositoryMock.Verify(betResultRepository => betResultRepository.Create(It.IsAny<BetResult>()), Times.Never);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Never);
        }
    }
}
