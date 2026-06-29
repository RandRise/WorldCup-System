using Core.DTOs.Groups;
using Core.Services.Groups;
using Data.Entities;
using Data.Repos;
using Moq;

namespace WorldCup_System.Tests.Services.Groups
{
    public class GroupServiceTests
    {
        private readonly Mock<IRepositoryManager> _repositoryManagerMock;
        private readonly Mock<IRepository<Group>> _groupRepositoryMock;
        private readonly GroupService _groupService;

        public GroupServiceTests()
        {
            _repositoryManagerMock = new Mock<IRepositoryManager>();
            _groupRepositoryMock = new Mock<IRepository<Group>>();
            _repositoryManagerMock.Setup(repositoryManager => repositoryManager.Group).Returns(_groupRepositoryMock.Object);
            _groupService = new GroupService(_repositoryManagerMock.Object);
        }

        [Fact]
        public void GetGroups_ReturnsMappedGroupDtosWithIds()
        {
            List<Group> groups = new List<Group>
            {
                new Group { Id = 1, Name = "A", WorldCupId = 2026 },
                new Group { Id = 2, Name = "B", WorldCupId = 2026 }
            };

            _groupRepositoryMock
                .Setup(groupRepository => groupRepository.GetAllAsync())
                .Returns(groups.AsQueryable());

            List<GroupsDTO> result = _groupService.GetGroups();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, groupDto => groupDto.Id == 1 && groupDto.Name == "A" && groupDto.WorldCupId == 2026);
            Assert.Contains(result, groupDto => groupDto.Id == 2 && groupDto.Name == "B" && groupDto.WorldCupId == 2026);
        }

        [Fact]
        public async Task AddGroup_CreatesGroupAndSaves()
        {
            AddGroupsDTO addGroupsDto = new AddGroupsDTO
            {
                Name = "C",
                WorldCupId = 7
            };

            Group? capturedGroup = null;
            _groupRepositoryMock
                .Setup(groupRepository => groupRepository.Create(It.IsAny<Group>()))
                .Callback<Group>(group => capturedGroup = group);

            _repositoryManagerMock
                .Setup(repositoryManager => repositoryManager.SaveAsync())
                .Returns(Task.CompletedTask);

            await _groupService.AddGroup(addGroupsDto);

            Assert.NotNull(capturedGroup);
            Assert.Equal("C", capturedGroup.Name);
            Assert.Equal(7, capturedGroup.WorldCupId);
            _repositoryManagerMock.Verify(repositoryManager => repositoryManager.SaveAsync(), Times.Once);
        }
    }
}
