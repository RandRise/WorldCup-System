using Core.DTOs.Players;

namespace Core.Services.Players
{
    public interface IPlayerService
    {
        List<PlayerDTO> GetPlayers();
        List<PlayerDTO> GetPlayersByTeam(int teamId);
        Task AddPlayer(AddPlayerDTO playerDto);
        Task UpdatePlayer(UpdatePlayerDTO playerDto);
        Task DeletePlayer(int id);
    }
}
