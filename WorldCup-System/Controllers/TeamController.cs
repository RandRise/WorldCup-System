using Core.DTOs.Teams;
using Core.Services.Teams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class TeamController : Controller
    {
        private readonly ITeamService _teamService;

        public TeamController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        [HttpGet]
        public List<TeamDTO> GetTeams()
        {
            return _teamService.GetTeams();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTeamById(int id)
        {
            try
            {
                TeamDTO team = await _teamService.GetTeamById(id);
                return Ok(team);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddTeam([FromBody] AddTeamDTO team)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _teamService.AddTeam(team);
                return Ok("Team added successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateTeam([FromBody] UpdateTeamDTO team)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _teamService.UpdateTeam(team);
                return Ok("Team updated successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AssignTeamToGroup([FromBody] AssignTeamToGroupDTO assignDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _teamService.AssignTeamToGroup(assignDto);
                return Ok("Team assigned to group successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            try
            {
                await _teamService.DeleteTeam(id);
                return Ok("Team deleted successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }
    }
}
