using Core.DTOs.Matches;
using Core.Services.Knockout;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class KnockoutController : Controller
    {
        private readonly IKnockoutService _knockoutService;

        public KnockoutController(IKnockoutService knockoutService)
        {
            _knockoutService = knockoutService;
        }

        [HttpGet("{worldCupId}")]
        public IActionResult GetBracket(int worldCupId)
        {
            try
            {
                BracketDTO bracket = _knockoutService.GetBracket(worldCupId);
                return Ok(bracket);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> GenerateBracket([FromBody] GenerateBracketDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                GenerateBracketResultDTO result = await _knockoutService.GenerateBracket(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{matchId}")]
        public async Task<IActionResult> AdvanceFromMatch(int matchId)
        {
            try
            {
                await _knockoutService.TryAdvanceFromMatch(matchId);
                return Ok("Winner advanced where destination slots were open.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }
    }
}
