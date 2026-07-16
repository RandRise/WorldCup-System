using Core.DTOs.Matches;

namespace Core.Services.Matches
{
    public interface IMatchService
    {
        List<MatchDTO> GetMatches();
        Task<MatchDetailDTO> GetMatchById(int id);
        Task AddMatch(AddMatchDTO matchDto);
        Task UpdateMatch(UpdateMatchDTO matchDto);
        Task DeleteMatch(int id);
        List<MatchDTO> GetFixturesByWorldCup(int worldCupId);
        List<MatchDTO> GetFixturesByGroup(int groupId);
        List<MatchDTO> GetFixturesByDate(DateTime date);
        Task<LiveSnapshotDTO> GetLiveSnapshot(int worldCupId);
    }
}
