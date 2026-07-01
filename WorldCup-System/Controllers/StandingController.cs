using Core.DTOs.Standings;
using Core.Services.Standings;
using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class StandingController : Controller
    {
        private readonly IStandingsService _standingsService;

        public StandingController(IStandingsService standingsService)
        {
            _standingsService = standingsService;
        }

        [HttpGet("{groupId}")]
        public List<StandingDTO> GetGroupStandings(int groupId)
        {
            return _standingsService.GetGroupStandings(groupId);
        }
    }
}
