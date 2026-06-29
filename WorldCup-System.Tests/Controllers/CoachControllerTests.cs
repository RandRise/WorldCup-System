using Core.DTOs.Coaches;
using Core.Services.Coaches;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    public class CoachControllerTests
    {
        private readonly Mock<ICoachService> _coachServiceMock;
        private readonly CoachController _coachController;

        public CoachControllerTests()
        {
            _coachServiceMock = new Mock<ICoachService>();
            _coachController = new CoachController(_coachServiceMock.Object);
        }

        [Fact]
        public void GetCoaches_ReturnsCoachesFromService()
        {
            List<CoachDTO> coaches = new List<CoachDTO>
            {
                new CoachDTO { Id = 1, Name = "Tite", TeamId = 3 }
            };

            _coachServiceMock.Setup(coachService => coachService.GetCoaches()).Returns(coaches);

            List<CoachDTO> result = _coachController.GetCoaches();

            Assert.Single(result);
            Assert.Equal("Tite", result[0].Name);
            _coachServiceMock.Verify(coachService => coachService.GetCoaches(), Times.Once);
        }

        [Fact]
        public void GetCoachesByTeam_ReturnsCoachesFromService()
        {
            List<CoachDTO> coaches = new List<CoachDTO>
            {
                new CoachDTO { Id = 1, Name = "Tite", TeamId = 5 },
                new CoachDTO { Id = 2, Name = "Assistant", TeamId = 5 }
            };

            _coachServiceMock.Setup(coachService => coachService.GetCoachesByTeam(5)).Returns(coaches);

            List<CoachDTO> result = _coachController.GetCoachesByTeam(5);

            Assert.Equal(2, result.Count);
            Assert.All(result, coachDto => Assert.Equal(5, coachDto.TeamId));
        }

        [Fact]
        public async Task AddCoach_WhenServiceSucceeds_ReturnsOk()
        {
            AddCoachDTO addCoachDto = new AddCoachDTO { Name = "Tite", TeamId = 3 };

            _coachServiceMock
                .Setup(coachService => coachService.AddCoach(addCoachDto))
                .Returns(Task.CompletedTask);

            IActionResult actionResult = await _coachController.AddCoach(addCoachDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Coach added successfully.", okResult.Value);
        }

        [Fact]
        public async Task AddCoach_WhenTeamNotFound_ReturnsNotFound()
        {
            AddCoachDTO addCoachDto = new AddCoachDTO { Name = "Tite", TeamId = 99 };

            _coachServiceMock
                .Setup(coachService => coachService.AddCoach(addCoachDto))
                .ThrowsAsync(new KeyNotFoundException("Entity of type Team with id 99 was not found."));

            IActionResult actionResult = await _coachController.AddCoach(addCoachDto);

            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
            Assert.Contains("99", notFoundResult.Value?.ToString());
        }

        [Fact]
        public async Task UpdateCoach_WhenServiceSucceeds_ReturnsOk()
        {
            UpdateCoachDTO updateCoachDto = new UpdateCoachDTO { Id = 1, Name = "Updated Coach", TeamId = 3 };

            _coachServiceMock
                .Setup(coachService => coachService.UpdateCoach(updateCoachDto))
                .Returns(Task.CompletedTask);

            IActionResult actionResult = await _coachController.UpdateCoach(updateCoachDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Coach updated successfully.", okResult.Value);
        }

        [Fact]
        public async Task DeleteCoach_WhenServiceSucceeds_ReturnsOk()
        {
            _coachServiceMock
                .Setup(coachService => coachService.DeleteCoach(4))
                .Returns(Task.CompletedTask);

            IActionResult actionResult = await _coachController.DeleteCoach(4);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Coach deleted successfully.", okResult.Value);
        }
    }
}
