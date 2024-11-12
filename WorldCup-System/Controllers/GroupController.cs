using Core.DTOs.Groups;
using Core.Services.Groups;
using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class GroupController : Controller
    {
        private readonly IGroupService _groupService;
        public GroupController(IGroupService groupService)
        {
            _groupService = groupService;
        }
        [HttpGet]
        public List<GroupsDTO> GetGroups()
        {
            var groups = _groupService.GetGroups();
            return groups;
        }
        [HttpPost]
        public async Task<IActionResult> AddGroup([FromBody] AddGroupsDTO group)
        {
            try
            {
                await _groupService.AddGroup(group);
                return Ok("Group Added Successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
