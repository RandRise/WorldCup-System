using Core.DTOs.Matches;
using Core.Services.Matches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class MatchController : Controller
    {
        private readonly IMatchService _matchService;

        public MatchController(IMatchService matchService)
        {
            _matchService = matchService;
        }

        [HttpGet]
        public List<MatchDTO> GetMatches()
        {
            return _matchService.GetMatches();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMatchById(int id)
        {
            try
            {
                MatchDetailDTO match = await _matchService.GetMatchById(id);
                return Ok(match);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [HttpGet("{worldCupId}")]
        public List<MatchDTO> GetFixturesByWorldCup(int worldCupId)
        {
            return _matchService.GetFixturesByWorldCup(worldCupId);
        }

        [HttpGet("{groupId}")]
        public List<MatchDTO> GetFixturesByGroup(int groupId)
        {
            return _matchService.GetFixturesByGroup(groupId);
        }

        [HttpGet]
        public List<MatchDTO> GetFixturesByDate([FromQuery] DateTime date)
        {
            return _matchService.GetFixturesByDate(date);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddMatch([FromBody] AddMatchDTO match)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _matchService.AddMatch(match);
                return Ok("Match added successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateMatch([FromBody] UpdateMatchDTO match)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _matchService.UpdateMatch(match);
                return Ok("Match updated successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMatch(int id)
        {
            try
            {
                await _matchService.DeleteMatch(id);
                return Ok("Match deleted successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }
    }
}
