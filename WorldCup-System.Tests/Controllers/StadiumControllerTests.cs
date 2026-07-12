using Core.DTOs.Stadiums;
using Core.Services.Stadiums;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    public class StadiumControllerTests
    {
        private readonly Mock<IStadiumService> _stadiumServiceMock;
        private readonly StadiumController _stadiumController;

        public StadiumControllerTests()
        {
            _stadiumServiceMock = new Mock<IStadiumService>();
            _stadiumController = new StadiumController(_stadiumServiceMock.Object);
        }

        [Fact]
        public void GetStadiums_ReturnsStadiumsFromService()
        {
            List<StadiumDTO> stadiums = new List<StadiumDTO>
            {
                new StadiumDTO { Id = 1, Name = "Arena", CityId = 4 }
            };

            _stadiumServiceMock.Setup(stadiumService => stadiumService.GetStadiums()).Returns(stadiums);

            List<StadiumDTO> result = _stadiumController.GetStadiums();

            Assert.Single(result);
            Assert.Equal("Arena", result[0].Name);
        }

        [Fact]
        public async Task AddStadium_WhenServiceSucceeds_ReturnsOk()
        {
            StadiumDTO stadiumDto = new StadiumDTO { Id = 0, Name = "Arena", CityId = 1 };

            _stadiumServiceMock
                .Setup(stadiumService => stadiumService.AddStadium(stadiumDto))
                .Returns(Task.CompletedTask);

            IActionResult actionResult = await _stadiumController.AddStadium(stadiumDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Stadium created successfully.", okResult.Value);
        }

        [Fact]
        public async Task UpdateStadium_WhenServiceSucceeds_ReturnsOk()
        {
            UpdateStadiumDto updateStadiumDto = new UpdateStadiumDto
            {
                Id = 2,
                Name = "Updated Arena",
                CityId = 3
            };

            _stadiumServiceMock
                .Setup(stadiumService => stadiumService.UpdateStadium(updateStadiumDto))
                .Returns(Task.CompletedTask);

            IActionResult actionResult = await _stadiumController.UpdateStadium(updateStadiumDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Stadium information updated successfully.", okResult.Value);
        }
    }
}
