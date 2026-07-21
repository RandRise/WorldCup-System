using Core.DTOs.Teams;
using Core.Services.Teams;
using Data.Entities;
using Data.Repos;
using Moq;
using MatchEntity = Data.Entities.Match;

namespace WorldCup_System.Tests.Services.Teams
{
    public class TeamServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<Team>> _teamRepositoryMock;
        private readonly Mock<IRepository<Country>> _countryRepositoryMock;
        private readonly Mock<IRepository<Group>> _groupRepositoryMock;
        private readonly TeamService _teamService;

        public TeamServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _teamRepositoryMock = new Mock<IRepository<Team>>();
            _countryRepositoryMock = new Mock<IRepository<Country>>();
            _groupRepositoryMock = new Mock<IRepository<Group>>();
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Team).Returns(_teamRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Country).Returns(_countryRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Group).Returns(_groupRepositoryMock.Object);
            _teamService = new TeamService(_repositoryManagerMock.Object);
        }

        [Fact]
        public void GetTeams_ReturnsMappedTeamDtosWithCountryAndGroupNames()
        {
            List<Team> teams = new List<Team>
            {
                new Team { Id = 1, CountryId = 10, GroupId = 2, Country = new Country { Id = 10, Name = "Brazil" }, Group = new Group { Id = 2, Name = "A" }, Coach = new List<Coach>() }
            };
            List<Country> countries = new List<Country> { new Country { Id = 10, Name = "Brazil" } };
            List<Group> groups = new List<Group> { new Group { Id = 2, Name = "A" } };

            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetAllAsync()).Returns(teams.AsQueryable());
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetAllAsync()).Returns(countries.AsQueryable());
            _groupRepositoryMock.Setup(groupRepository => groupRepository.GetAllAsync()).Returns(groups.AsQueryable());

            List<TeamDTO> result = _teamService.GetTeams();

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
            Assert.Equal("Brazil", result[0].CountryName);
            Assert.Equal("A", result[0].GroupName);
        }

        [Fact]
        public async Task AddTeam_WhenCountryHasNoTeam_CreatesAndSaves()
        {
            Country country = new Country { Id = 5, Name = "France" };
            Group group = new Group { Id = 3, Name = "B", WorldCupId = 1 };
            AddTeamDTO addTeamDto = new AddTeamDTO { CountryId = 5, GroupId = 3 };

            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetByIdAsync(5)).ReturnsAsync(country);
            _groupRepositoryMock.Setup(groupRepository => groupRepository.GetByIdAsync(3)).ReturnsAsync(group);
            _teamRepositoryMock.Setup(teamRepository => teamRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Team, bool>>>()))
                .Returns(new List<Team>().AsQueryable());

            Team? capturedTeam = null;
            _teamRepositoryMock.Setup(teamRepository => teamRepository.Create(It.IsAny<Team>()))
                .Callback<Team>(team => capturedTeam = team);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _teamService.AddTeam(addTeamDto);

            Assert.NotNull(capturedTeam);
            Assert.Equal(5, capturedTeam.CountryId);
            Assert.Equal(3, capturedTeam.GroupId);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task AddTeam_WhenCountryAlreadyHasTeamInSameWorldCup_ThrowsInvalidOperationException()
        {
            Country country = new Country { Id = 5, Name = "France" };
            Group group = new Group { Id = 3, Name = "B", WorldCupId = 1 };
            Group existingGroup = new Group { Id = 1, Name = "A", WorldCupId = 1 };
            AddTeamDTO addTeamDto = new AddTeamDTO { CountryId = 5, GroupId = 3 };

            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetByIdAsync(5)).ReturnsAsync(country);
            _groupRepositoryMock.Setup(groupRepository => groupRepository.GetByIdAsync(3)).ReturnsAsync(group);
            _teamRepositoryMock.Setup(teamRepository => teamRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Team, bool>>>()))
                .Returns(new List<Team>
                {
                    new Team { Id = 99, CountryId = 5, GroupId = 1, Country = country, Group = existingGroup, Coach = new List<Coach>() }
                }.AsQueryable());
            _groupRepositoryMock.Setup(groupRepository => groupRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Group, bool>>>()))
                .Returns(new List<Group> { existingGroup }.AsQueryable());

            await Assert.ThrowsAsync<InvalidOperationException>(() => _teamService.AddTeam(addTeamDto));
        }

        [Fact]
        public async Task AddTeam_WhenCountryHasTeamInOtherWorldCup_CreatesAndSaves()
        {
            Country country = new Country { Id = 5, Name = "France" };
            Group group2030 = new Group { Id = 30, Name = "A", WorldCupId = 2 };
            Group group2026 = new Group { Id = 1, Name = "A", WorldCupId = 1 };
            AddTeamDTO addTeamDto = new AddTeamDTO { CountryId = 5, GroupId = 30 };

            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetByIdAsync(5)).ReturnsAsync(country);
            _groupRepositoryMock.Setup(groupRepository => groupRepository.GetByIdAsync(30)).ReturnsAsync(group2030);
            _teamRepositoryMock.Setup(teamRepository => teamRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Team, bool>>>()))
                .Returns(new List<Team>
                {
                    new Team { Id = 99, CountryId = 5, GroupId = 1, Country = country, Group = group2026, Coach = new List<Coach>() }
                }.AsQueryable());
            _groupRepositoryMock.Setup(groupRepository => groupRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Group, bool>>>()))
                .Returns(new List<Group>().AsQueryable());

            Team? capturedTeam = null;
            _teamRepositoryMock.Setup(teamRepository => teamRepository.Create(It.IsAny<Team>()))
                .Callback<Team>(team => capturedTeam = team);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _teamService.AddTeam(addTeamDto);

            Assert.NotNull(capturedTeam);
            Assert.Equal(5, capturedTeam.CountryId);
            Assert.Equal(30, capturedTeam.GroupId);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task AssignTeamToGroup_UpdatesGroupAndSaves()
        {
            Country country = new Country { Id = 1, Name = "Spain" };
            Group oldGroup = new Group { Id = 1, Name = "A", WorldCupId = 1 };
            Group newGroup = new Group { Id = 4, Name = "C", WorldCupId = 1 };
            Team team = new Team { Id = 7, CountryId = 1, GroupId = 1, Country = country, Group = oldGroup, Coach = new List<Coach>() };
            AssignTeamToGroupDTO assignDto = new AssignTeamToGroupDTO { TeamId = 7, GroupId = 4 };

            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(7)).ReturnsAsync(team);
            _groupRepositoryMock.Setup(groupRepository => groupRepository.GetByIdAsync(1)).ReturnsAsync(oldGroup);
            _groupRepositoryMock.Setup(groupRepository => groupRepository.GetByIdAsync(4)).ReturnsAsync(newGroup);
            _teamRepositoryMock.Setup(teamRepository => teamRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Team, bool>>>()))
                .Returns(new List<Team>().AsQueryable());
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _teamService.AssignTeamToGroup(assignDto);

            Assert.Equal(4, team.GroupId);
            _teamRepositoryMock.Verify(teamRepository => teamRepository.Update(team), Times.Once);
        }

        [Fact]
        public async Task AssignTeamToGroup_WhenTargetGroupIsOtherWorldCup_ThrowsInvalidOperationException()
        {
            Country country = new Country { Id = 1, Name = "Spain" };
            Group group2030 = new Group { Id = 30, Name = "A", WorldCupId = 2 };
            Group group2026 = new Group { Id = 1, Name = "A", WorldCupId = 1 };
            Team team = new Team { Id = 7, CountryId = 1, GroupId = 30, Country = country, Group = group2030, Coach = new List<Coach>() };
            AssignTeamToGroupDTO assignDto = new AssignTeamToGroupDTO { TeamId = 7, GroupId = 1 };

            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(7)).ReturnsAsync(team);
            _groupRepositoryMock.Setup(groupRepository => groupRepository.GetByIdAsync(30)).ReturnsAsync(group2030);
            _groupRepositoryMock.Setup(groupRepository => groupRepository.GetByIdAsync(1)).ReturnsAsync(group2026);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _teamService.AssignTeamToGroup(assignDto));

            Assert.Contains("Cannot move team 7 from World Cup 2 to World Cup 1", exception.Message);
            _teamRepositoryMock.Verify(teamRepository => teamRepository.Update(It.IsAny<Team>()), Times.Never);
        }

        [Fact]
        public async Task GetTeamById_ReturnsMappedTeamDto()
        {
            Country country = new Country { Id = 10, Name = "Brazil" };
            Group group = new Group { Id = 2, Name = "A" };
            Team team = new Team { Id = 1, CountryId = 10, GroupId = 2, Country = country, Group = group, Coach = new List<Coach>() };

            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(1)).ReturnsAsync(team);
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetByIdAsync(10)).ReturnsAsync(country);
            _groupRepositoryMock.Setup(groupRepository => groupRepository.GetByIdAsync(2)).ReturnsAsync(group);

            TeamDTO result = await _teamService.GetTeamById(1);

            Assert.Equal(1, result.Id);
            Assert.Equal("Brazil", result.CountryName);
            Assert.Equal("A", result.GroupName);
        }

        [Fact]
        public async Task UpdateTeam_WhenCountryChanged_UpdatesAndSaves()
        {
            Country oldCountry = new Country { Id = 1, Name = "Spain" };
            Country newCountry = new Country { Id = 5, Name = "France" };
            Group currentGroup = new Group { Id = 1, Name = "A", WorldCupId = 1 };
            Group group = new Group { Id = 3, Name = "B", WorldCupId = 1 };
            Team team = new Team { Id = 7, CountryId = 1, GroupId = 1, Country = oldCountry, Group = currentGroup, Coach = new List<Coach>() };
            UpdateTeamDTO updateTeamDto = new UpdateTeamDTO { Id = 7, CountryId = 5, GroupId = 3 };

            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(7)).ReturnsAsync(team);
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetByIdAsync(5)).ReturnsAsync(newCountry);
            _groupRepositoryMock.Setup(groupRepository => groupRepository.GetByIdAsync(1)).ReturnsAsync(currentGroup);
            _groupRepositoryMock.Setup(groupRepository => groupRepository.GetByIdAsync(3)).ReturnsAsync(group);
            _teamRepositoryMock.Setup(teamRepository => teamRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Team, bool>>>()))
                .Returns(new List<Team>().AsQueryable());
            _groupRepositoryMock.Setup(groupRepository => groupRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Group, bool>>>()))
                .Returns(new List<Group>().AsQueryable());
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _teamService.UpdateTeam(updateTeamDto);

            Assert.Equal(5, team.CountryId);
            Assert.Equal(3, team.GroupId);
            _teamRepositoryMock.Verify(teamRepository => teamRepository.Update(team), Times.Once);
        }

        [Fact]
        public async Task UpdateTeam_WhenTargetGroupIsOtherWorldCup_ThrowsInvalidOperationException()
        {
            Country country = new Country { Id = 1, Name = "Spain" };
            Group group2030 = new Group { Id = 30, Name = "A", WorldCupId = 2 };
            Group group2026 = new Group { Id = 3, Name = "B", WorldCupId = 1 };
            Team team = new Team { Id = 7, CountryId = 1, GroupId = 30, Country = country, Group = group2030, Coach = new List<Coach>() };
            UpdateTeamDTO updateTeamDto = new UpdateTeamDTO { Id = 7, CountryId = 1, GroupId = 3 };

            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(7)).ReturnsAsync(team);
            _groupRepositoryMock.Setup(groupRepository => groupRepository.GetByIdAsync(30)).ReturnsAsync(group2030);
            _groupRepositoryMock.Setup(groupRepository => groupRepository.GetByIdAsync(3)).ReturnsAsync(group2026);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _teamService.UpdateTeam(updateTeamDto));

            Assert.Contains("Cannot move team 7 from World Cup 2 to World Cup 1", exception.Message);
            _teamRepositoryMock.Verify(teamRepository => teamRepository.Update(It.IsAny<Team>()), Times.Never);
        }

        [Fact]
        public async Task UpdateTeam_WhenNewCountryAlreadyHasTeamInSameWorldCup_ThrowsInvalidOperationException()
        {
            Country oldCountry = new Country { Id = 1, Name = "Spain" };
            Country newCountry = new Country { Id = 5, Name = "France" };
            Group group = new Group { Id = 3, Name = "B", WorldCupId = 1 };
            Group existingGroup = new Group { Id = 1, Name = "A", WorldCupId = 1 };
            Team team = new Team { Id = 7, CountryId = 1, GroupId = 1, Country = oldCountry, Group = existingGroup, Coach = new List<Coach>() };
            UpdateTeamDTO updateTeamDto = new UpdateTeamDTO { Id = 7, CountryId = 5, GroupId = 3 };

            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(7)).ReturnsAsync(team);
            _countryRepositoryMock.Setup(countryRepository => countryRepository.GetByIdAsync(5)).ReturnsAsync(newCountry);
            _groupRepositoryMock.Setup(groupRepository => groupRepository.GetByIdAsync(1)).ReturnsAsync(existingGroup);
            _groupRepositoryMock.Setup(groupRepository => groupRepository.GetByIdAsync(3)).ReturnsAsync(group);
            _teamRepositoryMock.Setup(teamRepository => teamRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Team, bool>>>()))
                .Returns(new List<Team>
                {
                    new Team { Id = 99, CountryId = 5, GroupId = 1, Country = newCountry, Group = existingGroup, Coach = new List<Coach>() }
                }.AsQueryable());
            _groupRepositoryMock.Setup(groupRepository => groupRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Group, bool>>>()))
                .Returns(new List<Group> { existingGroup }.AsQueryable());

            await Assert.ThrowsAsync<InvalidOperationException>(() => _teamService.UpdateTeam(updateTeamDto));
        }

        [Fact]
        public async Task DeleteTeam_WhenNoCoachesOrPlayers_DeletesAndSaves()
        {
            Country country = new Country { Id = 1, Name = "Spain" };
            Group group = new Group { Id = 1, Name = "A" };
            Team team = new Team { Id = 7, CountryId = 1, GroupId = 1, Country = country, Group = group, Coach = new List<Coach>() };

            SetupDeleteDependencies(
                coachRepositoryMock => coachRepositoryMock
                    .Setup(coachRepository => coachRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Coach, bool>>>()))
                    .Returns(new List<Coach>().AsQueryable()),
                playerRepositoryMock => playerRepositoryMock
                    .Setup(playerRepository => playerRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Player, bool>>>()))
                    .Returns(new List<Player>().AsQueryable()));

            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(7)).ReturnsAsync(team);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _teamService.DeleteTeam(7);

            _teamRepositoryMock.Verify(teamRepository => teamRepository.Delete(team), Times.Once);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteTeam_WhenMatchesExist_ThrowsInvalidOperationException()
        {
            Country country = new Country { Id = 1, Name = "Spain" };
            Group group = new Group { Id = 1, Name = "A" };
            Team team = new Team { Id = 7, CountryId = 1, GroupId = 1, Country = country, Group = group, Coach = new List<Coach>() };

            Mock<IRepository<MatchEntity>> matchRepositoryMock = new Mock<IRepository<MatchEntity>>();
            SetupDeleteDependencies(
                coachRepositoryMock => coachRepositoryMock
                    .Setup(coachRepository => coachRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Coach, bool>>>()))
                    .Returns(new List<Coach>().AsQueryable()),
                playerRepositoryMock => playerRepositoryMock
                    .Setup(playerRepository => playerRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Player, bool>>>()))
                    .Returns(new List<Player>().AsQueryable()),
                matchRepositoryMock: matchRepositoryMock);

            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(7)).ReturnsAsync(team);
            matchRepositoryMock
                .Setup(matchRepository => matchRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<MatchEntity, bool>>>()))
                .Returns(new List<MatchEntity>
                {
                    new MatchEntity
                    {
                        Id = 1,
                        Date = DateTime.UtcNow,
                        StadiumId = 1,
                        TeamOneId = 7,
                        TeamTwoId = 2
                    }
                }.AsQueryable());

            await Assert.ThrowsAsync<InvalidOperationException>(() => _teamService.DeleteTeam(7));
        }

        [Fact]
        public async Task DeleteTeam_WhenPlayersExist_ThrowsInvalidOperationException()
        {
            Country country = new Country { Id = 1, Name = "Spain" };
            Group group = new Group { Id = 1, Name = "A" };
            Team team = new Team { Id = 7, CountryId = 1, GroupId = 1, Country = country, Group = group, Coach = new List<Coach>() };

            Mock<IRepository<Coach>> coachRepositoryMock = new Mock<IRepository<Coach>>();
            Mock<IRepository<Player>> playerRepositoryMock = new Mock<IRepository<Player>>();
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Coach).Returns(coachRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Player).Returns(playerRepositoryMock.Object);

            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(7)).ReturnsAsync(team);
            coachRepositoryMock
                .Setup(coachRepository => coachRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Coach, bool>>>()))
                .Returns(new List<Coach>().AsQueryable());
            playerRepositoryMock
                .Setup(playerRepository => playerRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Player, bool>>>()))
                .Returns(new List<Player> { new Player { Id = 1, Name = "Player", Number = 9, TeamId = 7, PositionId = 1 } }.AsQueryable());

            await Assert.ThrowsAsync<InvalidOperationException>(() => _teamService.DeleteTeam(7));
        }

        [Fact]
        public async Task DeleteTeam_WhenCoachesExist_ThrowsInvalidOperationException()
        {
            Country country = new Country { Id = 1, Name = "Spain" };
            Group group = new Group { Id = 1, Name = "A" };
            Team team = new Team { Id = 7, CountryId = 1, GroupId = 1, Country = country, Group = group, Coach = new List<Coach>() };

            Mock<IRepository<Coach>> coachRepositoryMock = new Mock<IRepository<Coach>>();
            Mock<IRepository<Player>> playerRepositoryMock = new Mock<IRepository<Player>>();
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Coach).Returns(coachRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Player).Returns(playerRepositoryMock.Object);

            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(7)).ReturnsAsync(team);
            coachRepositoryMock
                .Setup(coachRepository => coachRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Coach, bool>>>()))
                .Returns(new List<Coach> { new Coach { Id = 1, Name = "Coach", TeamId = 7 } }.AsQueryable());
            playerRepositoryMock
                .Setup(playerRepository => playerRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Player, bool>>>()))
                .Returns(new List<Player>().AsQueryable());

            await Assert.ThrowsAsync<InvalidOperationException>(() => _teamService.DeleteTeam(7));
        }

        private void SetupDeleteDependencies(
            Action<Mock<IRepository<Coach>>>? configureCoachRepository = null,
            Action<Mock<IRepository<Player>>>? configurePlayerRepository = null,
            Mock<IRepository<MatchEntity>>? matchRepositoryMock = null)
        {
            Mock<IRepository<Coach>> coachRepositoryMock = new Mock<IRepository<Coach>>();
            Mock<IRepository<Player>> playerRepositoryMock = new Mock<IRepository<Player>>();
            Mock<IRepository<Bet>> betRepositoryMock = new Mock<IRepository<Bet>>();
            Mock<IRepository<TeamStats>> teamStatsRepositoryMock = new Mock<IRepository<TeamStats>>();
            Mock<IRepository<MatchEntity>> resolvedMatchRepositoryMock = matchRepositoryMock ?? new Mock<IRepository<MatchEntity>>();

            configureCoachRepository?.Invoke(coachRepositoryMock);
            configurePlayerRepository?.Invoke(playerRepositoryMock);

            if (matchRepositoryMock == null)
            {
                resolvedMatchRepositoryMock
                    .Setup(matchRepository => matchRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<MatchEntity, bool>>>()))
                    .Returns(new List<MatchEntity>().AsQueryable());
            }

            betRepositoryMock
                .Setup(betRepository => betRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Bet, bool>>>()))
                .Returns(new List<Bet>().AsQueryable());
            teamStatsRepositoryMock
                .Setup(teamStatsRepository => teamStatsRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<TeamStats, bool>>>()))
                .Returns(new List<TeamStats>().AsQueryable());

            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Coach).Returns(coachRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Player).Returns(playerRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Match).Returns(resolvedMatchRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Bet).Returns(betRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.TeamStats).Returns(teamStatsRepositoryMock.Object);
        }
    }
}
