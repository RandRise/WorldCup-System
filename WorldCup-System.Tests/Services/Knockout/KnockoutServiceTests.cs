using System.Linq.Expressions;
using Core.DTOs.Matches;
using Core.DTOs.Standings;
using Core.Services.Bets;
using Core.Services.Knockout;
using Core.Services.Standings;
using Data.Entities;
using Data.Repos;
using Moq;
using MatchEntity = Data.Entities.Match;

namespace WorldCup_System.Tests.Services.Knockout
{
    public class KnockoutServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<MatchEntity>> _matchRepositoryMock;
        private readonly Mock<IRepository<TeamStats>> _teamStatsRepositoryMock;
        private readonly Mock<IRepository<Goal>> _goalRepositoryMock;
        private readonly Mock<IRepository<Card>> _cardRepositoryMock;
        private readonly Mock<IRepository<Group>> _groupRepositoryMock;
        private readonly Mock<IRepository<Team>> _teamRepositoryMock;
        private readonly Mock<IRepository<Country>> _countryRepositoryMock;
        private readonly Mock<IRepository<Stadium>> _stadiumRepositoryMock;
        private readonly Mock<IRepository<WorldCup>> _worldCupRepositoryMock;
        private readonly Mock<IStandingsService> _standingsServiceMock;
        private readonly KnockoutService _knockoutService;

        public KnockoutServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _matchRepositoryMock = new Mock<IRepository<MatchEntity>>();
            _teamStatsRepositoryMock = new Mock<IRepository<TeamStats>>();
            _goalRepositoryMock = new Mock<IRepository<Goal>>();
            _cardRepositoryMock = new Mock<IRepository<Card>>();
            _groupRepositoryMock = new Mock<IRepository<Group>>();
            _teamRepositoryMock = new Mock<IRepository<Team>>();
            _countryRepositoryMock = new Mock<IRepository<Country>>();
            _stadiumRepositoryMock = new Mock<IRepository<Stadium>>();
            _worldCupRepositoryMock = new Mock<IRepository<WorldCup>>();
            _standingsServiceMock = new Mock<IStandingsService>();

            _repositoryManagerMock.Setup(manager => manager.Match).Returns(_matchRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.TeamStats).Returns(_teamStatsRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.Goal).Returns(_goalRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.Card).Returns(_cardRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.Group).Returns(_groupRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.Team).Returns(_teamRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.Country).Returns(_countryRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.Stadium).Returns(_stadiumRepositoryMock.Object);
            _repositoryManagerMock.Setup(manager => manager.WorldCup).Returns(_worldCupRepositoryMock.Object);

