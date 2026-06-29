using Core.DTOs.Players;
using Core.Services.Players;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class PlayerController : Controller
    {
        private readonly IPlayerService _playerService;

        public PlayerController(IPlayerService playerService)
        {
            _playerService = playerService;
        }

        [HttpGet]
        public List<PlayerDTO> GetPlayers()
        {
            return _playerService.GetPlayers();
        }

        [HttpGet("{teamId}")]
        public List<PlayerDTO> GetPlayersByTeam(int teamId)
        {
            return _playerService.GetPlayersByTeam(teamId);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddPlayer([FromBody] AddPlayerDTO player)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _playerService.AddPlayer(player);
                return Ok("Player added successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdatePlayer([FromBody] UpdatePlayerDTO player)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _playerService.UpdatePlayer(player);
                return Ok("Player updated successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlayer(int id)
        {
            try
            {
                await _playerService.DeletePlayer(id);
                return Ok("Player deleted successfully.");
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }
    }
}
