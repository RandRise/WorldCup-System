using Core.DTOs.Goals;
using Core.Services.Goals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class GoalController : Controller
    {
        private readonly IGoalService _goalService;

        public GoalController(IGoalService goalService)
        {
            _goalService = goalService;
        }

        [HttpGet("{matchId}")]
        public List<GoalDTO> GetGoalsByMatch(int matchId)
        {
            return _goalService.GetGoalsByMatch(matchId);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddGoal([FromBody] AddGoalDTO goal)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                string? advanceWarning = await _goalService.AddGoal(goal);
                if (!string.IsNullOrWhiteSpace(advanceWarning))
                {
                    return Ok($"Goal recorded successfully. Warning: {advanceWarning}");
                }

                return Ok("Goal recorded successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGoal(int id)
        {
            try
            {
                string? advanceWarning = await _goalService.DeleteGoal(id);
                if (!string.IsNullOrWhiteSpace(advanceWarning))
                {
                    return Ok($"Goal deleted successfully. Warning: {advanceWarning}");
                }

                return Ok("Goal deleted successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }
    }
}
