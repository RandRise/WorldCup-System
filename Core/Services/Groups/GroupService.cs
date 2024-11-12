using Core.DTOs.Groups;
using Data.Entities;
using Data.Repos;

namespace Core.Services.Groups
{
    public class GroupService : IGroupService
    {
        private readonly IRepositoryManager _repository;
        public GroupService(IRepositoryManager repository)
        {
            _repository = repository;
        }
       
        public async Task AddGroup(AddGroupsDTO groupDto)
        {
            var group = new Group
            {

                WorldCupId = groupDto.WorldCupId,
                Name = groupDto.Name
            };
            _repository.Group.Create(group);
            await _repository.SaveAsync();
        }
        public List<GroupsDTO> GetGroups()
        {
            var groups = _repository.Group.GetAllAsync();
            var groupsDto = groups.Select(e => new GroupsDTO
            {
                Name = e.Name,
                WorldCupId=e.WorldCupId,
            }).ToList();
            return groupsDto;
        }
    }


}


