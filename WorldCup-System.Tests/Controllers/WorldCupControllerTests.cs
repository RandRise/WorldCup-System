using Core.DTOs.WorldCups;
using Core.Services.WorldCups;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    public class WorldCupControllerTests
    {
        private readonly Mock<IWorldCupService> _worldCupServiceMock;
        private readonly WorldCupController _worldCupController;

        public WorldCupControllerTests()
        {
            _worldCupServiceMock = new Mock<IWorldCupService>();
            _worldCupController = new WorldCupController(_worldCupServiceMock.Object);
        }

        [Fact]
        public void GetWorldCups_ReturnsWorldCupsFromService()
        {
            List<WorldCupDTO> worldCups = new List<WorldCupDTO>
            {
                new WorldCupDTO { Id = 1, Year = new DateTime(2026, 6, 11) }
            };

            _worldCupServiceMock
                .Setup(worldCupService => worldCupService.GetAllWorldCups())
                .Returns(worldCups);

            List<WorldCupDTO> result = _worldCupController.GetWorldCups();

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public async Task CreateNewWorldCup_WhenServiceSucceeds_ReturnsOk()
        {
            WorldCupDTO worldCupDto = new WorldCupDTO
            {
                Id = 0,
                Year = new DateTime(2030, 6, 1)
            };

            _worldCupServiceMock
                .Setup(worldCupService => worldCupService.CreateNewWorldCup(worldCupDto))
                .Returns(Task.CompletedTask);

            IActionResult actionResult = await _worldCupController.CreateNewWorldCup(worldCupDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("World Cup Created Successfully", okResult.Value);
        }
    }
}
