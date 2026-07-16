using Core.DTOs.Matches;

namespace Core.Services.Knockout
{
    public interface IKnockoutService
    {
        BracketDTO GetBracket(int worldCupId);
        Task<GenerateBracketResultDTO> GenerateBracket(GenerateBracketDTO request);
        Task TryAdvanceFromMatch(int matchId);
    }
}
