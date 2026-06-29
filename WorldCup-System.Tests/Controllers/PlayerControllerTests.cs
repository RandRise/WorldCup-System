using Core.DTOs.Players;
using Core.Services.Players;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    public class PlayerControllerTests
    {
        private readonly Mock<IPlayerService> _playerServiceMock;
        private readonly PlayerController _playerController;

        public PlayerControllerTests()
        {
            _playerServiceMock = new Mock<IPlayerService>();
            _playerController = new PlayerController(_playerServiceMock.Object);
        }

        [Fact]
        public void GetPlayers_ReturnsPlayersFromService()
        {
            List<PlayerDTO> players = new List<PlayerDTO>
            {
                new PlayerDTO { Id = 1, Name = "Messi", Number = 10, TeamId = 2, PositionId = 4, PositionName = "Forward" }
            };

            _playerServiceMock.Setup(playerService => playerService.GetPlayers()).Returns(players);

            List<PlayerDTO> result = _playerController.GetPlayers();

            Assert.Single(result);
            Assert.Equal("Messi", result[0].Name);
            _playerServiceMock.Verify(playerService => playerService.GetPlayers(), Times.Once);
        }

        [Fact]
        public void GetPlayersByTeam_ReturnsPlayersFromService()
        {
            List<PlayerDTO> players = new List<PlayerDTO>
            {
                new PlayerDTO { Id = 1, Name = "Messi", Number = 10, TeamId = 2, PositionId = 4, PositionName = "Forward" },
                new PlayerDTO { Id = 2, Name = "Di Maria", Number = 11, TeamId = 2, PositionId = 4, PositionName = "Forward" }
            };

            _playerServiceMock.Setup(playerService => playerService.GetPlayersByTeam(2)).Returns(players);

            List<PlayerDTO> result = _playerController.GetPlayersByTeam(2);

            Assert.Equal(2, result.Count);
            Assert.All(result, playerDto => Assert.Equal(2, playerDto.TeamId));
        }

        [Fact]
        public async Task AddPlayer_WhenServiceSucceeds_ReturnsOk()
        {
            AddPlayerDTO addPlayerDto = new AddPlayerDTO { Name = "Messi", Number = 10, TeamId = 2, PositionId = 4 };

            _playerServiceMock
                .Setup(playerService => playerService.AddPlayer(addPlayerDto))
                .Returns(Task.CompletedTask);

            IActionResult actionResult = await _playerController.AddPlayer(addPlayerDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Player added successfully.", okResult.Value);
        }

        [Fact]
        public async Task AddPlayer_WhenNumberTaken_ReturnsBadRequest()
        {
            AddPlayerDTO addPlayerDto = new AddPlayerDTO { Name = "Messi", Number = 10, TeamId = 2, PositionId = 4 };

            _playerServiceMock
                .Setup(playerService => playerService.AddPlayer(addPlayerDto))
                .ThrowsAsync(new InvalidOperationException("Jersey number 10 is already assigned on this team."));

            IActionResult actionResult = await _playerController.AddPlayer(addPlayerDto);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("already assigned", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task UpdatePlayer_WhenServiceSucceeds_ReturnsOk()
        {
            UpdatePlayerDTO updatePlayerDto = new UpdatePlayerDTO
            {
                Id = 1,
                Name = "Messi",
                Number = 10,
                TeamId = 2,
                PositionId = 4
            };

            _playerServiceMock
                .Setup(playerService => playerService.UpdatePlayer(updatePlayerDto))
                .Returns(Task.CompletedTask);

            IActionResult actionResult = await _playerController.UpdatePlayer(updatePlayerDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Player updated successfully.", okResult.Value);
        }

        [Fact]
        public async Task DeletePlayer_WhenServiceSucceeds_ReturnsOk()
        {
            _playerServiceMock
                .Setup(playerService => playerService.DeletePlayer(8))
                .Returns(Task.CompletedTask);

            IActionResult actionResult = await _playerController.DeletePlayer(8);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Player deleted successfully.", okResult.Value);
        }
    }
}
