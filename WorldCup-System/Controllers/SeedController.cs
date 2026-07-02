using Core.DTOs.Seeding;
using Core.Services.Seeding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class SeedController : Controller
    {
        private readonly IDemoSeedService _demoSeedService;

        public SeedController(IDemoSeedService demoSeedService)
        {
            _demoSeedService = demoSeedService;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> LoadWorldCup2026Demo()
        {
            try
            {
                DemoSeedResultDTO result = await _demoSeedService.SeedWorldCup2026DemoAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }
    }
}
