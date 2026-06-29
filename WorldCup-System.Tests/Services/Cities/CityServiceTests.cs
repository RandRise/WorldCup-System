using Core.DTOs.Cities;
using Core.Services.Cities;
using Data.Entities;
using Data.Repos;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Text;

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

        [Fact]
        public async Task LoadCitiesFromCsv_WhenFileIsEmpty_ThrowsException()
        {
            Mock<IFormFile> fileMock = new Mock<IFormFile>();
            fileMock.Setup(formFile => formFile.Length).Returns(0);

            Exception exception = await Assert.ThrowsAsync<Exception>(
                () => _cityService.LoadCitiesFromCsv(fileMock.Object, 1));

            Assert.Equal("File is empty.", exception.Message);
        }

        [Fact]
        public async Task LoadCitiesFromCsv_WhenValidCsv_CreatesNewCitiesAndSaves()
        {
            string csvContent = "Name\nBerlin\nMunich\n";
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            Mock<IFormFile> fileMock = new Mock<IFormFile>();
            fileMock.Setup(formFile => formFile.Length).Returns(stream.Length);
            fileMock.Setup(formFile => formFile.FileName).Returns("cities.csv");
            fileMock.Setup(formFile => formFile.OpenReadStream()).Returns(stream);

            _cityRepositoryMock
                .Setup(cityRepository => cityRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<City, bool>>>()))
                .Returns(new List<City>().AsQueryable());

            _repositoryManagerMock
                .Setup(repositoryManager => repositoryManager.SaveAsync())
                .Returns(Task.CompletedTask);

            await _cityService.LoadCitiesFromCsv(fileMock.Object, 3);

            _cityRepositoryMock.Verify(
                cityRepository => cityRepository.Create(It.IsAny<City>()),
                Times.Exactly(2));
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }
    }
}
