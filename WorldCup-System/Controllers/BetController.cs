using Core.DTOs.Bets;
using Core.Services.Bets;
using Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class BetController : Controller
    {
        private readonly IBetService _betService;
        private readonly ILeaderboardService _leaderboardService;
        private readonly UserManager<User> _userManager;

        public BetController(
            IBetService betService,
            ILeaderboardService leaderboardService,
            UserManager<User> userManager)
        {
            _betService = betService;
            _leaderboardService = leaderboardService;
            _userManager = userManager;
        }

        [HttpGet]
        public BettingRulesResponseDTO GetScoringRules()
        {
            return _betService.GetScoringRules();
        }

        [HttpGet]
        public List<LeaderboardEntryDTO> GetLeaderboard()
        {
            return _leaderboardService.GetLeaderboard();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PlaceBet([FromBody] PlaceBetDTO betDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                long userId = await ResolveCurrentUserIdAsync();
                await _betService.PlaceBet(userId, betDto);
                return Ok("Bet placed successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetMyBets()
        {
            try
            {
                long userId = await ResolveCurrentUserIdAsync();
                List<BetDTO> bets = _betService.GetUserBets(userId);
                return Ok(bets);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetMyActiveBets()
        {
            try
            {
                long userId = await ResolveCurrentUserIdAsync();
                List<BetDTO> bets = _betService.GetActiveBets(userId);
                return Ok(bets);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{matchId}")]
        public async Task<IActionResult> ResolveBetsForMatch(int matchId)
        {
            try
            {
                int resolvedCount = await _betService.ResolveBetsForMatch(matchId);
                return Ok(new { resolvedCount, message = $"{resolvedCount} bet(s) resolved." });
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        private async Task<long> ResolveCurrentUserIdAsync()
        {
            string? email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Authenticated user email claim is missing.");
            }

            User? user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                throw new KeyNotFoundException("Authenticated user was not found.");
            }

            return user.Id;
        }
    }
}
