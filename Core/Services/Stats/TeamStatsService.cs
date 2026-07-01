using Core.DTOs.Stats;
using Data.Entities;
using Data.Repos;

namespace Core.Services.Stats
{
    public class TeamStatsService : ITeamStatsService
    {
        private readonly IRepositoryManager _repository;

        public TeamStatsService(IRepositoryManager repository)
        {
            _repository = repository;
        }

        public List<TeamStatsDTO> GetTeamStatsByMatch(int matchId)
        {
            List<TeamStats> stats = _repository.TeamStats.Find(teamStats => teamStats.MatchId == matchId).ToList();
            if (stats.Count == 0)
            {
                return new List<TeamStatsDTO>();
            }

            List<int> statIds = stats.Select(teamStats => teamStats.Id).ToList();
            List<Goal> goals = _repository.Goal.Find(goal => statIds.Contains(goal.TeamStatsId)).ToList();
            List<Team> teams = _repository.Team.GetAllAsync().ToList();
            List<Country> countries = _repository.Country.GetAllAsync().ToList();

            return stats.Select(teamStats => new TeamStatsDTO
            {
                TeamStatsId = teamStats.Id,
                MatchId = teamStats.MatchId,
                TeamId = teamStats.TeamId,
                TeamName = ResolveTeamName(teamStats.TeamId, teams, countries),
                Possession = teamStats.Possession,
                Shots = teamStats.Shots,
                ShotsOnTarget = teamStats.ShotsOnTarget,
                Score = goals.Count(goal => goal.TeamStatsId == teamStats.Id)
            }).ToList();
        }

        public async Task UpdateTeamStats(UpdateTeamStatsDTO teamStatsDto)
        {
            await _repository.Match.GetByIdAsync(teamStatsDto.MatchId);

            TeamStats? stats = _repository.TeamStats
                .Find(teamStats => teamStats.MatchId == teamStatsDto.MatchId && teamStats.TeamId == teamStatsDto.TeamId)
                .FirstOrDefault();
            if (stats == null)
            {
                throw new InvalidOperationException(
                    $"No team stats record exists for team {teamStatsDto.TeamId} in match {teamStatsDto.MatchId}.");
            }

            TeamStats? otherStats = _repository.TeamStats
                .Find(teamStats => teamStats.MatchId == teamStatsDto.MatchId && teamStats.TeamId != teamStatsDto.TeamId)
                .FirstOrDefault();
            int otherPossession = otherStats?.Possession ?? 0;
            if ((teamStatsDto.Possession > 0 || otherPossession > 0) &&
                teamStatsDto.Possession + otherPossession != 100)
            {
                throw new InvalidOperationException(
                    "Possession for both teams in a match must sum to 100%.");
            }

            stats.Possession = teamStatsDto.Possession;
            stats.Shots = teamStatsDto.Shots;
            stats.ShotsOnTarget = teamStatsDto.ShotsOnTarget;

            _repository.TeamStats.Update(stats);
            await _repository.SaveAsync();
        }

        private static string? ResolveTeamName(int teamId, List<Team> teams, List<Country> countries)
        {
            Team? team = teams.FirstOrDefault(existingTeam => existingTeam.Id == teamId);
            if (team == null)
            {
                return null;
            }

            return countries.FirstOrDefault(country => country.Id == team.CountryId)?.Name;
        }
    }
}
