using System.Linq.Expressions;
using System.Reflection;
using Core.DTOs.Seeding;
using Core.Services.Bets;
using Core.Services.Seeding;
using Data.Entities;
using Data.Repos;
using Moq;
using MatchEntity = Data.Entities.Match;

namespace WorldCup_System.Tests.Services.Seeding
{
    public class DemoSeedServiceTests
    {
        private static readonly DateTime WorldCup2026Year = new DateTime(2026, 6, 11, 0, 0, 0, DateTimeKind.Utc);

        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<WorldCup>> _worldCupRepositoryMock;
        private readonly Mock<IRepository<Group>> _groupRepositoryMock;
        private readonly Mock<IRepository<Team>> _teamRepositoryMock;
        private readonly Mock<IRepository<MatchEntity>> _matchRepositoryMock;
        private readonly Mock<IRepository<TeamStats>> _teamStatsRepositoryMock;
        private readonly Mock<IRepository<Goal>> _goalRepositoryMock;
        private readonly Mock<IRepository<PlayerPosition>> _playerPositionRepositoryMock;
        private readonly Mock<IBetService> _betServiceMock;
        private readonly DemoSeedService _demoSeedService;

        public DemoSeedServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _worldCupRepositoryMock = new Mock<IRepository<WorldCup>>();
            _groupRepositoryMock = new Mock<IRepository<Group>>();
            _teamRepositoryMock = new Mock<IRepository<Team>>();
            _matchRepositoryMock = new Mock<IRepository<MatchEntity>>();
            _teamStatsRepositoryMock = new Mock<IRepository<TeamStats>>();
            _goalRepositoryMock = new Mock<IRepository<Goal>>();
            _playerPositionRepositoryMock = new Mock<IRepository<PlayerPosition>>();
            _betServiceMock = new Mock<IBetService>();

            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.WorldCup).Returns(_worldCupRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Group).Returns(_groupRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Team).Returns(_teamRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Match).Returns(_matchRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.TeamStats).Returns(_teamStatsRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Goal).Returns(_goalRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.PlayerPosition).Returns(_playerPositionRepositoryMock.Object);
            _betServiceMock
                .Setup(betService => betService.ResolveBetsForMatch(It.IsAny<int>()))
                .ReturnsAsync(0);

