using Core.DTOs.Bets;
using Core.Helpers;
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
        public List<LeaderboardEntryDTO> GetLeaderboard([FromQuery] int? worldCupId = null)
        {
            return _leaderboardService.GetLeaderboard(worldCupId);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetMySummary([FromQuery] int? worldCupId = null)
        {
            try
            {
                long userId = await ResolveCurrentUserIdAsync();
                LeaderboardSummaryDTO summary = _leaderboardService.GetMySummary(userId, worldCupId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetMyBetsForWorldCup([FromQuery] int worldCupId)
        {
            try
            {
                long userId = await ResolveCurrentUserIdAsync();
                List<BetDTO> bets = _betService.GetMyBetsForWorldCup(userId, worldCupId);
                return Ok(bets);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize]
        [HttpGet("{matchId}")]
        public async Task<IActionResult> GetMyBetForMatch(int matchId)
        {
            try
            {
                long userId = await ResolveCurrentUserIdAsync();
                BetDTO? bet = _betService.GetMyBetForMatch(userId, matchId);
                return Ok(bet);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
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

        private Task<long> ResolveCurrentUserIdAsync()
        {
            return CurrentUserResolver.ResolveUserIdAsync(User, _userManager);
        }
    }
}
