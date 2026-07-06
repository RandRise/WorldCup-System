using Core.DTOs.Countries;
using Core.Services.Countries;
using Data.Entities;
using Data.Repos;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Text;

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

        [Fact]
        public async Task LoadCountriesFromExcel_WhenFileIsEmpty_ThrowsException()
        {
            Mock<IFormFile> fileMock = new Mock<IFormFile>();
            fileMock.Setup(formFile => formFile.Length).Returns(0);

            Exception exception = await Assert.ThrowsAsync<Exception>(
                () => _countryService.LoadCountriesFromExcel(fileMock.Object));

            Assert.Equal("File is empty.", exception.Message);
        }

        [Fact]
        public async Task LoadCountriesFromExcel_WhenUnsupportedExtension_ThrowsException()
        {
            Mock<IFormFile> fileMock = new Mock<IFormFile>();
            fileMock.Setup(formFile => formFile.Length).Returns(100);
            fileMock.Setup(formFile => formFile.FileName).Returns("countries.xlsx");

            Exception exception = await Assert.ThrowsAsync<Exception>(
                () => _countryService.LoadCountriesFromExcel(fileMock.Object));

            Assert.Equal("Unsupported file type.", exception.Message);
        }

        [Fact]
        public async Task LoadCountriesFromExcel_WhenFileTooLarge_ThrowsException()
        {
            Mock<IFormFile> fileMock = new Mock<IFormFile>();
            fileMock.Setup(formFile => formFile.Length).Returns(3 * 1024 * 1024);
            fileMock.Setup(formFile => formFile.FileName).Returns("countries.csv");

            Exception exception = await Assert.ThrowsAsync<Exception>(
                () => _countryService.LoadCountriesFromExcel(fileMock.Object));

            Assert.Equal("File size exceeds limit.", exception.Message);
        }

        [Fact]
        public async Task LoadCountriesFromExcel_WhenValidCsv_CreatesNewCountriesAndSaves()
        {
            string csvContent = "Id,Name\n1,Brazil\n2,France\n";
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            Mock<IFormFile> fileMock = new Mock<IFormFile>();
            fileMock.Setup(formFile => formFile.Length).Returns(stream.Length);
            fileMock.Setup(formFile => formFile.FileName).Returns("countries.csv");
            fileMock.Setup(formFile => formFile.OpenReadStream()).Returns(stream);

            _countryRepositoryMock
                .Setup(countryRepository => countryRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Country, bool>>>()))
                .Returns(new List<Country>().AsQueryable());

            _repositoryManagerMock
                .Setup(repositoryManager => repositoryManager.SaveAsync())
                .Returns(Task.CompletedTask);

            await _countryService.LoadCountriesFromExcel(fileMock.Object);

            _countryRepositoryMock.Verify(
                countryRepository => countryRepository.Create(It.IsAny<Country>()),
                Times.Exactly(2));
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task LoadCountriesFromExcel_WhenCountryAlreadyExists_SkipsDuplicate()
        {
            string csvContent = "Id,Name\n1,Brazil\n";
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

            Mock<IFormFile> fileMock = new Mock<IFormFile>();
            fileMock.Setup(formFile => formFile.Length).Returns(stream.Length);
            fileMock.Setup(formFile => formFile.FileName).Returns("countries.csv");
            fileMock.Setup(formFile => formFile.OpenReadStream()).Returns(stream);

            Country existingCountry = new Country { Id = 1, Name = "Brazil" };
            _countryRepositoryMock
                .Setup(countryRepository => countryRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Country, bool>>>()))
                .Returns(new List<Country> { existingCountry }.AsQueryable());

            _repositoryManagerMock
                .Setup(repositoryManager => repositoryManager.SaveAsync())
                .Returns(Task.CompletedTask);

            await _countryService.LoadCountriesFromExcel(fileMock.Object);

            _countryRepositoryMock.Verify(
                countryRepository => countryRepository.Create(It.IsAny<Country>()),
                Times.Never);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }
    }
}