            _demoSeedService = new DemoSeedService(_repositoryManagerMock.Object, _betServiceMock.Object);
        }

        [Theory]
        [InlineData("Core.CitiesCsvData.all_countries.csv")]
        [InlineData("Core.CitiesCsvData.WorldCup2026_host_cities.csv")]
        [InlineData("Core.StadiumCsvData.Stadiums.csv")]
        [InlineData("Core.DemoSeedData.WorldCup2026_teams.csv")]
        public void SeedCsvFiles_AreEmbeddedInCoreAssembly(string resourceName)
        {
            Assembly assembly = typeof(DemoSeedService).Assembly;
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            Assert.NotNull(stream);
            Assert.True(stream.Length > 0);
        }

        [Fact]
        public async Task SeedWorldCup2026Demo_WhenFullySeeded_ReturnsAlreadySeeded()
        {
            WorldCup worldCup = new WorldCup
            {
                Id = 9,
                Year = WorldCup2026Year,
                Groups = new List<Group>(),
            };

            string[] groupNames =
            {
                "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L",
            };
            List<Group> groups = groupNames
                .Select((string name, int index) => new Group
                {
                    Id = index + 1,
                    Name = name,
                    WorldCupId = 9,
                    Teams = new List<Team>(),
                })
                .ToList();

            List<Team> teams = new List<Team>();
            int teamId = 1;
            foreach (Group group in groups)
            {
                for (int slot = 0; slot < 4; slot++)
                {
                    teams.Add(new Team
                    {
                        Id = teamId++,
                        GroupId = group.Id,
                        CountryId = teamId,
                        Country = new Country { Id = teamId, Name = $"Country {teamId}" },
                        Group = group,
                        Coach = new List<Coach>(),
                    });
                }
            }

            _worldCupRepositoryMock
                .Setup(worldCupRepository => worldCupRepository.Find(It.IsAny<Expression<Func<WorldCup, bool>>>()))
                .Returns((Expression<Func<WorldCup, bool>> predicate) =>
                    new List<WorldCup> { worldCup }.AsQueryable().Where(predicate));

            _groupRepositoryMock
                .Setup(groupRepository => groupRepository.Find(It.IsAny<Expression<Func<Group, bool>>>()))
                .Returns((Expression<Func<Group, bool>> predicate) =>
                    groups.AsQueryable().Where(predicate));

            _teamRepositoryMock
                .Setup(teamRepository => teamRepository.Find(It.IsAny<Expression<Func<Team, bool>>>()))
                .Returns((Expression<Func<Team, bool>> predicate) =>
                    teams.AsQueryable().Where(predicate));

            // Future kickoffs so AlreadySeeded path skips goal backfill and bet resolution.
            DateTime futureKickoff = DateTime.UtcNow.AddDays(30);
            List<MatchEntity> matches = new List<MatchEntity>();
            int matchId = 1;
            foreach (Group group in groups)
            {
                List<int> groupTeamIds = teams.Where(team => team.GroupId == group.Id).Select(team => team.Id).ToList();
                for (int leftIndex = 0; leftIndex < groupTeamIds.Count; leftIndex++)
                {
                    for (int rightIndex = leftIndex + 1; rightIndex < groupTeamIds.Count; rightIndex++)
                    {
                        matches.Add(new MatchEntity
                        {
                            Id = matchId++,
                            TeamOneId = groupTeamIds[leftIndex],
                            TeamTwoId = groupTeamIds[rightIndex],
                            StadiumId = 1,
                            Date = futureKickoff,
                        });
                    }
                }
            }

            _matchRepositoryMock
                .Setup(matchRepository => matchRepository.Find(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .Returns((Expression<Func<MatchEntity, bool>> predicate) =>
                    matches.AsQueryable().Where(predicate));

            DemoSeedResultDTO result = await _demoSeedService.SeedWorldCup2026DemoAsync();

            Assert.True(result.AlreadySeeded);
            Assert.Equal(9, result.WorldCupId);
            Assert.Equal(0, result.GoalsAdded);
            Assert.Equal(0, result.BetsResolved);
            Assert.Equal("World Cup 2026 demo tournament is already seeded.", result.Message);
            _betServiceMock.Verify(betService => betService.ResolveBetsForMatch(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SeedWorldCup2026Demo_WhenDatabaseEmpty_PerformsFullSeed()
        {
            InMemorySeedStores stores = new InMemorySeedStores();
            DemoSeedService demoSeedService = CreateServiceWithInMemoryStores(stores);

            DemoSeedResultDTO result = await demoSeedService.SeedWorldCup2026DemoAsync();

            Assert.False(result.AlreadySeeded);
            Assert.True(result.WorldCupId > 0);
            Assert.Equal(12, result.GroupsAdded);
            Assert.Equal(48, result.TeamsAdded);
            Assert.True(result.CountriesAdded > 0);
            Assert.Contains(stores.Countries, country => country.Name == "Afghanistan");
            Assert.Equal(16, result.CitiesAdded);
            Assert.Equal(16, result.StadiumsAdded);
            Assert.Contains("Seeded World Cup 2026 demo", result.Message);
            Assert.Equal(72, result.MatchesAdded);
            Assert.Equal(72, stores.Matches.Count);
            Assert.True(result.GoalsAdded > 0);
            Assert.True(stores.Goals.Count > 0);
            Assert.Equal(12, stores.Groups.Count(group => group.WorldCupId == result.WorldCupId));
            Assert.Equal(48, stores.Teams.Count);
            stores.RepositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task SeedWorldCup2026Demo_WhenWorldCupExistsButIncomplete_ContinuesSeeding()
        {
            InMemorySeedStores stores = new InMemorySeedStores();
            WorldCup existingWorldCup = new WorldCup
            {
                Id = 5,
                Year = WorldCup2026Year,
                Groups = new List<Group>(),
            };
            stores.WorldCups.Add(existingWorldCup);

            stores.Groups.Add(new Group
            {
                Id = 1,
                Name = "A",
                WorldCupId = 5,
                Teams = new List<Team>(),
            });

            DemoSeedService demoSeedService = CreateServiceWithInMemoryStores(stores);

            DemoSeedResultDTO result = await demoSeedService.SeedWorldCup2026DemoAsync();

            Assert.False(result.AlreadySeeded);
            Assert.Equal(5, result.WorldCupId);
            Assert.Equal(11, result.GroupsAdded);
            Assert.Equal(48, result.TeamsAdded);
            Assert.Equal(12, stores.Groups.Count(group => group.WorldCupId == 5));
            Assert.Equal(48, stores.Teams.Count);
        }

        [Fact]
        public async Task SeedWorldCup2026Demo_WhenRunTwice_SecondInvocationReturnsAlreadySeeded()
        {
            InMemorySeedStores stores = new InMemorySeedStores();
            DemoSeedService demoSeedService = CreateServiceWithInMemoryStores(stores);

            DemoSeedResultDTO firstResult = await demoSeedService.SeedWorldCup2026DemoAsync();
            DemoSeedResultDTO secondResult = await demoSeedService.SeedWorldCup2026DemoAsync();

            Assert.False(firstResult.AlreadySeeded);
            Assert.Equal(48, firstResult.TeamsAdded);
            Assert.True(secondResult.AlreadySeeded);
            Assert.Equal(firstResult.WorldCupId, secondResult.WorldCupId);
            Assert.Equal(0, secondResult.GoalsAdded);
            Assert.Equal("World Cup 2026 demo tournament is already seeded.", secondResult.Message);
        }

        [Fact]
        public async Task SeedWorldCup2026Demo_WhenAlreadySeededButFinishedMatchesLackGoals_BackfillsGoalsAndResolvesBets()
        {
            InMemorySeedStores stores = new InMemorySeedStores();
            Mock<IBetService> betServiceMock = new Mock<IBetService>();
            betServiceMock
                .Setup(betService => betService.ResolveBetsForMatch(It.IsAny<int>()))
                .ReturnsAsync(1);

            DemoSeedService demoSeedService = CreateServiceWithInMemoryStores(stores, betServiceMock);

            DemoSeedResultDTO firstResult = await demoSeedService.SeedWorldCup2026DemoAsync();
            Assert.False(firstResult.AlreadySeeded);
            Assert.True(firstResult.GoalsAdded > 0);

            int goalsBeforeClear = stores.Goals.Count;
            stores.Goals.Clear();
            betServiceMock.Invocations.Clear();

            DemoSeedResultDTO secondResult = await demoSeedService.SeedWorldCup2026DemoAsync();

            Assert.True(secondResult.AlreadySeeded);
            Assert.True(secondResult.GoalsAdded > 0);
            Assert.Equal(stores.Goals.Count, secondResult.GoalsAdded);
            Assert.True(secondResult.BetsResolved > 0);
            Assert.Contains("backfilled", secondResult.Message);
            Assert.Equal(goalsBeforeClear, stores.Goals.Count);
            betServiceMock.Verify(
                betService => betService.ResolveBetsForMatch(It.IsAny<int>()),
                Times.AtLeastOnce);
        }

        private static DemoSeedService CreateServiceWithInMemoryStores(
            InMemorySeedStores stores,
            Mock<IBetService>? betServiceMock = null)
        {
            SetupRepository(stores.CountryRepositoryMock, stores.Countries);
            SetupRepository(stores.CityRepositoryMock, stores.Cities);
            SetupRepository(stores.StadiumRepositoryMock, stores.Stadiums);
            SetupRepository(stores.WorldCupRepositoryMock, stores.WorldCups);
            SetupRepository(stores.GroupRepositoryMock, stores.Groups);
            SetupRepository(stores.TeamRepositoryMock, stores.Teams);
            SetupRepository(stores.PlayerRepositoryMock, stores.Players);
            SetupRepository(stores.TeamStatsRepositoryMock, stores.TeamStats);
            SetupRepository(stores.GoalRepositoryMock, stores.Goals);
            SetupRepository(stores.PlayerPositionRepositoryMock, stores.PlayerPositions);

            int nextMatchId = 1;
            stores.MatchRepositoryMock
                .Setup(matchRepository => matchRepository.Find(It.IsAny<Expression<Func<MatchEntity, bool>>>()))
                .Returns((Expression<Func<MatchEntity, bool>> predicate) => stores.Matches.AsQueryable().Where(predicate));
            stores.MatchRepositoryMock
                .Setup(matchRepository => matchRepository.GetAllAsync())
                .Returns(stores.Matches.AsQueryable());
            stores.MatchRepositoryMock
                .Setup(matchRepository => matchRepository.Create(It.IsAny<MatchEntity>()))
                .Callback<MatchEntity>(match =>
                {
                    if (match.Id == 0)
                    {
                        match.Id = nextMatchId++;
                    }

                    stores.Matches.Add(match);
                    foreach (TeamStats teamStats in match.TeamStats)
                    {
                        teamStats.MatchId = match.Id;
                        if (teamStats.Id == 0)
                        {
                            teamStats.Id = stores.TeamStats.Count + 1;
                        }

                        stores.TeamStats.Add(teamStats);
                    }
                });

            stores.PlayerPositionRepositoryMock
                .Setup(playerPositionRepository => playerPositionRepository.Find(It.IsAny<Expression<Func<PlayerPosition, bool>>>()))
                .Returns((Expression<Func<PlayerPosition, bool>> predicate) =>
                    stores.PlayerPositions.AsQueryable().Where(predicate));
            if (stores.PlayerPositions.Count == 0)
            {
                stores.PlayerPositions.Add(new PlayerPosition { Id = 1, Name = "Forward" });
            }

            stores.GroupRepositoryMock
                .Setup(groupRepository => groupRepository.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int groupId) =>
                {
                    Group? group = stores.Groups.FirstOrDefault(existingGroup => existingGroup.Id == groupId);
                    if (group == null)
                    {
                        throw new KeyNotFoundException($"Group {groupId} was not found.");
                    }

                    return group;
                });

            stores.RepositoryManagerMock
                .Setup(repositoryManager => repositoryManager.SaveAsync())
                .Returns(Task.CompletedTask);

            Mock<IBetService> resolvedBetServiceMock = betServiceMock ?? new Mock<IBetService>();
            if (betServiceMock == null)
            {
                resolvedBetServiceMock
                    .Setup(betService => betService.ResolveBetsForMatch(It.IsAny<int>()))
                    .ReturnsAsync(0);
            }

            return new DemoSeedService(stores.RepositoryManagerMock.Object, resolvedBetServiceMock.Object);
        }

        private static void SetupRepository<TEntity>(Mock<IRepository<TEntity>> repositoryMock, List<TEntity> entities)
            where TEntity : class
        {
            int nextId = 1;

            repositoryMock
                .Setup(repository => repository.Find(It.IsAny<Expression<Func<TEntity, bool>>>()))
                .Returns((Expression<Func<TEntity, bool>> predicate) => entities.AsQueryable().Where(predicate));

            repositoryMock
                .Setup(repository => repository.GetAllAsync())
                .Returns(entities.AsQueryable());

            repositoryMock
                .Setup(repository => repository.Create(It.IsAny<TEntity>()))
                .Callback<TEntity>(entity =>
                {
                    AssignIdIfMissing(entity, ref nextId);
                    entities.Add(entity);
                });
        }

        private static void AssignIdIfMissing<TEntity>(TEntity entity, ref int nextId)
        {
            switch (entity)
            {
                case Country country when country.Id == 0:
                    country.Id = nextId++;
                    break;
                case City city when city.Id == 0:
                    city.Id = nextId++;
                    break;
                case Stadium stadium when stadium.Id == 0:
                    stadium.Id = nextId++;
                    break;
                case WorldCup worldCup when worldCup.Id == 0:
                    worldCup.Id = nextId++;
                    break;
                case Group group when group.Id == 0:
                    group.Id = nextId++;
                    break;
                case Team team when team.Id == 0:
                    team.Id = nextId++;
                    break;
                case MatchEntity match when match.Id == 0:
                    match.Id = nextId++;
                    break;
                case Player player when player.Id == 0:
                    player.Id = nextId++;
                    break;
                case TeamStats teamStats when teamStats.Id == 0:
                    teamStats.Id = nextId++;
                    break;
                case Goal goal when goal.Id == 0:
                    goal.Id = nextId++;
                    break;
            }
        }

        private sealed class InMemorySeedStores
        {
            public Mock<IRepositoryManager> RepositoryManagerMock { get; } = new Mock<IRepositoryManager>();
            public Mock<IRepository<Country>> CountryRepositoryMock { get; } = new Mock<IRepository<Country>>();
            public Mock<IRepository<City>> CityRepositoryMock { get; } = new Mock<IRepository<City>>();
            public Mock<IRepository<Stadium>> StadiumRepositoryMock { get; } = new Mock<IRepository<Stadium>>();
            public Mock<IRepository<WorldCup>> WorldCupRepositoryMock { get; } = new Mock<IRepository<WorldCup>>();
            public Mock<IRepository<Group>> GroupRepositoryMock { get; } = new Mock<IRepository<Group>>();
            public Mock<IRepository<Team>> TeamRepositoryMock { get; } = new Mock<IRepository<Team>>();
            public Mock<IRepository<MatchEntity>> MatchRepositoryMock { get; } = new Mock<IRepository<MatchEntity>>();
            public Mock<IRepository<Player>> PlayerRepositoryMock { get; } = new Mock<IRepository<Player>>();
            public Mock<IRepository<TeamStats>> TeamStatsRepositoryMock { get; } = new Mock<IRepository<TeamStats>>();
            public Mock<IRepository<Goal>> GoalRepositoryMock { get; } = new Mock<IRepository<Goal>>();
            public Mock<IRepository<PlayerPosition>> PlayerPositionRepositoryMock { get; } = new Mock<IRepository<PlayerPosition>>();

            public List<Country> Countries { get; } = new List<Country>();
            public List<City> Cities { get; } = new List<City>();
            public List<Stadium> Stadiums { get; } = new List<Stadium>();
            public List<WorldCup> WorldCups { get; } = new List<WorldCup>();
            public List<Group> Groups { get; } = new List<Group>();
            public List<Team> Teams { get; } = new List<Team>();
            public List<MatchEntity> Matches { get; } = new List<MatchEntity>();
            public List<Player> Players { get; } = new List<Player>();
            public List<TeamStats> TeamStats { get; } = new List<TeamStats>();
            public List<Goal> Goals { get; } = new List<Goal>();
            public List<PlayerPosition> PlayerPositions { get; } = new List<PlayerPosition>();

            public InMemorySeedStores()
            {
                RepositoryManagerMock.Setup(repositoryManager => repositoryManager.Country).Returns(CountryRepositoryMock.Object);
                RepositoryManagerMock.Setup(repositoryManager => repositoryManager.City).Returns(CityRepositoryMock.Object);
                RepositoryManagerMock.Setup(repositoryManager => repositoryManager.Stadium).Returns(StadiumRepositoryMock.Object);
                RepositoryManagerMock.Setup(repositoryManager => repositoryManager.WorldCup).Returns(WorldCupRepositoryMock.Object);
                RepositoryManagerMock.Setup(repositoryManager => repositoryManager.Group).Returns(GroupRepositoryMock.Object);
                RepositoryManagerMock.Setup(repositoryManager => repositoryManager.Team).Returns(TeamRepositoryMock.Object);
                RepositoryManagerMock.Setup(repositoryManager => repositoryManager.Match).Returns(MatchRepositoryMock.Object);
                RepositoryManagerMock.Setup(repositoryManager => repositoryManager.Player).Returns(PlayerRepositoryMock.Object);
                RepositoryManagerMock.Setup(repositoryManager => repositoryManager.TeamStats).Returns(TeamStatsRepositoryMock.Object);
                RepositoryManagerMock.Setup(repositoryManager => repositoryManager.Goal).Returns(GoalRepositoryMock.Object);
                RepositoryManagerMock.Setup(repositoryManager => repositoryManager.PlayerPosition).Returns(PlayerPositionRepositoryMock.Object);
            }
        }
    }
}
