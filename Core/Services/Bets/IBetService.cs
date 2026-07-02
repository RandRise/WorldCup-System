using Core.DTOs.Bets;

namespace Core.Services.Bets
{
    public interface IBetService
    {
        Task PlaceBet(long userId, PlaceBetDTO betDto);
        List<BetDTO> GetUserBets(long userId);
        List<BetDTO> GetActiveBets(long userId);
        BetDTO? GetMyBetForMatch(long userId, int matchId);
        Task<int> ResolveBetsForMatch(int matchId);
        BettingRulesResponseDTO GetScoringRules();
    }
}
