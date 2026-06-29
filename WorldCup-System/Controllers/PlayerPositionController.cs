using Core.DTOs.PlayerPositions;
using Core.Services.PlayerPositions;
using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class PlayerPositionController : Controller
    {
        private readonly IPlayerPositionService _playerPositionService;

        public PlayerPositionController(IPlayerPositionService playerPositionService)
        {
            _playerPositionService = playerPositionService;
        }

        [HttpGet]
        public List<PlayerPositionDTO> GetPlayerPositions()
        {
            return _playerPositionService.GetPlayerPositions();
        }
    }
}
