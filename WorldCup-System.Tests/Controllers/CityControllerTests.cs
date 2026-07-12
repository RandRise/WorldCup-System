using Core.DTOs.Cities;
using Core.Services.Cities;
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
    }
}
