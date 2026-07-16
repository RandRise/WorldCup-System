using Core.DTOs.Bets;

namespace Core.Services.Bets
{
    public interface IBetService
    {
        Task PlaceBet(long userId, PlaceBetDTO betDto);
        List<BetDTO> GetUserBets(long userId);
        List<BetDTO> GetActiveBets(long userId);
        BetDTO? GetMyBetForMatch(long userId, int matchId);
        List<BetDTO> GetMyBetsForWorldCup(long userId, int worldCupId);
        Task<ResolveBetsResultDTO> ResolveBetsForMatch(int matchId);
        Task<ResolveBetsWorldCupResultDTO> ResolveBetsForWorldCup(int worldCupId);
        Task TryAutoResolveFinishedMatch(int matchId);
        BettingRulesResponseDTO GetScoringRules();
    }
}
