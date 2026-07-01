using Core.DTOs.Stats;

namespace Core.Services.Stats
{
    public interface ITeamStatsService
    {
        List<TeamStatsDTO> GetTeamStatsByMatch(int matchId);
        Task UpdateTeamStats(UpdateTeamStatsDTO teamStatsDto);
    }
}
