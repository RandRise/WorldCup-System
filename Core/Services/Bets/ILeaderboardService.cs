using Core.DTOs.Bets;

namespace Core.Services.Bets
{
    public interface ILeaderboardService
    {
        List<LeaderboardEntryDTO> GetLeaderboard(int? worldCupId = null);
        LeaderboardSummaryDTO GetMySummary(long userId, int? worldCupId = null);
    }
}
