using Core.DTOs.WorldCups;
using Core.Services.WorldCups;
using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class WorldCupController : Controller
    {
        private readonly IWorldCupService _worldCupService;
        public WorldCupController(IWorldCupService worldCupService)
        {
            _worldCupService = worldCupService;
        }
        [HttpGet]
        public List<WorldCupDTO> GetWorldCups()
        {
            var worldCups = _worldCupService.GetAllWorldCups();
            return worldCups;
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewWorldCup([FromBody] WorldCupDTO worldCupDTO)
        {
            try
            {
                await _worldCupService.CreateNewWorldCup(worldCupDTO);
                return Ok("World Cup Created Successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }
    }
}
