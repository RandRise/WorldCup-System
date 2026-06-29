using Core.DTOs.WorldCups;
using Core.Services.WorldCups;
using Data.Entities;
using Data.Repos;
using Moq;

namespace WorldCup_System.Tests.Services.WorldCups
{
    public class WorldCupServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<WorldCup>> _worldCupRepositoryMock;
        private readonly WorldCupService _worldCupService;

        public WorldCupServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _worldCupRepositoryMock = new Mock<IRepository<WorldCup>>();
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.WorldCup).Returns(_worldCupRepositoryMock.Object);
            _worldCupService = new WorldCupService(_repositoryManagerMock.Object);
        }

        [Fact]
        public void GetAllWorldCups_ReturnsMappedWorldCupDtosWithIds()
        {
            DateTime year2022 = new DateTime(2022, 11, 20);
            DateTime year2026 = new DateTime(2026, 6, 11);
            List<WorldCup> worldCups = new List<WorldCup>
            {
                new WorldCup { Id = 1, Year = year2022 },
                new WorldCup { Id = 2, Year = year2026 }
            };

            _worldCupRepositoryMock
                .Setup(worldCupRepository => worldCupRepository.GetAllAsync())
                .Returns(worldCups.AsQueryable());

            List<WorldCupDTO> result = _worldCupService.GetAllWorldCups();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, worldCupDto => worldCupDto.Id == 1 && worldCupDto.Year == year2022);
            Assert.Contains(result, worldCupDto => worldCupDto.Id == 2 && worldCupDto.Year == year2026);
        }

        [Fact]
        public async Task CreateNewWorldCup_CreatesWorldCupAndSaves()
        {
            WorldCupDTO worldCupDto = new WorldCupDTO
            {
                Id = 0,
                Year = new DateTime(2030, 6, 1)
            };

            WorldCup? capturedWorldCup = null;
            _worldCupRepositoryMock
                .Setup(worldCupRepository => worldCupRepository.Create(It.IsAny<WorldCup>()))
                .Callback<WorldCup>(worldCup => capturedWorldCup = worldCup);

            _repositoryManagerMock
                .Setup(repositoryManager => repositoryManager.SaveAsync())
                .Returns(Task.CompletedTask);

            await _worldCupService.CreateNewWorldCup(worldCupDto);

            Assert.NotNull(capturedWorldCup);
            Assert.Equal(new DateTime(2030, 6, 1), capturedWorldCup.Year);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }
    }
}
