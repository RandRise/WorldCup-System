using Core.DTOs.Coaches;
using Core.Services.Coaches;
using Data.Entities;
using Data.Repos;
using Moq;

namespace WorldCup_System.Tests.Services.Coaches
{
    public class CoachServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<Coach>> _coachRepositoryMock;
        private readonly Mock<IRepository<Team>> _teamRepositoryMock;
        private readonly CoachService _coachService;

        public CoachServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _coachRepositoryMock = new Mock<IRepository<Coach>>();
            _teamRepositoryMock = new Mock<IRepository<Team>>();
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Coach).Returns(_coachRepositoryMock.Object);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Team).Returns(_teamRepositoryMock.Object);
            _coachService = new CoachService(_repositoryManagerMock.Object);
        }

        [Fact]
        public void GetCoaches_ReturnsAllCoaches()
        {
            List<Coach> coaches = new List<Coach>
            {
                new Coach { Id = 1, Name = "Coach A", TeamId = 5 },
                new Coach { Id = 2, Name = "Coach B", TeamId = 8 }
            };

            _coachRepositoryMock
                .Setup(coachRepository => coachRepository.GetAllAsync())
                .Returns(coaches.AsQueryable());

            List<CoachDTO> result = _coachService.GetCoaches();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, coachDto => coachDto.Name == "Coach A" && coachDto.TeamId == 5);
        }

        [Fact]
        public void GetCoachesByTeam_ReturnsCoachesForTeam()
        {
            List<Coach> coaches = new List<Coach>
            {
                new Coach { Id = 1, Name = "Coach A", TeamId = 5 },
                new Coach { Id = 2, Name = "Coach B", TeamId = 5 }
            };

            _coachRepositoryMock
                .Setup(coachRepository => coachRepository.Find(It.IsAny<System.Linq.Expressions.Expression<Func<Coach, bool>>>()))
                .Returns(coaches.AsQueryable());

            List<CoachDTO> result = _coachService.GetCoachesByTeam(5);

            Assert.Equal(2, result.Count);
            Assert.All(result, coachDto => Assert.Equal(5, coachDto.TeamId));
        }

        [Fact]
        public async Task AddCoach_WhenTeamExists_CreatesAndSaves()
        {
            AddCoachDTO addCoachDto = new AddCoachDTO { Name = "Tite", TeamId = 3 };
            Team team = new Team { Id = 3, CountryId = 1, GroupId = 1, Country = new Country { Id = 1, Name = "Brazil" }, Group = new Group { Id = 1, Name = "A" }, Coach = new List<Coach>() };

            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(3)).ReturnsAsync(team);

            Coach? capturedCoach = null;
            _coachRepositoryMock.Setup(coachRepository => coachRepository.Create(It.IsAny<Coach>()))
                .Callback<Coach>(coach => capturedCoach = coach);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _coachService.AddCoach(addCoachDto);

            Assert.NotNull(capturedCoach);
            Assert.Equal("Tite", capturedCoach.Name);
            Assert.Equal(3, capturedCoach.TeamId);
        }

        [Fact]
        public async Task AddCoach_WhenTeamNotFound_ThrowsKeyNotFoundException()
        {
            AddCoachDTO addCoachDto = new AddCoachDTO { Name = "Tite", TeamId = 99 };

            _teamRepositoryMock
                .Setup(teamRepository => teamRepository.GetByIdAsync(99))
                .ThrowsAsync(new KeyNotFoundException("Entity of type Team with id 99 was not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _coachService.AddCoach(addCoachDto));
        }

        [Fact]
        public async Task UpdateCoach_UpdatesAndSaves()
        {
            UpdateCoachDTO updateCoachDto = new UpdateCoachDTO { Id = 1, Name = "Updated Coach", TeamId = 3 };
            Coach coach = new Coach { Id = 1, Name = "Old Coach", TeamId = 2 };
            Team team = new Team { Id = 3, CountryId = 1, GroupId = 1, Country = new Country { Id = 1, Name = "Brazil" }, Group = new Group { Id = 1, Name = "A" }, Coach = new List<Coach>() };

            _coachRepositoryMock.Setup(coachRepository => coachRepository.GetByIdAsync(1)).ReturnsAsync(coach);
            _teamRepositoryMock.Setup(teamRepository => teamRepository.GetByIdAsync(3)).ReturnsAsync(team);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _coachService.UpdateCoach(updateCoachDto);

            Assert.Equal("Updated Coach", coach.Name);
            Assert.Equal(3, coach.TeamId);
            _coachRepositoryMock.Verify(coachRepository => coachRepository.Update(coach), Times.Once);
        }

        [Fact]
        public async Task DeleteCoach_DeletesAndSaves()
        {
            Coach coach = new Coach { Id = 4, Name = "Coach", TeamId = 2 };

            _coachRepositoryMock.Setup(coachRepository => coachRepository.GetByIdAsync(4)).ReturnsAsync(coach);
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.SaveAsync()).Returns(Task.CompletedTask);

            await _coachService.DeleteCoach(4);

            _coachRepositoryMock.Verify(coachRepository => coachRepository.Delete(coach), Times.Once);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }
    }
}
