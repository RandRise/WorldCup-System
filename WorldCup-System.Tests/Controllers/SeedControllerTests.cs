using Core.DTOs.Seeding;
using Core.Services.Seeding;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    public class SeedControllerTests
    {
        private readonly Mock<IDemoSeedService> _demoSeedServiceMock;
        private readonly SeedController _seedController;

        public SeedControllerTests()
        {
            _demoSeedServiceMock = new Mock<IDemoSeedService>();
            _seedController = new SeedController(_demoSeedServiceMock.Object);
        }

        [Fact]
        public async Task LoadWorldCup2026Demo_WhenServiceSucceeds_ReturnsOkWithResult()
        {
            DemoSeedResultDTO seedResult = new DemoSeedResultDTO
            {
                CountriesAdded = 195,
                CitiesAdded = 16,
                StadiumsAdded = 16,
                WorldCupId = 1,
                GroupsAdded = 12,
                TeamsAdded = 48,
                AlreadySeeded = false,
                Message = "Seeded World Cup 2026 demo: 195 countries, 16 cities, 16 stadiums, 48 teams.",
            };

            _demoSeedServiceMock
                .Setup(demoSeedService => demoSeedService.SeedWorldCup2026DemoAsync())
                .ReturnsAsync(seedResult);

            IActionResult actionResult = await _seedController.LoadWorldCup2026Demo();

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            DemoSeedResultDTO result = Assert.IsType<DemoSeedResultDTO>(okResult.Value);
            Assert.Equal(1, result.WorldCupId);
            Assert.Equal(48, result.TeamsAdded);
            Assert.False(result.AlreadySeeded);
            _demoSeedServiceMock.Verify(demoSeedService => demoSeedService.SeedWorldCup2026DemoAsync(), Times.Once);
        }

        [Fact]
        public async Task LoadWorldCup2026Demo_WhenAlreadySeeded_ReturnsOkWithAlreadySeededFlag()
        {
            DemoSeedResultDTO seedResult = new DemoSeedResultDTO
            {
                WorldCupId = 9,
                AlreadySeeded = true,
                Message = "World Cup 2026 demo tournament is already seeded.",
            };

            _demoSeedServiceMock
                .Setup(demoSeedService => demoSeedService.SeedWorldCup2026DemoAsync())
                .ReturnsAsync(seedResult);

            IActionResult actionResult = await _seedController.LoadWorldCup2026Demo();

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            DemoSeedResultDTO result = Assert.IsType<DemoSeedResultDTO>(okResult.Value);
            Assert.True(result.AlreadySeeded);
            Assert.Equal(9, result.WorldCupId);
        }

        [Fact]
        public async Task LoadWorldCup2026Demo_WhenServiceThrowsKeyNotFound_ReturnsNotFound()
        {
            _demoSeedServiceMock
                .Setup(demoSeedService => demoSeedService.SeedWorldCup2026DemoAsync())
                .ThrowsAsync(new KeyNotFoundException("City 'Toronto' was not found."));

            IActionResult actionResult = await _seedController.LoadWorldCup2026Demo();

            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
            Assert.Contains("Toronto", notFoundResult.Value?.ToString());
        }

        [Fact]
        public async Task LoadWorldCup2026Demo_WhenServiceThrowsInvalidOperation_ReturnsBadRequest()
        {
            _demoSeedServiceMock
                .Setup(demoSeedService => demoSeedService.SeedWorldCup2026DemoAsync())
                .ThrowsAsync(new InvalidOperationException("Host country 'Canada' was not found after importing countries."));

            IActionResult actionResult = await _seedController.LoadWorldCup2026Demo();

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("Canada", badRequestResult.Value?.ToString());
        }
    }
}
