using Core.DTOs.Matches;
using Core.Services.Matches;
using Core.Services.MatchSync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class MatchController : Controller
    {
        private readonly IMatchService _matchService;
        private readonly IMatchResultSyncService _matchResultSyncService;

        public MatchController(IMatchService matchService, IMatchResultSyncService matchResultSyncService)
        {
            _matchService = matchService;
            _matchResultSyncService = matchResultSyncService;
        }

        [HttpGet]
        public List<MatchDTO> GetMatches()
        {
            return _matchService.GetMatches();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMatchById(int id)
        {
            try
            {
                MatchDetailDTO match = await _matchService.GetMatchById(id);
                return Ok(match);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [HttpGet("{worldCupId}")]
        public List<MatchDTO> GetFixturesByWorldCup(int worldCupId)
        {
            return _matchService.GetFixturesByWorldCup(worldCupId);
        }

        [HttpGet("{groupId}")]
        public List<MatchDTO> GetFixturesByGroup(int groupId)
        {
            return _matchService.GetFixturesByGroup(groupId);
        }

        [HttpGet]
        public List<MatchDTO> GetFixturesByDate([FromQuery] DateTime date)
        {
            return _matchService.GetFixturesByDate(date);
        }

        [HttpGet]
        public async Task<IActionResult> GetLiveSnapshot([FromQuery] int worldCupId)
        {
            try
            {
                LiveSnapshotDTO snapshot = await _matchService.GetLiveSnapshot(worldCupId);
                return Ok(snapshot);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddMatch([FromBody] AddMatchDTO match)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _matchService.AddMatch(match);
                return Ok("Match added successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateMatch([FromBody] UpdateMatchDTO match)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _matchService.UpdateMatch(match);
                return Ok("Match updated successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMatch(int id)
        {
            try
            {
                await _matchService.DeleteMatch(id);
                return Ok("Match deleted successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> SetExternalMatchId([FromBody] SetExternalMatchIdDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _matchResultSyncService.SetExternalMatchId(request);
                return Ok("External match id saved.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{matchId}")]
        public async Task<IActionResult> SyncResult(int matchId, CancellationToken cancellationToken)
        {
            try
            {
                SyncMatchResultDTO result = await _matchResultSyncService.SyncResult(matchId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> SyncFinishedResults(
            [FromQuery] int worldCupId,
            CancellationToken cancellationToken)
        {
            try
            {
                SyncFinishedResultsDTO result =
                    await _matchResultSyncService.SyncFinishedResults(worldCupId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        /// <summary>
        /// Scorers-only backfill for one mapped match (FT score and bets unchanged).
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("{matchId}")]
        public async Task<IActionResult> SyncScorers(int matchId, CancellationToken cancellationToken)
        {
            try
            {
                SyncMatchResultDTO result = await _matchResultSyncService.SyncScorers(matchId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        /// <summary>
        /// Scorers-only backfill for all mapped fixtures in a World Cup.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> SyncScorersForWorldCup(
            [FromQuery] int worldCupId,
            CancellationToken cancellationToken)
        {
            try
            {
                SyncFinishedResultsDTO result =
                    await _matchResultSyncService.SyncScorersForWorldCup(worldCupId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }
    }
}
