using Core.DTOs.Matches;

namespace Core.Services.Matches
{
    public interface IMatchService
    {
        public List<MatchDTO> GetMatchList();
        Task AddNewMatch(AddMatchDetailsDto match);
    }
}
