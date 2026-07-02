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

            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Match).Returns(_matchRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Bet).Returns(_betRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.BetResult).Returns(_betResultRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Team).Returns(_teamRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Country).Returns(_countryRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.TeamStats).Returns(_teamStatsRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Goal).Returns(_goalRepositoryMock.Object);

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

            int resolvedCount = await _betService.ResolveBetsForMatch(1);

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

            int resolvedCount = await _betService.ResolveBetsForMatch(1);

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
    }
}
