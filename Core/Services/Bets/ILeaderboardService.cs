using Core.DTOs.Bets;

namespace Core.Services.Bets
{
    public interface ILeaderboardService
    {
        List<LeaderboardEntryDTO> GetLeaderboard();
    }
}
