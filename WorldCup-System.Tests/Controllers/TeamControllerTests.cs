using Core.DTOs.Teams;
using Core.Services.Teams;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    public class TeamControllerTests
    {
        private readonly Mock<ITeamService> _teamServiceMock;
        private readonly TeamController _teamController;

        public TeamControllerTests()
        {
            _teamServiceMock = new Mock<ITeamService>();
            _teamController = new TeamController(_teamServiceMock.Object);
        }

        [Fact]
        public void GetTeams_ReturnsTeamsFromService()
        {
            List<TeamDTO> teams = new List<TeamDTO>
            {
                new TeamDTO { Id = 1, CountryId = 10, CountryName = "Brazil", GroupId = 2, GroupName = "A" }
            };

            _teamServiceMock.Setup(teamService => teamService.GetTeams()).Returns(teams);

            List<TeamDTO> result = _teamController.GetTeams();

            Assert.Single(result);
            Assert.Equal("Brazil", result[0].CountryName);
            _teamServiceMock.Verify(teamService => teamService.GetTeams(), Times.Once);
        }

        [Fact]
        public async Task GetTeamById_WhenServiceSucceeds_ReturnsOk()
        {
            TeamDTO teamDto = new TeamDTO { Id = 3, CountryId = 5, CountryName = "France", GroupId = 1, GroupName = "B" };

            _teamServiceMock
                .Setup(teamService => teamService.GetTeamById(3))
                .ReturnsAsync(teamDto);

            IActionResult actionResult = await _teamController.GetTeamById(3);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            TeamDTO result = Assert.IsType<TeamDTO>(okResult.Value);
            Assert.Equal(3, result.Id);
            Assert.Equal("France", result.CountryName);
        }

        [Fact]
        public async Task GetTeamById_WhenTeamNotFound_ReturnsNotFound()
        {
            _teamServiceMock
                .Setup(teamService => teamService.GetTeamById(99))
                .ThrowsAsync(new KeyNotFoundException("Entity of type Team with id 99 was not found."));

            IActionResult actionResult = await _teamController.GetTeamById(99);

            NotFoundObjectResult notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
            Assert.Contains("99", notFoundResult.Value?.ToString());
        }

        [Fact]
        public async Task AddTeam_WhenServiceSucceeds_ReturnsOk()
        {
            AddTeamDTO addTeamDto = new AddTeamDTO { CountryId = 5, GroupId = 2 };

            _teamServiceMock
                .Setup(teamService => teamService.AddTeam(addTeamDto))
                .Returns(Task.CompletedTask);

            IActionResult actionResult = await _teamController.AddTeam(addTeamDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Team added successfully.", okResult.Value);
        }

        [Fact]
        public async Task AddTeam_WhenCountryAlreadyHasTeam_ReturnsBadRequest()
        {
            AddTeamDTO addTeamDto = new AddTeamDTO { CountryId = 5, GroupId = 2 };

            _teamServiceMock
                .Setup(teamService => teamService.AddTeam(addTeamDto))
                .ThrowsAsync(new InvalidOperationException("Country with ID 5 already has a team."));

            IActionResult actionResult = await _teamController.AddTeam(addTeamDto);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("already has a team", badRequestResult.Value?.ToString());
        }

        [Fact]
        public async Task UpdateTeam_WhenServiceSucceeds_ReturnsOk()
        {
            UpdateTeamDTO updateTeamDto = new UpdateTeamDTO { Id = 1, CountryId = 5, GroupId = 3 };

            _teamServiceMock
                .Setup(teamService => teamService.UpdateTeam(updateTeamDto))
                .Returns(Task.CompletedTask);

            IActionResult actionResult = await _teamController.UpdateTeam(updateTeamDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Team updated successfully.", okResult.Value);
        }

        [Fact]
        public async Task AssignTeamToGroup_WhenServiceSucceeds_ReturnsOk()
        {
            AssignTeamToGroupDTO assignDto = new AssignTeamToGroupDTO { TeamId = 7, GroupId = 4 };

            _teamServiceMock
                .Setup(teamService => teamService.AssignTeamToGroup(assignDto))
                .Returns(Task.CompletedTask);

            IActionResult actionResult = await _teamController.AssignTeamToGroup(assignDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Team assigned to group successfully.", okResult.Value);
        }

        [Fact]
        public async Task DeleteTeam_WhenServiceSucceeds_ReturnsOk()
        {
            _teamServiceMock
                .Setup(teamService => teamService.DeleteTeam(7))
                .Returns(Task.CompletedTask);

            IActionResult actionResult = await _teamController.DeleteTeam(7);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Team deleted successfully.", okResult.Value);
        }

        [Fact]
        public async Task DeleteTeam_WhenTeamHasCoaches_ReturnsBadRequest()
        {
            _teamServiceMock
                .Setup(teamService => teamService.DeleteTeam(7))
                .ThrowsAsync(new InvalidOperationException(
                    "Cannot delete team while coaches or players are still assigned. Remove them first."));

            IActionResult actionResult = await _teamController.DeleteTeam(7);

            BadRequestObjectResult badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("Cannot delete team", badRequestResult.Value?.ToString());
        }
    }
}
