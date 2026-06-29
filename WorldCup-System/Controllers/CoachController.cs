using Core.DTOs.Coaches;
using Core.Services.Coaches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CoachController : Controller
    {
        private readonly ICoachService _coachService;

        public CoachController(ICoachService coachService)
        {
            _coachService = coachService;
        }

        [HttpGet]
        public List<CoachDTO> GetCoaches()
        {
            return _coachService.GetCoaches();
        }

        [HttpGet("{teamId}")]
        public List<CoachDTO> GetCoachesByTeam(int teamId)
        {
            return _coachService.GetCoachesByTeam(teamId);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddCoach([FromBody] AddCoachDTO coach)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _coachService.AddCoach(coach);
                return Ok("Coach added successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateCoach([FromBody] UpdateCoachDTO coach)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _coachService.UpdateCoach(coach);
                return Ok("Coach updated successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCoach(int id)
        {
            try
            {
                await _coachService.DeleteCoach(id);
                return Ok("Coach deleted successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }
    }
}
