using Core.DTOs.Stats;
using Core.Services.Stats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class TeamStatsController : Controller
    {
        private readonly ITeamStatsService _teamStatsService;

        public TeamStatsController(ITeamStatsService teamStatsService)
        {
            _teamStatsService = teamStatsService;
        }

        [HttpGet("{matchId}")]
        public List<TeamStatsDTO> GetTeamStatsByMatch(int matchId)
        {
            return _teamStatsService.GetTeamStatsByMatch(matchId);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateTeamStats([FromBody] UpdateTeamStatsDTO teamStats)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _teamStatsService.UpdateTeamStats(teamStats);
                return Ok("Team stats updated successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }
    }
}
