using Core.DTOs.Cities;
using Core.Services.Cities;
using Data.Entities;
using Data.Repos;
using Moq;

namespace WorldCup_System.Tests.Services.Cities
{
    public class CityServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<City>> _cityRepositoryMock;
        private readonly CityService _cityService;

        public CityServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _cityRepositoryMock = new Mock<IRepository<City>>();
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.City).Returns(_cityRepositoryMock.Object);
            _cityService = new CityService(_repositoryManagerMock.Object);
        }

        [Fact]
        public void GetAllCities_ReturnsMappedCityDtosWithIds()
        {
            List<City> cities = new List<City>
            {
                new City { Id = 1, Name = "Paris", CountryId = 10 },
                new City { Id = 2, Name = "Lyon", CountryId = 10 }
            };

            _cityRepositoryMock
                .Setup(cityRepository => cityRepository.GetAllAsync())
                .Returns(cities.AsQueryable());

            List<CityDTO> result = _cityService.GetAllCities();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, cityDto => cityDto.Id == 1 && cityDto.Name == "Paris" && cityDto.CountryId == 10);
            Assert.Contains(result, cityDto => cityDto.Id == 2 && cityDto.Name == "Lyon" && cityDto.CountryId == 10);
        }

        [Fact]
        public async Task AddCities_CreatesCityAndSaves()
        {
            CityDTO cityDto = new CityDTO
            {
                Id = 0,
                Name = "Marseille",
                CountryId = 5
            };

            City? capturedCity = null;
            _cityRepositoryMock
                .Setup(cityRepository => cityRepository.Create(It.IsAny<City>()))
                .Callback<City>(city => capturedCity = city);

            _repositoryManagerMock
                .Setup(repositoryManager => repositoryManager.SaveAsync())
                .Returns(Task.CompletedTask);

            await _cityService.AddCities(cityDto);

            Assert.NotNull(capturedCity);
            Assert.Equal("Marseille", capturedCity.Name);
            Assert.Equal(5, capturedCity.CountryId);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }
    }
}
