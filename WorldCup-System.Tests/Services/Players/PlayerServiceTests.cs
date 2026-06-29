using Core.DTOs.Players;
using Core.Services.Players;
using Data.Entities;
using Data.Repos;
using Moq;

namespace WorldCup_System.Tests.Services.Players
{
    public class PlayerServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<Player>> _playerRepositoryMock;
        private readonly Mock<IRepository<Team>> _teamRepositoryMock;
        private readonly Mock<IRepository<PlayerPosition>> _playerPositionRepositoryMock;
        private readonly PlayerService _playerService;

        public PlayerServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _playerRepositoryMock = new Mock<IRepository<Player>>();
            _teamRepositoryMock = new Mock<IRepository<Team>>();
            _playerPositionRepositoryMock = new Mock<IRepository<PlayerPosition>>();
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Player).Returns(_playerRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Team).Returns(_teamRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.PlayerPosition).Returns(_playerPositionRepositoryMock.Object);
            _playerService = new PlayerService(_repositoryManagerMock.Object);
        }

        [Fact]
        public void GetPlayers_ReturnsMappedPlayerDtosWithPositionNames()
        {
            List<Player> players = new List<Player>
            {
                new Player { Id = 1, Name = "Messi", Number = 10, TeamId = 2, PositionId = 4 }
            };
            List<PlayerPosition> positions = new List<PlayerPosition>
            {
                new PlayerPosition { Id = 4, Name = "Forward" }
            };

            _playerRepositoryMock.Setup(playerRepository => playerRepository.GetAllAsync()).Returns(players.AsQueryable());
            _playerPositionRepositoryMock.Setup(positionRepository => positionRepository.GetAllAsync()).Returns(positions.AsQueryable());

            List<PlayerDTO> result = _playerService.GetPlayers();

            Assert.Single(result);
            Assert.Equal("Forward", result[0].PositionName);
        }

        [Fact]
        public void GetPlayersByTeam_ReturnsPlayersForTeam()
        {
            List<Player> players = new List<Player>
            {
                new Player { Id = 1, Name = "Messi", Number = 10, TeamId = 2, PositionId = 4 },
                new Player { Id = 2, Name = "Other", Number = 7, TeamId = 3, PositionId = 1 }
            };
            List<PlayerPosition> positions = new List<PlayerPosition>
            {
                new PlayerPosition { Id = 4, Name = "Forward" },
                new PlayerPosition { Id = 1, Name = "Goalkeeper" }
            };

            _playerRepositoryMock
                .Setup(playerRepository => playerRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Player, bool>>>()))
                .Returns(players.Where(player => player.TeamId == 2).AsQueryable());
            _playerPositionRepositoryMock.Setup(positionRepository => positionRepository.GetAllAsync()).Returns(positions.AsQueryable());

            List<PlayerDTO> result = _playerService.GetPlayersByTeam(2);

            Assert.Single(result);
            Assert.Equal("Messi", result[0].Name);
        }

        [Fact]
        public async Task AddPlayer_WhenNumberAvailable_CreatesAndSaves()
        {
            AddPlayerDTO addPlayerDto = new AddPlayerDTO { Name = "Messi", Number = 10, TeamId = 2, PositionId = 4 };
            Team team = new Team { Id = 2, CountryId = 1, GroupId = 1, Country = new Country { Id = 1, Name = "Argentina" }, Group = new Group { Id = 1, Name = "A" }, Coach = new List<Coach>() };
            PlayerPosition position = new PlayerPosition { Id = 4, Name = "Forward" };

            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(2)).ReturnsAsync(team);
            _playerPositionRepositoryMock.Setup(positionRepository => positionRepository.GetByIdAsync(4)).ReturnsAsync(position);
            _playerRepositoryMock.Setup(playerRepository => playerRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Player, bool>>>()))
                .Returns(new List<Player>().AsQueryable());

            Player? capturedPlayer = null;
            _playerRepositoryMock.Setup(playerRepository => playerRepository.Create(It.IsAny<Player>()))
                .Callback<Player>(player => capturedPlayer = player);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _playerService.AddPlayer(addPlayerDto);

            Assert.NotNull(capturedPlayer);
            Assert.Equal("Messi", capturedPlayer.Name);
            Assert.Equal(10, capturedPlayer.Number);
        }

        [Fact]
        public async Task AddPlayer_WhenNumberTaken_ThrowsInvalidOperationException()
        {
            AddPlayerDTO addPlayerDto = new AddPlayerDTO { Name = "Messi", Number = 10, TeamId = 2, PositionId = 4 };
            Team team = new Team { Id = 2, CountryId = 1, GroupId = 1, Country = new Country { Id = 1, Name = "Argentina" }, Group = new Group { Id = 1, Name = "A" }, Coach = new List<Coach>() };
            PlayerPosition position = new PlayerPosition { Id = 4, Name = "Forward" };

            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(2)).ReturnsAsync(team);
            _playerPositionRepositoryMock.Setup(positionRepository => positionRepository.GetByIdAsync(4)).ReturnsAsync(position);
            _playerRepositoryMock.Setup(playerRepository => playerRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Player, bool>>>()))
                .Returns(new List<Player> { new Player { Id = 1, Name = "Other", Number = 10, TeamId = 2, PositionId = 4 } }.AsQueryable());

            await Assert.ThrowsAsync<InvalidOperationException>(() => _playerService.AddPlayer(addPlayerDto));
        }

        [Fact]
        public async Task UpdatePlayer_WhenNumberAvailable_UpdatesAndSaves()
        {
            UpdatePlayerDTO updatePlayerDto = new UpdatePlayerDTO
            {
                Id = 1,
                Name = "Messi Updated",
                Number = 10,
                TeamId = 2,
                PositionId = 4
            };
            Player player = new Player { Id = 1, Name = "Messi", Number = 9, TeamId = 2, PositionId = 4 };
            Team team = new Team { Id = 2, CountryId = 1, GroupId = 1, Country = new Country { Id = 1, Name = "Argentina" }, Group = new Group { Id = 1, Name = "A" }, Coach = new List<Coach>() };
            PlayerPosition position = new PlayerPosition { Id = 4, Name = "Forward" };

            _playerRepositoryMock.Setup(playerRepository => playerRepository.GetByIdAsync(1)).ReturnsAsync(player);
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(2)).ReturnsAsync(team);
            _playerPositionRepositoryMock.Setup(positionRepository => positionRepository.GetByIdAsync(4)).ReturnsAsync(position);
            _playerRepositoryMock.Setup(playerRepository => playerRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Player, bool>>>()))
                .Returns(new List<Player>().AsQueryable());
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _playerService.UpdatePlayer(updatePlayerDto);

            Assert.Equal("Messi Updated", player.Name);
            Assert.Equal(10, player.Number);
            _playerRepositoryMock.Verify(playerRepository => playerRepository.Update(player), Times.Once);
        }

        [Fact]
        public async Task UpdatePlayer_WhenNumberTaken_ThrowsInvalidOperationException()
        {
            UpdatePlayerDTO updatePlayerDto = new UpdatePlayerDTO
            {
                Id = 1,
                Name = "Messi",
                Number = 10,
                TeamId = 2,
                PositionId = 4
            };
            Player player = new Player { Id = 1, Name = "Messi", Number = 9, TeamId = 2, PositionId = 4 };
            Team team = new Team { Id = 2, CountryId = 1, GroupId = 1, Country = new Country { Id = 1, Name = "Argentina" }, Group = new Group { Id = 1, Name = "A" }, Coach = new List<Coach>() };
            PlayerPosition position = new PlayerPosition { Id = 4, Name = "Forward" };

            _playerRepositoryMock.Setup(playerRepository => playerRepository.GetByIdAsync(1)).ReturnsAsync(player);
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(2)).ReturnsAsync(team);
            _playerPositionRepositoryMock.Setup(positionRepository => positionRepository.GetByIdAsync(4)).ReturnsAsync(position);
            _playerRepositoryMock.Setup(playerRepository => playerRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Player, bool>>>()))
                .Returns(new List<Player> { new Player { Id = 2, Name = "Other", Number = 10, TeamId = 2, PositionId = 4 } }.AsQueryable());

            await Assert.ThrowsAsync<InvalidOperationException>(() => _playerService.UpdatePlayer(updatePlayerDto));
        }

        [Fact]
        public async Task DeletePlayer_DeletesAndSaves()
        {
            Player player = new Player { Id = 8, Name = "Player", Number = 7, TeamId = 2, PositionId = 1 };

            _playerRepositoryMock.Setup(playerRepository => playerRepository.GetByIdAsync(8)).ReturnsAsync(player);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _playerService.DeletePlayer(8);

            _playerRepositoryMock.Verify(playerRepository => playerRepository.Delete(player), Times.Once);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }
    }
}
