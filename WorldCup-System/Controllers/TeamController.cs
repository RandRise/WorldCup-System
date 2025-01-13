using Core.DTOs.Teams;
using Core.Services.Teams;
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
            var teams = _teamService.GetTeams();
            return teams;
        }
        [HttpPost]
        public async Task<IActionResult> AddNewTeam([FromBody] AddTeamDto teamDto)
        {
            try
            {
                await _teamService.AddNewTeam(teamDto);
                return Ok("Team added successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
