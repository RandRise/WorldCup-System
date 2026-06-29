using Core.DTOs.PlayerPositions;

namespace Core.Services.PlayerPositions
{
    public interface IPlayerPositionService
    {
        List<PlayerPositionDTO> GetPlayerPositions();
    }
}
