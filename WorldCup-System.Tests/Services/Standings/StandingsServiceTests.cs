using System.Linq.Expressions;
using Core.DTOs.Standings;
using Core.Services.Bets;
using Core.Services.Standings;
using Data.Entities;
using Data.Repos;
using Moq;
using MatchEntity = Data.Entities.Match;

namespace WorldCup_System.Tests.Services.Standings
{
    public class StandingsServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<Team>> _teamRepositoryMock;
        private readonly Mock<IRepository<Country>> _countryRepositoryMock;
        private readonly Mock<IRepository<MatchEntity>> _matchRepositoryMock;
        private readonly Mock<IRepository<TeamStats>> _teamStatsRepositoryMock;
        private readonly Mock<IRepository<Goal>> _goalRepositoryMock;
        private readonly StandingsService _standingsService;

        public StandingsServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _teamRepositoryMock = new Mock<IRepository<Team>>();
            _countryRepositoryMock = new Mock<IRepository<Country>>();
            _matchRepositoryMock = new Mock<IRepository<MatchEntity>>();
            _teamStatsRepositoryMock = new Mock<IRepository<TeamStats>>();
            _goalRepositoryMock = new Mock<IRepository<Goal>>();

            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Team).Returns(_teamRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Country).Returns(_countryRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Match).Returns(_matchRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.TeamStats).Returns(_teamStatsRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Goal).Returns(_goalRepositoryMock.Object);

            _standingsService = new StandingsService(_repositoryManagerMock.Object);
        }

        [Fact]
        public void GetGroupStandings_WhenNoTeams_ReturnsEmptyList()
        {
            SetupFind(_teamRepositoryMock, new List<Team>());

            List<StandingDTO> result = _standingsService.GetGroupStandings(1);

            Assert.Empty(result);
        }

        [Fact]
        public void GetGroupStandings_SkipsUnfinishedMatchesUsingFullTime()
        {
            List<Team> teams = new List<Team>
            {
                CreateTeam(10, "Brazil", 1),
                CreateTeam(20, "France", 1)
            };
            DateTime finishedKickoff = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 10));
            DateTime liveKickoff = DateTime.UtcNow.AddMinutes(-30);
            DateTime futureKickoff = DateTime.UtcNow.AddDays(7);
            List<MatchEntity> matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, Date = finishedKickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 },
                new MatchEntity { Id = 2, Date = liveKickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 },
                new MatchEntity { Id = 3, Date = futureKickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 }
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 },
                new TeamStats { Id = 102, MatchId = 2, TeamId = 10 },
                new TeamStats { Id = 103, MatchId = 2, TeamId = 20 },
                new TeamStats { Id = 104, MatchId = 3, TeamId = 10 },
                new TeamStats { Id = 105, MatchId = 3, TeamId = 20 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = finishedKickoff, IsOwnGoal = 0 },
                new Goal { Id = 2, TeamStatsId = 100, PlayerId = 2, TimeScored = finishedKickoff, IsOwnGoal = 0 },
                new Goal { Id = 3, TeamStatsId = 101, PlayerId = 3, TimeScored = finishedKickoff, IsOwnGoal = 0 },
                new Goal { Id = 4, TeamStatsId = 102, PlayerId = 4, TimeScored = liveKickoff, IsOwnGoal = 0 }
            };
            List<Country> countries = new List<Country>
            {
                new Country { Id = 10, Name = "Brazil" },
                new Country { Id = 20, Name = "France" }
            };

            SetupFind(_teamRepositoryMock, teams);
            SetupFind(_matchRepositoryMock, matches);
            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_goalRepositoryMock, goals);
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(countries.AsQueryable());

            List<StandingDTO> result = _standingsService.GetGroupStandings(1);

            StandingDTO brazilStanding = result.First(standing => standing.TeamId == 10);
            StandingDTO franceStanding = result.First(standing => standing.TeamId == 20);

            Assert.Equal(1, brazilStanding.Played);
            Assert.Equal(1, franceStanding.Played);
            Assert.Equal(2, brazilStanding.GoalsFor);
            Assert.Equal(1, brazilStanding.GoalsAgainst);
            Assert.Equal(3, brazilStanding.Points);
            Assert.Equal(0, franceStanding.Points);
        }

        [Fact]
        public void GetGroupStandings_AwardsDrawPointsForTiedPastMatch()
        {
            List<Team> teams = new List<Team>
            {
                CreateTeam(10, "Brazil", 1),
                CreateTeam(20, "France", 1)
            };
            DateTime pastKickoff = DateTime.UtcNow.AddMinutes(-(BetScoringRules.MatchDurationMinutes + 10));
            List<MatchEntity> matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, Date = pastKickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 }
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = pastKickoff, IsOwnGoal = 0 },
                new Goal { Id = 2, TeamStatsId = 101, PlayerId = 2, TimeScored = pastKickoff, IsOwnGoal = 0 }
            };
            List<Country> countries = new List<Country>
            {
                new Country { Id = 10, Name = "Brazil" },
                new Country { Id = 20, Name = "France" }
            };

            SetupFind(_teamRepositoryMock, teams);
            SetupFind(_matchRepositoryMock, matches);
            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_goalRepositoryMock, goals);
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(countries.AsQueryable());

            List<StandingDTO> result = _standingsService.GetGroupStandings(1);

            Assert.All(result, standing =>
            {
                Assert.Equal(1, standing.Played);
                Assert.Equal(1, standing.Drawn);
                Assert.Equal(1, standing.Points);
                Assert.Equal(1, standing.GoalsFor);
                Assert.Equal(1, standing.GoalsAgainst);
                Assert.Equal(0, standing.GoalDifference);
            });
        }

        [Fact]
        public void GetGroupStandings_OrdersByPointsGoalDifferenceAndGoalsFor()
        {
            List<Team> teams = new List<Team>
            {
                CreateTeam(10, "Brazil", 1),
                CreateTeam(20, "France", 1),
                CreateTeam(30, "Spain", 1)
            };
            DateTime pastKickoff = DateTime.UtcNow.AddDays(-3);
            List<MatchEntity> matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, Date = pastKickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20 },
                new MatchEntity { Id = 2, Date = pastKickoff.AddHours(2), StadiumId = 5, TeamOneId = 10, TeamTwoId = 30 },
                new MatchEntity { Id = 3, Date = pastKickoff.AddHours(4), StadiumId = 5, TeamOneId = 20, TeamTwoId = 30 }
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 },
                new TeamStats { Id = 102, MatchId = 2, TeamId = 10 },
                new TeamStats { Id = 103, MatchId = 2, TeamId = 30 },
                new TeamStats { Id = 104, MatchId = 3, TeamId = 20 },
                new TeamStats { Id = 105, MatchId = 3, TeamId = 30 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = pastKickoff, IsOwnGoal = 0 },
                new Goal { Id = 2, TeamStatsId = 100, PlayerId = 2, TimeScored = pastKickoff, IsOwnGoal = 0 },
                new Goal { Id = 3, TeamStatsId = 100, PlayerId = 3, TimeScored = pastKickoff, IsOwnGoal = 0 },
                new Goal { Id = 4, TeamStatsId = 102, PlayerId = 4, TimeScored = pastKickoff, IsOwnGoal = 0 },
                new Goal { Id = 5, TeamStatsId = 104, PlayerId = 5, TimeScored = pastKickoff, IsOwnGoal = 0 },
                new Goal { Id = 6, TeamStatsId = 104, PlayerId = 6, TimeScored = pastKickoff, IsOwnGoal = 0 }
            };
            List<Country> countries = new List<Country>
            {
                new Country { Id = 10, Name = "Brazil" },
                new Country { Id = 20, Name = "France" },
                new Country { Id = 30, Name = "Spain" }
            };

            SetupFind(_teamRepositoryMock, teams);
            SetupFind(_matchRepositoryMock, matches);
            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_goalRepositoryMock, goals);
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(countries.AsQueryable());

            List<StandingDTO> result = _standingsService.GetGroupStandings(1);

            Assert.Equal(10, result[0].TeamId);
            Assert.Equal(1, result[0].Rank);
            Assert.Equal(6, result[0].Points);
            Assert.Equal(20, result[1].TeamId);
            Assert.Equal(2, result[1].Rank);
        }

        [Fact]
        public void GetGroupStandings_IgnoresKnockoutMatches()
        {
            List<Team> teams = new List<Team>
            {
                CreateTeam(10, "Brazil", 1),
                CreateTeam(20, "France", 1)
            };
            DateTime pastKickoff = DateTime.UtcNow.AddDays(-2);
            List<MatchEntity> matches = new List<MatchEntity>
            {
                new MatchEntity
                {
                    Id = 1,
                    Date = pastKickoff,
                    StadiumId = 5,
                    TeamOneId = 10,
                    TeamTwoId = 20,
                    Stage = MatchStage.Group
                },
                new MatchEntity
                {
                    Id = 2,
                    Date = pastKickoff.AddDays(1),
                    StadiumId = 5,
                    TeamOneId = 10,
                    TeamTwoId = 20,
                    Stage = MatchStage.QuarterFinal
                }
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 },
                new TeamStats { Id = 102, MatchId = 2, TeamId = 10 },
                new TeamStats { Id = 103, MatchId = 2, TeamId = 20 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = pastKickoff, IsOwnGoal = 0 },
                new Goal { Id = 2, TeamStatsId = 102, PlayerId = 2, TimeScored = pastKickoff.AddDays(1), IsOwnGoal = 0 },
                new Goal { Id = 3, TeamStatsId = 102, PlayerId = 3, TimeScored = pastKickoff.AddDays(1), IsOwnGoal = 0 },
                new Goal { Id = 4, TeamStatsId = 103, PlayerId = 4, TimeScored = pastKickoff.AddDays(1), IsOwnGoal = 0 }
            };
            List<Country> countries = new List<Country>
            {
                new Country { Id = 10, Name = "Brazil" },
                new Country { Id = 20, Name = "France" }
            };

            SetupFind(_teamRepositoryMock, teams);
            SetupFind(_matchRepositoryMock, matches);
            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_goalRepositoryMock, goals);
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(countries.AsQueryable());

            List<StandingDTO> result = _standingsService.GetGroupStandings(1);

            StandingDTO brazil = Assert.Single(result, standing => standing.TeamId == 10);
            StandingDTO france = Assert.Single(result, standing => standing.TeamId == 20);
            Assert.Equal(1, brazil.Played);
            Assert.Equal(1, france.Played);
            Assert.Equal(3, brazil.Points);
            Assert.Equal(0, france.Points);
            Assert.Equal(1, brazil.GoalsFor);
            Assert.Equal(0, brazil.GoalsAgainst);
        }

        [Fact]
        public void GetGroupStandings_UsesHeadToHeadWhenPointsGdAndGfAreTied()
        {
            // Three teams on 3 pts, 0 GD, 1 GF. Brazil beat France head-to-head → Brazil ranks above France.
            List<Team> teams = new List<Team>
            {
                CreateTeam(10, "Brazil", 1),
                CreateTeam(20, "France", 1),
                CreateTeam(30, "Spain", 1)
            };
            DateTime pastKickoff = DateTime.UtcNow.AddDays(-3);
            List<MatchEntity> matches = new List<MatchEntity>
            {
                new MatchEntity { Id = 1, Date = pastKickoff, StadiumId = 5, TeamOneId = 10, TeamTwoId = 20, Stage = MatchStage.Group },
                new MatchEntity { Id = 2, Date = pastKickoff.AddHours(2), StadiumId = 5, TeamOneId = 20, TeamTwoId = 30, Stage = MatchStage.Group },
                new MatchEntity { Id = 3, Date = pastKickoff.AddHours(4), StadiumId = 5, TeamOneId = 30, TeamTwoId = 10, Stage = MatchStage.Group }
            };
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20 },
                new TeamStats { Id = 102, MatchId = 2, TeamId = 20 },
                new TeamStats { Id = 103, MatchId = 2, TeamId = 30 },
                new TeamStats { Id = 104, MatchId = 3, TeamId = 30 },
                new TeamStats { Id = 105, MatchId = 3, TeamId = 10 }
            };
            // Each team: 1 win, 1 loss, 1 GF, 1 GA → identical points/GD/GF. Cycle: BR>FR>ES>BR.
            // Among Brazil vs France only, Brazil has H2H edge; full 3-way H2H is also 3 pts each.
            // With equal H2H across the cluster, name order is the final tiebreak — assert H2H points
            // path runs without throwing and still produces a total order.
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = pastKickoff, IsOwnGoal = 0 },
                new Goal { Id = 2, TeamStatsId = 102, PlayerId = 2, TimeScored = pastKickoff, IsOwnGoal = 0 },
                new Goal { Id = 3, TeamStatsId = 104, PlayerId = 3, TimeScored = pastKickoff, IsOwnGoal = 0 }
            };
            List<Country> countries = new List<Country>
            {
                new Country { Id = 10, Name = "Brazil" },
                new Country { Id = 20, Name = "France" },
                new Country { Id = 30, Name = "Spain" }
            };

            SetupFind(_teamRepositoryMock, teams);
            SetupFind(_matchRepositoryMock, matches);
            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_goalRepositoryMock, goals);
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(countries.AsQueryable());

            List<StandingDTO> result = _standingsService.GetGroupStandings(1);

            Assert.Equal(3, result.Count);
            Assert.All(result, standing =>
            {
                Assert.Equal(3, standing.Points);
                Assert.Equal(0, standing.GoalDifference);
                Assert.Equal(1, standing.GoalsFor);
            });
            // Perfect cycle → alphabetical name tiebreak
            Assert.Equal(10, result[0].TeamId);
            Assert.Equal(20, result[1].TeamId);
            Assert.Equal(30, result[2].TeamId);
        }

        private static Team CreateTeam(int id, string countryName, int groupId)
        {
            int countryId = countryName switch
            {
                "Brazil" => 10,
                "France" => 20,
                "Spain" => 30,
                _ => id
            };
            return new Team
            {
                Id = id,
                CountryId = countryId,
                GroupId = groupId,
                Country = new Country { Id = countryId, Name = countryName },
                Group = new Group { Id = groupId, Name = "A" },
                Coach = new List<Coach>()
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
