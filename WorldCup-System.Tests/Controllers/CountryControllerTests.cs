using Core.DTOs.Countries;
using Core.Services.Countries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    public class CountryControllerTests
    {
        private readonly Mock<ICountryService> _countryServiceMock;
        private readonly CountryController _countryController;

        public CountryControllerTests()
        {
            _countryServiceMock = new Mock<ICountryService>();
            _countryController = new CountryController(_countryServiceMock.Object);
        }

        [Fact]
        public void GetAllCountries_ReturnsCountriesFromService()
        {
            List<CountryDTO> countries = new List<CountryDTO>
            {
                new CountryDTO { Id = 1, Name = "Brazil" },
                new CountryDTO { Id = 2, Name = "France" }
            };

            _countryServiceMock.Setup(countryService => countryService.GetAllCountries()).Returns(countries);

            List<CountryDTO> result = _countryController.GetAllCountries();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, countryDto => countryDto.Id == 1 && countryDto.Name == "Brazil");
            Assert.Contains(result, countryDto => countryDto.Id == 2 && countryDto.Name == "France");
        }

        [Fact]
        public async Task AddCountries_WhenUploadSucceeds_ReturnsOk()
        {
            Mock<IFormFile> fileMock = new Mock<IFormFile>();
            UploadCountryDto uploadCountryDto = new UploadCountryDto
            {
                File = fileMock.Object
            };

            _countryServiceMock
                .Setup(countryService => countryService.LoadCountriesFromExcel(uploadCountryDto.File!))
                .Returns(Task.CompletedTask);

            IActionResult actionResult = await _countryController.AddCountries(uploadCountryDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("File uploaded and processed successfully.", okResult.Value);
        }

        [Fact]
        public async Task AddCountries_WhenServiceThrows_ReturnsInternalServerError()
        {
            Mock<IFormFile> fileMock = new Mock<IFormFile>();
            UploadCountryDto uploadCountryDto = new UploadCountryDto
            {
                File = fileMock.Object
            };

            _countryServiceMock
                .Setup(countryService => countryService.LoadCountriesFromExcel(uploadCountryDto.File!))
                .ThrowsAsync(new Exception("File is empty."));

            IActionResult actionResult = await _countryController.AddCountries(uploadCountryDto);

            ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.Contains("File is empty.", objectResult.Value?.ToString());
        }
    }
}
