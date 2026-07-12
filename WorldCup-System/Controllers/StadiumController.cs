using Core.DTOs.Stadiums;
using Core.Services.Stadiums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class StadiumController : Controller
    {
        private readonly IStadiumService _stadiumService;

        public StadiumController(IStadiumService stadiumService)
        {
            _stadiumService = stadiumService;
        }

        [HttpGet]
        public List<StadiumDTO> GetStadiums()
        {
            List<StadiumDTO> stadiums = _stadiumService.GetStadiums();
            return stadiums;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddStadium([FromBody] StadiumDTO stadium)
        {
            try
            {
                await _stadiumService.AddStadium(stadium);
                return Ok("Stadium created successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateStadium([FromBody] UpdateStadiumDto stadium)
        {
            try
            {
                await _stadiumService.UpdateStadium(stadium);
                return Ok("Stadium information updated successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }
    }
}