            _knockoutService = new KnockoutService(_repositoryManagerMock.Object, _standingsServiceMock.Object);
        }

        [Fact]
        public async Task TryAdvanceFromMatch_FillsDestinationSlotsWithWinnerAndLoser()
        {
            DateTime kickoff = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 5));
            MatchEntity source = new MatchEntity
            {
                Id = 10,
                Date = kickoff,
                Stage = MatchStage.SemiFinal,
                StadiumId = 1,
                TeamOneId = 100,
                TeamTwoId = 200
            };
            MatchEntity final = new MatchEntity
            {
                Id = 20,
                Date = kickoff.AddDays(3),
                Stage = MatchStage.Final,
                StadiumId = 1,
                FeederMatchOneId = 10,
                FeederMatchTwoId = 11,
                FeederOneTakesLoser = false,
                FeederTwoTakesLoser = false
            };
            MatchEntity third = new MatchEntity
            {
                Id = 21,
                Date = kickoff.AddDays(2),
                Stage = MatchStage.ThirdPlace,
                StadiumId = 1,
                FeederMatchOneId = 10,
                FeederMatchTwoId = 11,
                FeederOneTakesLoser = true,
                FeederTwoTakesLoser = true
            };

            List<TeamStats> sourceStats = new List<TeamStats>
            {
                new TeamStats { Id = 1, MatchId = 10, TeamId = 100 },
                new TeamStats { Id = 2, MatchId = 10, TeamId = 200 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 1, PlayerId = 1, TimeScored = kickoff, IsOwnGoal = 0 },
                new Goal { Id = 2, TeamStatsId = 1, PlayerId = 2, TimeScored = kickoff, IsOwnGoal = 0 },
                new Goal { Id = 3, TeamStatsId = 2, PlayerId = 3, TimeScored = kickoff, IsOwnGoal = 0 }
            };

            _matchRepositoryMock
                .Setup(repository => repository.GetByIdAsync(10))
                .ReturnsAsync(source);
            SetupFind(_matchRepositoryMock, new List<MatchEntity> { final, third });
            SetupFind(_teamStatsRepositoryMock, sourceStats);
            SetupFind(_goalRepositoryMock, goals);
            SetupFind(_cardRepositoryMock, new List<Card>());
            _repositoryManagerMock.Setup(manager => manager.SaveAsync()).Returns(Task.CompletedTask);

            await _knockoutService.TryAdvanceFromMatch(10);

            Assert.Equal(100, final.TeamOneId);
            Assert.Equal(200, third.TeamOneId);
            _matchRepositoryMock.Verify(repository => repository.Update(final), Times.Once);
            _matchRepositoryMock.Verify(repository => repository.Update(third), Times.Once);
            _teamStatsRepositoryMock.Verify(
                repository => repository.Create(It.Is<TeamStats>(stats => stats.TeamId == 100 && stats.MatchId == 20)),
                Times.Once);
            _teamStatsRepositoryMock.Verify(
                repository => repository.Create(It.Is<TeamStats>(stats => stats.TeamId == 200 && stats.MatchId == 21)),
                Times.Once);
        }

        [Fact]
        public async Task GenerateBracket_WhenNotEightGroups_Throws()
        {
            _worldCupRepositoryMock
                .Setup(repository => repository.GetByIdAsync(1))
                .ReturnsAsync(new WorldCup { Id = 1, Year = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc) });
            _stadiumRepositoryMock
                .Setup(repository => repository.GetByIdAsync(5))
                .ReturnsAsync(new Stadium { Id = 5, Name = "Arena", CityId = 1 });
            SetupFind(_groupRepositoryMock, new List<Group>
            {
                new Group { Id = 1, Name = "A", WorldCupId = 1 },
                new Group { Id = 2, Name = "B", WorldCupId = 1 }
            });
            // GetWorldCupKnockoutMatches is only reached after the 8-group check fails.

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _knockoutService.GenerateBracket(new GenerateBracketDTO
                {
                    WorldCupId = 1,
                    StadiumId = 5,
                    FirstKickoff = DateTime.UtcNow.AddDays(1)
                }));

            Assert.Contains("exactly 8 groups", exception.Message);
        }

        [Fact]
        public async Task GenerateBracket_WhenKnockoutMatchesAlreadyExist_Throws()
        {
            List<Group> groups = Enumerable.Range(0, 8)
                .Select(index => new Group { Id = index + 1, Name = ((char)('A' + index)).ToString(), WorldCupId = 1 })
                .ToList();
            List<Team> teams = new List<Team>
            {
                new Team { Id = 100, GroupId = 1, CountryId = 1, Country = new Country { Id = 1, Name = "Brazil" }, Group = groups[0], Coach = new List<Coach>() }
            };
            MatchEntity existingKnockout = new MatchEntity
            {
                Id = 90,
                Date = DateTime.UtcNow.AddDays(5),
                Stage = MatchStage.RoundOf16,
                StadiumId = 5,
                TeamOneId = 100,
                TeamTwoId = null
            };

            _worldCupRepositoryMock
                .Setup(repository => repository.GetByIdAsync(1))
                .ReturnsAsync(new WorldCup { Id = 1, Year = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc) });
            _stadiumRepositoryMock
                .Setup(repository => repository.GetByIdAsync(5))
                .ReturnsAsync(new Stadium { Id = 5, Name = "Arena", CityId = 1 });
            SetupFind(_groupRepositoryMock, groups);
            SetupFind(_teamRepositoryMock, teams);
            _matchRepositoryMock.Setup(repository => repository.GetAllAsync())
                .Returns(new List<MatchEntity> { existingKnockout }.AsQueryable());
            SetupFind(_matchRepositoryMock, new List<MatchEntity> { existingKnockout });

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _knockoutService.GenerateBracket(new GenerateBracketDTO
                {
                    WorldCupId = 1,
                    StadiumId = 5,
                    FirstKickoff = DateTime.UtcNow.AddDays(1)
                }));

            Assert.Contains("already exist", exception.Message);
        }

        [Fact]
        public void GetBracket_ReturnsRoundsGroupedByStageWithTbdNames()
        {
            List<Group> groups = new List<Group> { new Group { Id = 1, Name = "A", WorldCupId = 2026 } };
            List<Team> teams = new List<Team>
            {
                new Team
                {
                    Id = 100,
                    GroupId = 1,
                    CountryId = 1,
                    Country = new Country { Id = 1, Name = "Brazil" },
                    Group = groups[0],
                    Coach = new List<Coach>()
                }
            };
            MatchEntity quarter = new MatchEntity
            {
                Id = 30,
                Date = DateTime.UtcNow.AddDays(2),
                Stage = MatchStage.QuarterFinal,
                StadiumId = 5,
                TeamOneId = 100,
                TeamTwoId = null,
                FeederMatchOneId = 10,
                FeederMatchTwoId = 11
            };
            MatchEntity final = new MatchEntity
            {
                Id = 40,
                Date = DateTime.UtcNow.AddDays(10),
                Stage = MatchStage.Final,
                StadiumId = 5,
                TeamOneId = null,
                TeamTwoId = null,
                FeederMatchOneId = 30,
                FeederMatchTwoId = 31
            };
            List<MatchEntity> matches = new List<MatchEntity> { quarter, final };

            SetupFind(_groupRepositoryMock, groups);
            SetupFind(_teamRepositoryMock, teams);
            _matchRepositoryMock.Setup(repository => repository.GetAllAsync()).Returns(matches.AsQueryable());
            SetupFind(_matchRepositoryMock, matches);
            _teamRepositoryMock.Setup(repository => repository.GetAllAsync()).Returns(teams.AsQueryable());
            _countryRepositoryMock.Setup(repository => repository.GetAllAsync())
                .Returns(new List<Country> { new Country { Id = 1, Name = "Brazil" } }.AsQueryable());
            _stadiumRepositoryMock.Setup(repository => repository.GetAllAsync())
                .Returns(new List<Stadium> { new Stadium { Id = 5, Name = "Arena", CityId = 1 } }.AsQueryable());
            SetupFind(_teamStatsRepositoryMock, new List<TeamStats>());
            SetupFind(_goalRepositoryMock, new List<Goal>());

            BracketDTO bracket = _knockoutService.GetBracket(2026);

            Assert.Equal(2026, bracket.WorldCupId);
            Assert.Equal(2, bracket.Rounds.Count);
            Assert.Equal(MatchStage.QuarterFinal, bracket.Rounds[0].Stage);
            Assert.Equal("Quarter-final", bracket.Rounds[0].StageName);
            Assert.Equal("Brazil", bracket.Rounds[0].Matches[0].TeamOneName);
            Assert.Equal("TBD", bracket.Rounds[0].Matches[0].TeamTwoName);
            Assert.False(bracket.Rounds[0].Matches[0].CanBet);
            Assert.Equal(MatchStage.Final, bracket.Rounds[1].Stage);
            Assert.Equal("TBD", bracket.Rounds[1].Matches[0].TeamOneName);
            Assert.Equal("TBD", bracket.Rounds[1].Matches[0].TeamTwoName);
        }

        [Fact]
        public async Task TryAdvanceFromMatch_WhenGroupStage_DoesNothing()
        {
            MatchEntity groupMatch = new MatchEntity
            {
                Id = 1,
                Date = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 5)),
                Stage = MatchStage.Group,
                StadiumId = 1,
                TeamOneId = 10,
                TeamTwoId = 20
            };
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(groupMatch);

            await _knockoutService.TryAdvanceFromMatch(1);

            _matchRepositoryMock.Verify(repository => repository.Update(It.IsAny<MatchEntity>()), Times.Never);
            _repositoryManagerMock.Verify(manager => manager.SaveAsync(), Times.Never);
        }

        [Fact]
        public async Task TryAdvanceFromMatch_WhenMatchStillLive_DoesNothing()
        {
            MatchEntity liveSemi = new MatchEntity
            {
                Id = 10,
                Date = DateTime.UtcNow.AddMinutes(-30),
                Stage = MatchStage.SemiFinal,
                StadiumId = 1,
                TeamOneId = 100,
                TeamTwoId = 200
            };
            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(10)).ReturnsAsync(liveSemi);

            await _knockoutService.TryAdvanceFromMatch(10);

            _matchRepositoryMock.Verify(repository => repository.Update(It.IsAny<MatchEntity>()), Times.Never);
            _repositoryManagerMock.Verify(manager => manager.SaveAsync(), Times.Never);
        }

        [Fact]
        public async Task TryAdvanceFromMatch_WhenDestinationHasEvents_Throws()
        {
            DateTime kickoff = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 5));
            MatchEntity source = new MatchEntity
            {
                Id = 10,
                Date = kickoff,
                Stage = MatchStage.SemiFinal,
                StadiumId = 1,
                TeamOneId = 100,
                TeamTwoId = 200
            };
            MatchEntity final = new MatchEntity
            {
                Id = 20,
                Date = kickoff.AddDays(3),
                Stage = MatchStage.Final,
                StadiumId = 1,
                FeederMatchOneId = 10,
                FeederMatchTwoId = 11,
                FeederOneTakesLoser = false,
                FeederTwoTakesLoser = false
            };
            List<TeamStats> sourceStats = new List<TeamStats>
            {
                new TeamStats { Id = 1, MatchId = 10, TeamId = 100 },
                new TeamStats { Id = 2, MatchId = 10, TeamId = 200 }
            };
            List<TeamStats> destinationStats = new List<TeamStats>
            {
                new TeamStats { Id = 10, MatchId = 20, TeamId = 999 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 1, PlayerId = 1, TimeScored = kickoff, IsOwnGoal = 0 },
                new Goal { Id = 2, TeamStatsId = 10, PlayerId = 9, TimeScored = kickoff, IsOwnGoal = 0 }
            };

            _matchRepositoryMock.Setup(repository => repository.GetByIdAsync(10)).ReturnsAsync(source);
            SetupFind(_matchRepositoryMock, new List<MatchEntity> { final });
            SetupFind(_teamStatsRepositoryMock, sourceStats.Concat(destinationStats).ToList());
            SetupFind(_goalRepositoryMock, goals);
            SetupFind(_cardRepositoryMock, new List<Card>());

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _knockoutService.TryAdvanceFromMatch(10));

            Assert.Contains("Cannot advance into match 20", exception.Message);
        }

        private static void SetupFind<T>(Mock<IRepository<T>> repositoryMock, List<T> items)
            where T : class
        {
            repositoryMock
                .Setup(repository => repository.Find(It.IsAny<Expression<Func<T, bool>>>()))
                .Returns((Expression<Func<T, bool>> predicate) => items.AsQueryable().Where(predicate));
        }
    }
}
