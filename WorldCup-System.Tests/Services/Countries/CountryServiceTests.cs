using Core.DTOs.Countries;
using Core.Services.Countries;
using Data.Entities;
using Data.Repos;
using Moq;

namespace WorldCup_System.Tests.Services.Countries
{
    public class CountryServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<Country>> _countryRepositoryMock;
        private readonly CountryService _countryService;

        public CountryServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _countryRepositoryMock = new Mock<IRepository<Country>>();
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Country).Returns(_countryRepositoryMock.Object);
            _countryService = new CountryService(_repositoryManagerMock.Object);
        }

        [Fact]
        public void GetAllCountries_WhenNoCountries_ReturnsEmptyList()
        {
            _countryRepositoryMock
                .Setup(countryRepository => countryRepository.GetAllAsync())
                .Returns(new List<Country>().AsQueryable());

            List<CountryDTO> result = _countryService.GetAllCountries();

            Assert.Empty(result);
        }

        [Fact]
        public void GetAllCountries_ReturnsMappedCountryDtosWithIds()
        {
            List<Country> countries = new List<Country>
            {
                new Country { Id = 1, Name = "Brazil" },
                new Country { Id = 2, Name = "France" }
            };

            _countryRepositoryMock
                .Setup(countryRepository => countryRepository.GetAllAsync())
                .Returns(countries.AsQueryable());

            List<CountryDTO> result = _countryService.GetAllCountries();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, countryDto => countryDto.Id == 1 && countryDto.Name == "Brazil");
            Assert.Contains(result, countryDto => countryDto.Id == 2 && countryDto.Name == "France");
        }

        [Fact]
        public async Task AddCountries_CreatesCountryAndSaves()
        {
            CountryDTO countryDto = new CountryDTO
            {
                Id = 0,
                Name = "Spain"
            };

            Country? capturedCountry = null;
            _countryRepositoryMock
                .Setup(countryRepository => countryRepository.Create(It.IsAny<Country>()))
                .Callback<Country>(country => capturedCountry = country);

            _repositoryManagerMock
                .Setup(repositoryManager => repositoryManager.SaveAsync())
                .Returns(Task.CompletedTask);

            await _countryService.AddCountries(countryDto);

            Assert.NotNull(capturedCountry);
            Assert.Equal("Spain", capturedCountry.Name);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }
    }
}
