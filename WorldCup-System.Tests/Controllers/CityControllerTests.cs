using Core.DTOs.Cities;
using Core.Services.Cities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    public class CityControllerTests
    {
        private readonly Mock<ICityService> _cityServiceMock;
        private readonly CityController _cityController;

        public CityControllerTests()
        {
            _cityServiceMock = new Mock<ICityService>();
            _cityController = new CityController(_cityServiceMock.Object);
        }

        [Fact]
        public void GetAllCities_ReturnsCitiesFromService()
        {
            List<CityDTO> cities = new List<CityDTO>
            {
                new CityDTO { Id = 1, Name = "Paris", CountryId = 3 }
            };

            _cityServiceMock.Setup(cityService => cityService.GetAllCities()).Returns(cities);

            List<CityDTO> result = _cityController.GetAllCities();

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public async Task AddCities_WhenUploadSucceeds_ReturnsOk()
        {
            Mock<IFormFile> fileMock = new Mock<IFormFile>();
            UploadCityDto uploadCityDto = new UploadCityDto
            {
                File = fileMock.Object,
                CountryId = 2
            };

            _cityServiceMock
                .Setup(cityService => cityService.LoadCitiesFromCsv(uploadCityDto.File!, uploadCityDto.CountryId))
                .Returns(Task.CompletedTask);

            IActionResult actionResult = await _cityController.AddCities(uploadCityDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("File uploaded and processed successfully.", okResult.Value);
        }

        [Fact]
        public async Task AddCities_WhenServiceThrows_ReturnsInternalServerError()
        {
            Mock<IFormFile> fileMock = new Mock<IFormFile>();
            UploadCityDto uploadCityDto = new UploadCityDto
            {
                File = fileMock.Object,
                CountryId = 2
            };

            _cityServiceMock
                .Setup(cityService => cityService.LoadCitiesFromCsv(uploadCityDto.File!, uploadCityDto.CountryId))
                .ThrowsAsync(new Exception("File is empty."));

            IActionResult actionResult = await _cityController.AddCities(uploadCityDto);

            ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.Contains("File is empty.", objectResult.Value?.ToString());
        }
    }
}
