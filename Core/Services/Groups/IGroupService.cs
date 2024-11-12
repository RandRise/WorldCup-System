using Core.DTOs.Groups;

namespace Core.Services.Groups
{
    public interface IGroupService
    {
        public List<GroupsDTO> GetGroups();
        Task AddGroup(AddGroupsDTO group);
    }
}
