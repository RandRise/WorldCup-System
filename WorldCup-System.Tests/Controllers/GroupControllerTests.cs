using Core.DTOs.Groups;
using Core.Services.Groups;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    public class GroupControllerTests
    {
        private readonly Mock<IGroupService> _groupServiceMock;
        private readonly GroupController _groupController;

        public GroupControllerTests()
        {
            _groupServiceMock = new Mock<IGroupService>();
            _groupController = new GroupController(_groupServiceMock.Object);
        }

        [Fact]
        public void GetGroups_ReturnsGroupsFromService()
        {
            List<GroupsDTO> groups = new List<GroupsDTO>
            {
                new GroupsDTO { Id = 1, Name = "A", WorldCupId = 2026 }
            };

            _groupServiceMock.Setup(groupService => groupService.GetGroups()).Returns(groups);

            List<GroupsDTO> result = _groupController.GetGroups();

            Assert.Single(result);
            Assert.Equal("A", result[0].Name);
        }

        [Fact]
        public async Task AddGroup_WhenServiceSucceeds_ReturnsOk()
        {
            AddGroupsDTO addGroupsDto = new AddGroupsDTO { Name = "B", WorldCupId = 1 };

            _groupServiceMock
                .Setup(groupService => groupService.AddGroup(addGroupsDto))
                .Returns(Task.CompletedTask);

            IActionResult actionResult = await _groupController.AddGroup(addGroupsDto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal("Group Added Successfully", okResult.Value);
        }
    }
}
