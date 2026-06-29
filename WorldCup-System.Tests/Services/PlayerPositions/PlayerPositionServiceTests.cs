using Core.DTOs.PlayerPositions;
using Core.Services.PlayerPositions;
using Data.Entities;
using Data.Repos;
using Moq;

namespace WorldCup_System.Tests.Services.PlayerPositions
{
    public class PlayerPositionServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<PlayerPosition>> _playerPositionRepositoryMock;
        private readonly PlayerPositionService _playerPositionService;

        public PlayerPositionServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _playerPositionRepositoryMock = new Mock<IRepository<PlayerPosition>>();
            _repositoryManagerMock
                .Setup(repositoryManager => repositoryManager.PlayerPosition)
                .Returns(_playerPositionRepositoryMock.Object);
            _playerPositionService = new PlayerPositionService(_repositoryManagerMock.Object);
        }

        [Fact]
        public void GetPlayerPositions_ReturnsMappedPositionDtos()
        {
            List<PlayerPosition> positions = new List<PlayerPosition>
            {
                new PlayerPosition { Id = 1, Name = "Goalkeeper" },
                new PlayerPosition { Id = 4, Name = "Forward" }
            };

            _playerPositionRepositoryMock
                .Setup(positionRepository => positionRepository.GetAllAsync())
                .Returns(positions.AsQueryable());

            List<PlayerPositionDTO> result = _playerPositionService.GetPlayerPositions();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, positionDto => positionDto.Id == 1 && positionDto.Name == "Goalkeeper");
            Assert.Contains(result, positionDto => positionDto.Id == 4 && positionDto.Name == "Forward");
        }

        [Fact]
        public void GetPlayerPositions_WhenNoPositions_ReturnsEmptyList()
        {
            _playerPositionRepositoryMock
                .Setup(positionRepository => positionRepository.GetAllAsync())
                .Returns(new List<PlayerPosition>().AsQueryable());

            List<PlayerPositionDTO> result = _playerPositionService.GetPlayerPositions();

            Assert.Empty(result);
        }
    }
}
