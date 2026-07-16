using System.Linq.Expressions;
using Core.DTOs.Stats;
using Core.Services.Stats;
using Data.Entities;
using Data.Repos;
using Moq;
using MatchEntity = Data.Entities.Match;

namespace WorldCup_System.Tests.Services.Stats
{
    public class TeamStatsServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<MatchEntity>> _matchRepositoryMock;
        private readonly Mock<IRepository<TeamStats>> _teamStatsRepositoryMock;
        private readonly Mock<IRepository<Goal>> _goalRepositoryMock;
        private readonly Mock<IRepository<Team>> _teamRepositoryMock;
        private readonly Mock<IRepository<Country>> _countryRepositoryMock;
        private readonly TeamStatsService _teamStatsService;

        public TeamStatsServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _matchRepositoryMock = new Mock<IRepository<MatchEntity>>();
            _teamStatsRepositoryMock = new Mock<IRepository<TeamStats>>();
            _goalRepositoryMock = new Mock<IRepository<Goal>>();
            _teamRepositoryMock = new Mock<IRepository<Team>>();
            _countryRepositoryMock = new Mock<IRepository<Country>>();

            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Match).Returns(_matchRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.TeamStats).Returns(_teamStatsRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Goal).Returns(_goalRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Team).Returns(_teamRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Country).Returns(_countryRepositoryMock.Object);

            _teamStatsService = new TeamStatsService(_repositoryManagerMock.Object);
        }

        [Fact]
        public void GetTeamStatsByMatch_ReturnsMappedStatsWithScores()
        {
            List<TeamStats> stats = new List<TeamStats>
            {
                new TeamStats { Id = 100, MatchId = 1, TeamId = 10, Possession = 55, Shots = 12, ShotsOnTarget = 5 },
                new TeamStats { Id = 101, MatchId = 1, TeamId = 20, Possession = 45, Shots = 8, ShotsOnTarget = 3 }
            };
            List<Goal> goals = new List<Goal>
            {
                new Goal { Id = 1, TeamStatsId = 100, PlayerId = 1, TimeScored = DateTime.UtcNow, IsOwnGoal = 0 },
                new Goal { Id = 2, TeamStatsId = 100, PlayerId = 2, TimeScored = DateTime.UtcNow, IsOwnGoal = 0 }
            };
            List<Team> teams = new List<Team>
            {
                CreateTeam(10, "Brazil", 1),
                CreateTeam(20, "France", 1)
            };
            List<Country> countries = new List<Country>
            {
                new Country { Id = 10, Name = "Brazil" },
                new Country { Id = 20, Name = "France" }
            };

            SetupFind(_teamStatsRepositoryMock, stats);
            SetupFind(_goalRepositoryMock, goals);
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetAllAsync()).Returns(teams.AsQueryable());
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(countries.AsQueryable());

            List<TeamStatsDTO> result = _teamStatsService.GetTeamStatsByMatch(1);

            Assert.Equal(2, result.Count);
            TeamStatsDTO teamOneStats = result.First(teamStatsDto => teamStatsDto.TeamId == 10);
            Assert.Equal(2, teamOneStats.Score);
            Assert.Equal("Brazil", teamOneStats.TeamName);
        }

        [Fact]
        public async Task UpdateTeamStats_WhenPossessionSumsTo100_UpdatesAndSaves()
        {
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = DateTime.UtcNow,
                StadiumId = 5,
                TeamOneId = 10,
                TeamTwoId = 20
            };
            TeamStats teamOneStats = new TeamStats { Id = 100, MatchId = 1, TeamId = 10, Possession = 50, Shots = 0, ShotsOnTarget = 0 };
            TeamStats teamTwoStats = new TeamStats { Id = 101, MatchId = 1, TeamId = 20, Possession = 40, Shots = 0, ShotsOnTarget = 0 };
            UpdateTeamStatsDTO updateDto = new UpdateTeamStatsDTO
            {
                MatchId = 1,
                TeamId = 10,
                Possession = 60,
                Shots = 14,
                ShotsOnTarget = 6
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            SetupFind(_teamStatsRepositoryMock, new List<TeamStats> { teamOneStats, teamTwoStats });
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _teamStatsService.UpdateTeamStats(updateDto);

            Assert.Equal(60, teamOneStats.Possession);
            Assert.Equal(14, teamOneStats.Shots);
            _teamStatsRepositoryMock.Verify(teamStatsRepository => teamStatsRepository.Update(teamOneStats), Times.Once);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateTeamStats_WhenBothPossessionZeroAndSettingSplit_AutoBalancesOpponent()
        {
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = DateTime.UtcNow,
                StadiumId = 5,
                TeamOneId = 10,
                TeamTwoId = 20
            };
            TeamStats teamOneStats = new TeamStats { Id = 100, MatchId = 1, TeamId = 10, Possession = 0, Shots = 0, ShotsOnTarget = 0 };
            TeamStats teamTwoStats = new TeamStats { Id = 101, MatchId = 1, TeamId = 20, Possession = 0, Shots = 0, ShotsOnTarget = 0 };
            UpdateTeamStatsDTO updateDto = new UpdateTeamStatsDTO
            {
                MatchId = 1,
                TeamId = 10,
                Possession = 50,
                Shots = 8,
                ShotsOnTarget = 3
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            SetupFind(_teamStatsRepositoryMock, new List<TeamStats> { teamOneStats, teamTwoStats });
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _teamStatsService.UpdateTeamStats(updateDto);

            Assert.Equal(50, teamOneStats.Possession);
            Assert.Equal(50, teamTwoStats.Possession);
            _teamStatsRepositoryMock.Verify(teamStatsRepository => teamStatsRepository.Update(teamTwoStats), Times.Once);
        }

        [Fact]
        public async Task UpdateTeamStats_WhenChangingSplit_AutoBalancesOpponent()
        {
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = DateTime.UtcNow,
                StadiumId = 5,
                TeamOneId = 10,
                TeamTwoId = 20
            };
            TeamStats teamOneStats = new TeamStats { Id = 100, MatchId = 1, TeamId = 10, Possession = 50 };
            TeamStats teamTwoStats = new TeamStats { Id = 101, MatchId = 1, TeamId = 20, Possession = 50 };
            UpdateTeamStatsDTO updateDto = new UpdateTeamStatsDTO
            {
                MatchId = 1,
                TeamId = 10,
                Possession = 70,
                Shots = 10,
                ShotsOnTarget = 4
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            SetupFind(_teamStatsRepositoryMock, new List<TeamStats> { teamOneStats, teamTwoStats });
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _teamStatsService.UpdateTeamStats(updateDto);

            Assert.Equal(70, teamOneStats.Possession);
            Assert.Equal(30, teamTwoStats.Possession);
            _teamStatsRepositoryMock.Verify(teamStatsRepository => teamStatsRepository.Update(teamTwoStats), Times.Once);
        }

        [Fact]
        public async Task UpdateTeamStats_WhenPossessionOutOfRange_ThrowsInvalidOperationException()
        {
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = DateTime.UtcNow,
                StadiumId = 5,
                TeamOneId = 10,
                TeamTwoId = 20
            };
            TeamStats teamOneStats = new TeamStats { Id = 100, MatchId = 1, TeamId = 10, Possession = 50 };
            TeamStats teamTwoStats = new TeamStats { Id = 101, MatchId = 1, TeamId = 20, Possession = 50 };
            UpdateTeamStatsDTO updateDto = new UpdateTeamStatsDTO
            {
                MatchId = 1,
                TeamId = 10,
                Possession = 120,
                Shots = 10,
                ShotsOnTarget = 4
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            SetupFind(_teamStatsRepositoryMock, new List<TeamStats> { teamOneStats, teamTwoStats });

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _teamStatsService.UpdateTeamStats(updateDto));

            Assert.Contains("between 0 and 100", exception.Message);
        }

        [Fact]
        public async Task UpdateTeamStats_WhenBothPossessionZero_Succeeds()
        {
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = DateTime.UtcNow,
                StadiumId = 5,
                TeamOneId = 10,
                TeamTwoId = 20
            };
            TeamStats teamOneStats = new TeamStats { Id = 100, MatchId = 1, TeamId = 10, Possession = 0 };
            TeamStats teamTwoStats = new TeamStats { Id = 101, MatchId = 1, TeamId = 20, Possession = 0 };
            UpdateTeamStatsDTO updateDto = new UpdateTeamStatsDTO
            {
                MatchId = 1,
                TeamId = 10,
                Possession = 0,
                Shots = 5,
                ShotsOnTarget = 2
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            SetupFind(_teamStatsRepositoryMock, new List<TeamStats> { teamOneStats, teamTwoStats });
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _teamStatsService.UpdateTeamStats(updateDto);

            Assert.Equal(5, teamOneStats.Shots);
            _teamStatsRepositoryMock.Verify(teamStatsRepository => teamStatsRepository.Update(teamOneStats), Times.Once);
        }

        [Fact]
        public async Task UpdateTeamStats_WhenStatsRecordMissing_ThrowsInvalidOperationException()
        {
            MatchEntity match = new MatchEntity
            {
                Id = 1,
                Date = DateTime.UtcNow,
                StadiumId = 5,
                TeamOneId = 10,
                TeamTwoId = 20
            };
            UpdateTeamStatsDTO updateDto = new UpdateTeamStatsDTO
            {
                MatchId = 1,
                TeamId = 99,
                Possession = 50,
                Shots = 0,
                ShotsOnTarget = 0
            };

            _matchRepositoryMock.Setup(matchRepository => matchRepository.GetByIdAsync(1)).ReturnsAsync(match);
            SetupFind(_teamStatsRepositoryMock, new List<TeamStats>());

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _teamStatsService.UpdateTeamStats(updateDto));

            Assert.Contains("No team stats record exists", exception.Message);
        }

        private static Team CreateTeam(int id, string countryName, int groupId)
        {
            int countryId = countryName switch
            {
                "Brazil" => 10,
                "France" => 20,
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
