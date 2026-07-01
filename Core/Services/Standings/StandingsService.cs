using Core.DTOs.Standings;
using Data.Entities;
using Data.Repos;

namespace Core.Services.Standings
{
    public class StandingsService : IStandingsService
    {
        private readonly IRepositoryManager _repository;

        public StandingsService(IRepositoryManager repository)
        {
            _repository = repository;
        }

        public List<StandingDTO> GetGroupStandings(int groupId)
        {
            List<Team> teams = _repository.Team.Find(team => team.GroupId == groupId).ToList();
            if (teams.Count == 0)
            {
                return new List<StandingDTO>();
            }

            List<int> teamIds = teams.Select(team => team.Id).ToList();
            List<Country> countries = _repository.Country.GetAllAsync().ToList();

            Dictionary<int, StandingDTO> table = teams.ToDictionary(
                team => team.Id,
                team => new StandingDTO
                {
                    TeamId = team.Id,
                    TeamName = countries.FirstOrDefault(country => country.Id == team.CountryId)?.Name
                });

            List<Match> matches = _repository.Match
                .Find(match => teamIds.Contains(match.TeamOneId) && teamIds.Contains(match.TeamTwoId))
                .ToList();
            List<int> matchIds = matches.Select(match => match.Id).ToList();

            List<TeamStats> stats = matchIds.Count > 0
                ? _repository.TeamStats.Find(teamStats => matchIds.Contains(teamStats.MatchId)).ToList()
                : new List<TeamStats>();
            List<int> statIds = stats.Select(teamStats => teamStats.Id).ToList();
            List<Goal> goals = statIds.Count > 0
                ? _repository.Goal.Find(goal => statIds.Contains(goal.TeamStatsId)).ToList()
                : new List<Goal>();

            DateTime now = DateTime.UtcNow;
            foreach (Match match in matches)
            {
                if (match.Date > now)
                {
                    continue;
                }

                TeamStats? oneStats = stats.FirstOrDefault(teamStats =>
                    teamStats.MatchId == match.Id && teamStats.TeamId == match.TeamOneId);
                TeamStats? twoStats = stats.FirstOrDefault(teamStats =>
                    teamStats.MatchId == match.Id && teamStats.TeamId == match.TeamTwoId);
                if (oneStats == null || twoStats == null)
                {
                    continue;
                }

                int oneScore = goals.Count(goal => goal.TeamStatsId == oneStats.Id);
                int twoScore = goals.Count(goal => goal.TeamStatsId == twoStats.Id);

                StandingDTO one = table[match.TeamOneId];
                StandingDTO two = table[match.TeamTwoId];

                one.Played++;
                two.Played++;
                one.GoalsFor += oneScore;
                one.GoalsAgainst += twoScore;
                two.GoalsFor += twoScore;
                two.GoalsAgainst += oneScore;

                if (oneScore > twoScore)
                {
                    one.Won++;
                    two.Lost++;
                    one.Points += 3;
                }
                else if (twoScore > oneScore)
                {
                    two.Won++;
                    one.Lost++;
                    two.Points += 3;
                }
                else
                {
                    one.Drawn++;
                    two.Drawn++;
                    one.Points += 1;
                    two.Points += 1;
                }
            }

            foreach (StandingDTO standing in table.Values)
            {
                standing.GoalDifference = standing.GoalsFor - standing.GoalsAgainst;
            }

            List<StandingDTO> standings = table.Values
                .OrderByDescending(standing => standing.Points)
                .ThenByDescending(standing => standing.GoalDifference)
                .ThenByDescending(standing => standing.GoalsFor)
                .ThenBy(standing => standing.TeamName)
                .ToList();

            for (int index = 0; index < standings.Count; index++)
            {
                standings[index].Rank = index + 1;
            }

            return standings;
        }
    }
}
