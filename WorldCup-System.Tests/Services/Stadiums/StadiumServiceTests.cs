using Core.DTOs.Stadiums;
using Core.Services.Stadiums;
using Data.Entities;
using Data.Repos;
using Microsoft.AspNetCore.Http;
using Moq;

namespace WorldCup_System.Tests.Services.Stadiums
{
    public class StadiumServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<Stadium>> _stadiumRepositoryMock;
        private readonly Mock<IRepository<City>> _cityRepositoryMock;
        private readonly StadiumService _stadiumService;

        public StadiumServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _stadiumRepositoryMock = new Mock<IRepository<Stadium>>();
            _cityRepositoryMock = new Mock<IRepository<City>>();
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Stadium).Returns(_stadiumRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.City).Returns(_cityRepositoryMock.Object);
            _stadiumService = new StadiumService(_repositoryManagerMock.Object);
        }

        [Fact]
        public void GetStadiums_ReturnsMappedStadiumDtosWithIds()
        {
            List<Stadium> stadiums = new List<Stadium>
            {
                new Stadium { Id = 1, Name = "Arena", CityId = 7 },
                new Stadium { Id = 2, Name = "Stade", CityId = 8 }
            };

            _stadiumRepositoryMock
                .Setup(stadiumRepository => stadiumRepository.GetAllAsync())
                .Returns(stadiums.AsQueryable());

            List<StadiumDTO> result = _stadiumService.GetStadiums();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, stadiumDto => stadiumDto.Id == 1 && stadiumDto.Name == "Arena" && stadiumDto.CityId == 7);
            Assert.Contains(result, stadiumDto => stadiumDto.Id == 2 && stadiumDto.Name == "Stade" && stadiumDto.CityId == 8);
        }

        [Fact]
        public async Task AddStadium_CreatesStadiumAndSaves()
        {
            StadiumDTO stadiumDto = new StadiumDTO
            {
                Id = 0,
                Name = "Olympic Stadium",
                CityId = 4
            };

            Stadium? capturedStadium = null;
            _stadiumRepositoryMock
                .Setup(stadiumRepository => stadiumRepository.Create(It.IsAny<Stadium>()))
                .Callback<Stadium>(stadium => capturedStadium = stadium);

            _repositoryManagerMock
                .Setup(repositoryManager => repositoryManager.SaveAsync())
                .Returns(Task.CompletedTask);

            await _stadiumService.AddStadium(stadiumDto);

            Assert.NotNull(capturedStadium);
            Assert.Equal("Olympic Stadium", capturedStadium.Name);
            Assert.Equal(4, capturedStadium.CityId);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateStadium_WhenStadiumAndCityExist_UpdatesAndSaves()
        {
            Stadium existingStadium = new Stadium { Id = 3, Name = "Old Name", CityId = 1 };
            UpdateStadiumDto updateStadiumDto = new UpdateStadiumDto
            {
                Id = 3,
                Name = "New Name",
                CityId = 2
            };

            _stadiumRepositoryMock
                .Setup(stadiumRepository => stadiumRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Stadium, bool>>>()))
                .Returns(new List<Stadium> { existingStadium }.AsQueryable());

            _cityRepositoryMock
                .Setup(cityRepository => cityRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<City, bool>>>()))
                .Returns(new List<City> { new City { Id = 2, Name = "City", CountryId = 1 } }.AsQueryable());

            _repositoryManagerMock
                .Setup(repositoryManager => repositoryManager.SaveAsync())
                .Returns(Task.CompletedTask);

            await _stadiumService.UpdateStadium(updateStadiumDto);

            Assert.Equal("New Name", existingStadium.Name);
            Assert.Equal(2, existingStadium.CityId);
            _stadiumRepositoryMock.Verify(stadiumRepository => stadiumRepository.Update(existingStadium), Times.Once);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateStadium_WhenDtoIsNull_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _stadiumService.UpdateStadium(null!));
        }

        [Fact]
        public async Task UpdateStadium_WhenIdIsInvalid_ThrowsException()
        {
            UpdateStadiumDto updateStadiumDto = new UpdateStadiumDto
            {
                Id = 0,
                Name = "Name",
                CityId = 1
            };

            Exception exception = await Assert.ThrowsAsync<Exception>(
                () => _stadiumService.UpdateStadium(updateStadiumDto));

            Assert.Equal("ID cannot be less than 1", exception.Message);
        }

        [Fact]
        public async Task UpdateStadium_WhenStadiumNotFound_ThrowsKeyNotFoundException()
        {
            UpdateStadiumDto updateStadiumDto = new UpdateStadiumDto
            {
                Id = 99,
                Name = "Name",
                CityId = 1
            };

            _stadiumRepositoryMock
                .Setup(stadiumRepository => stadiumRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Stadium, bool>>>()))
                .Returns(new List<Stadium>().AsQueryable());

            KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _stadiumService.UpdateStadium(updateStadiumDto));

            Assert.Contains("Stadium with ID 99", exception.Message);
        }

        [Fact]
        public async Task UpdateStadium_WhenCityNotFound_ThrowsKeyNotFoundException()
        {
            Stadium existingStadium = new Stadium { Id = 1, Name = "Arena", CityId = 1 };
            UpdateStadiumDto updateStadiumDto = new UpdateStadiumDto
            {
                Id = 1,
                Name = "Arena",
                CityId = 404
            };

            _stadiumRepositoryMock
                .Setup(stadiumRepository => stadiumRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Stadium, bool>>>()))
                .Returns(new List<Stadium> { existingStadium }.AsQueryable());

            _cityRepositoryMock
                .Setup(cityRepository => cityRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<City, bool>>>()))
                .Returns(new List<City>().AsQueryable());

            KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _stadiumService.UpdateStadium(updateStadiumDto));

            Assert.Contains("City with ID 404", exception.Message);
        }

        [Fact]
        public async Task LoadStadiumsFromCsv_WhenCityMissing_ThrowsKeyNotFoundException()
        {
            string csvContent = "Stadium Name,City Name\nLusail Stadium,Unknown City\n";
            MemoryStream stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));
            Mock<IFormFile> fileMock = new Mock<IFormFile>();
            fileMock.Setup(file => file.Length).Returns(stream.Length);
            fileMock.Setup(file => file.FileName).Returns("stadiums.csv");
            fileMock.Setup(file => file.OpenReadStream()).Returns(stream);

            _cityRepositoryMock
                .Setup(cityRepository => cityRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<City, bool>>>()))
                .Returns(new List<City>().AsQueryable());

            KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _stadiumService.LoadStadiumsFromCsv(fileMock.Object));

            Assert.Contains("Unknown City", exception.Message);
        }

        [Fact]
        public async Task LoadStadiumsFromCsv_WhenCityExists_CreatesStadiumAndSaves()
        {
            string csvContent = "Stadium Name,City Name\nLusail Stadium,Lusail\n";
            MemoryStream stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));
            Mock<IFormFile> fileMock = new Mock<IFormFile>();
            fileMock.Setup(file => file.Length).Returns(stream.Length);
            fileMock.Setup(file => file.FileName).Returns("stadiums.csv");
            fileMock.Setup(file => file.OpenReadStream()).Returns(stream);

            _cityRepositoryMock
                .Setup(cityRepository => cityRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<City, bool>>>()))
                .Returns(new List<City> { new City { Id = 1, Name = "Lusail", CountryId = 1 } }.AsQueryable());

            _stadiumRepositoryMock
                .Setup(stadiumRepository => stadiumRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Stadium, bool>>>()))
                .Returns(new List<Stadium>().AsQueryable());

            Stadium? capturedStadium = null;
            _stadiumRepositoryMock
                .Setup(stadiumRepository => stadiumRepository.Create(It.IsAny<Stadium>()))
                .Callback<Stadium>(stadium => capturedStadium = stadium);

            _repositoryManagerMock
                .Setup(repositoryManager => repositoryManager.SaveAsync())
                .Returns(Task.CompletedTask);

            await _stadiumService.LoadStadiumsFromCsv(fileMock.Object);

            Assert.NotNull(capturedStadium);
            Assert.Equal("Lusail Stadium", capturedStadium.Name);
            Assert.Equal(1, capturedStadium.CityId);
        }

        [Fact]
        public async Task LoadStadiumsFromCsv_WhenDuplicateNamesInFile_ImportsOnlyOnce()
        {
            string csvContent = "Stadium Name,City Name\nLusail Stadium,Lusail\nLusail Stadium,Lusail\n";
            MemoryStream stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));
            Mock<IFormFile> fileMock = new Mock<IFormFile>();
            fileMock.Setup(file => file.Length).Returns(stream.Length);
            fileMock.Setup(file => file.FileName).Returns("stadiums.csv");
            fileMock.Setup(file => file.OpenReadStream()).Returns(stream);

            _cityRepositoryMock
                .Setup(cityRepository => cityRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<City, bool>>>()))
                .Returns(new List<City> { new City { Id = 1, Name = "Lusail", CountryId = 1 } }.AsQueryable());

            _stadiumRepositoryMock
                .Setup(stadiumRepository => stadiumRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Stadium, bool>>>()))
                .Returns(new List<Stadium>().AsQueryable());

            int createCount = 0;
            _stadiumRepositoryMock
                .Setup(stadiumRepository => stadiumRepository.Create(It.IsAny<Stadium>()))
                .Callback(() => createCount++);

            _repositoryManagerMock
                .Setup(repositoryManager => repositoryManager.SaveAsync())
                .Returns(Task.CompletedTask);

            await _stadiumService.LoadStadiumsFromCsv(fileMock.Object);

            Assert.Equal(1, createCount);
        }
    }
}
