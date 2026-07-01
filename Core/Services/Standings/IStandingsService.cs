using Core.DTOs.Standings;

namespace Core.Services.Standings
{
    public interface IStandingsService
    {
        List<StandingDTO> GetGroupStandings(int groupId);
    }
}
