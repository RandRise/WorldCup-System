using Core.DTOs.PlayerPositions;
using Core.Services.PlayerPositions;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    public class PlayerPositionControllerTests
    {
        private readonly Mock<IPlayerPositionService> _playerPositionServiceMock;
        private readonly PlayerPositionController _playerPositionController;

        public PlayerPositionControllerTests()
        {
            _playerPositionServiceMock = new Mock<IPlayerPositionService>();
            _playerPositionController = new PlayerPositionController(_playerPositionServiceMock.Object);
        }

        [Fact]
        public void GetPlayerPositions_ReturnsPositionsFromService()
        {
            List<PlayerPositionDTO> positions = new List<PlayerPositionDTO>
            {
                new PlayerPositionDTO { Id = 1, Name = "Goalkeeper" },
                new PlayerPositionDTO { Id = 2, Name = "Defender" },
                new PlayerPositionDTO { Id = 3, Name = "Midfielder" },
                new PlayerPositionDTO { Id = 4, Name = "Forward" }
            };

            _playerPositionServiceMock
                .Setup(playerPositionService => playerPositionService.GetPlayerPositions())
                .Returns(positions);

            List<PlayerPositionDTO> result = _playerPositionController.GetPlayerPositions();

            Assert.Equal(4, result.Count);
            Assert.Contains(result, positionDto => positionDto.Name == "Forward");
            _playerPositionServiceMock.Verify(
                playerPositionService => playerPositionService.GetPlayerPositions(),
                Times.Once);
        }
    }
}
