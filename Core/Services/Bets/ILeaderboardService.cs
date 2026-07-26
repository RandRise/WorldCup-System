using Core.DTOs.Bets;

namespace Core.Services.Bets
{
    public interface ILeaderboardService
    {
        /// <summary>
        /// Company-scoped rankings. Pass the caller's <c>User.CompanyId</c> from the DB
        /// (never a client-supplied company id). Null company → empty list.
        /// </summary>
        List<LeaderboardEntryDTO> GetLeaderboard(long? companyId, int? worldCupId = null);

        LeaderboardSummaryDTO GetMySummary(long userId, int? worldCupId = null);
    }
}
