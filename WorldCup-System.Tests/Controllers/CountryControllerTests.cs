using Core.DTOs.Countries;
using Core.Services.Countries;
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
    }
}
