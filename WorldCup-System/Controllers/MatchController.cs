using Core.DTOs.Matches;
using Core.Services.Matches;
using Microsoft.AspNetCore.Http;
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
        public List<MatchDTO> GetMatchList()
        {
            var matchList = _matchService.GetMatchList();
            return matchList;
        }
        [HttpPost]
        public async Task<IActionResult> AddNewMatch([FromBody] AddMatchDetailsDto matchDetails)
        {
            try
            {
                await _matchService.AddNewMatch(matchDetails);
                return Ok("Match Details Added Successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
