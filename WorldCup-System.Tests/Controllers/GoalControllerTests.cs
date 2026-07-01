using Core.DTOs.Goals;
using Core.Services.Goals;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    public class GoalControllerTests
    {
        private readonly Mock<IGoalService> _goalServiceMock;
        private readonly GoalController _goalController;

        public GoalControllerTests()
        {
            _goalServiceMock = new Mock<IGoalService>();
            _goalController = new GoalController(_goalServiceMock.Object);
        }

        [Fact]
        public void GetGoalsByMatch_ReturnsGoalsFromService()
        {
            List<GoalDTO> goals = new List<GoalDTO>
            {
                new GoalDTO { Id = 1, MatchId = 5, TeamId = 10, PlayerId = 1, PlayerName = "Neymar", Minute = 25 }
            };

            _goalServiceMock.Setup(goalService => goalService.GetGoalsByMatch(5)).Returns(goals);

            List<GoalDTO> result = _goalController.GetGoalsByMatch(5);

            Assert.Single(result);
            Assert.Equal("Neymar", result[0].PlayerName);
            _goalServiceMock.Verify(goalService => goalService.GetGoalsByMatch(5), Times.Once);
        }

        [Fact]
        public async Task AddGoal_WhenServiceSucceeds_ReturnsOk()
        {
            AddGoalDTO addGoalDto = new AddGoalDTO { MatchId = 1, TeamId = 10, PlayerId = 1, Minute = 42 };

            _goalServiceMock.Setup(goalService => goalService.AddGoal(addGoalDto)).Returns(Task.CompletedTask);

            IActionResult actionResult = await _goalController.AddGoal(addGoalDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Goal recorded successfully.", okResult.Value);
        }

        [Fact]
        public async Task AddGoal_WhenTeamNotInMatch_ReturnsBadRequest()
        {
            AddGoalDTO addGoalDto = new AddGoalDTO { MatchId = 1, TeamId = 99, PlayerId = 1, Minute = 10 };

            _goalServiceMock
                .Setup(goalService => goalService.AddGoal(addGoalDto))
                .ThrowsAsync(new InvalidOperationException("The specified team is not part of this match."));

            IActionResult actionResult = await _goalController.AddGoal(addGoalDto);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("not part of this match", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task DeleteGoal_WhenServiceSucceeds_ReturnsOk()
        {
            _goalServiceMock.Setup(goalService => goalService.DeleteGoal(1)).Returns(Task.CompletedTask);

            IActionResult actionResult = await _goalController.DeleteGoal(1);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Goal deleted successfully.", okResult.Value);
        }

        [Fact]
        public async Task DeleteGoal_WhenGoalNotFound_ReturnsNotFound()
        {
            _goalServiceMock
                .Setup(goalService => goalService.DeleteGoal(99))
                .ThrowsAsync(new KeyNotFoundException("Entity of type Goal with id 99 was not found."));

            IActionResult actionResult = await _goalController.DeleteGoal(99);

            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
            Assert.Contains("99", notFoundResult.Value?.ToString());
        }
    }
}
