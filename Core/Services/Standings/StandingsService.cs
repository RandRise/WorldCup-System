using Core.DTOs.Standings;
using Core.Services.Bets;
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

            // Only group-stage fixtures count toward standings; knockout results are separate.
            List<Match> matches = _repository.Match
                .Find(match =>
                    match.Stage == MatchStage.Group
                    && match.TeamOneId.HasValue
                    && match.TeamTwoId.HasValue
                    && teamIds.Contains(match.TeamOneId.Value)
                    && teamIds.Contains(match.TeamTwoId.Value))
                .ToList();
            List<int> matchIds = matches.Select(match => match.Id).ToList();

            List<TeamStats> stats = matchIds.Count > 0
                ? _repository.TeamStats.Find(teamStats => matchIds.Contains(teamStats.MatchId)).ToList()
                : new List<TeamStats>();
            List<int> statIds = stats.Select(teamStats => teamStats.Id).ToList();
            List<Goal> goals = statIds.Count > 0
                ? _repository.Goal.Find(goal => statIds.Contains(goal.TeamStatsId)).ToList()
                : new List<Goal>();

            // Head-to-head results among any tied cluster (points after full table calc).
            Dictionary<(int TeamA, int TeamB), (int PointsA, int PointsB, int GdA, int GdB, int GfA, int GfB)> headToHead =
                new Dictionary<(int, int), (int, int, int, int, int, int)>();

            // Goals/cards/stats are recorded after full time, not mid-match.
            // Only count matches that have reached Finished (kickoff + 90 minutes).
            DateTime now = DateTime.UtcNow;
            foreach (Match match in matches)
            {
                DateTime fullTime = match.Date.AddMinutes(BetScoringRules.MatchDurationMinutes);
                if (fullTime > now)
                {
                    continue;
                }

                int teamOneId = match.TeamOneId!.Value;
                int teamTwoId = match.TeamTwoId!.Value;

                TeamStats? oneStats = stats.FirstOrDefault(teamStats =>
                    teamStats.MatchId == match.Id && teamStats.TeamId == teamOneId);
                TeamStats? twoStats = stats.FirstOrDefault(teamStats =>
                    teamStats.MatchId == match.Id && teamStats.TeamId == teamTwoId);
                if (oneStats == null || twoStats == null)
                {
                    continue;
                }

                int oneScore = goals.Count(goal => goal.TeamStatsId == oneStats.Id);
                int twoScore = goals.Count(goal => goal.TeamStatsId == twoStats.Id);

                StandingDTO one = table[teamOneId];
                StandingDTO two = table[teamTwoId];

                one.Played++;
                two.Played++;
                one.GoalsFor += oneScore;
                one.GoalsAgainst += twoScore;
                two.GoalsFor += twoScore;
                two.GoalsAgainst += oneScore;

                int onePoints = 0;
                int twoPoints = 0;
                if (oneScore > twoScore)
                {
                    one.Won++;
                    two.Lost++;
                    one.Points += 3;
                    onePoints = 3;
                }
                else if (twoScore > oneScore)
                {
                    two.Won++;
                    one.Lost++;
                    two.Points += 3;
                    twoPoints = 3;
                }
                else
                {
                    one.Drawn++;
                    two.Drawn++;
                    one.Points += 1;
                    two.Points += 1;
                    onePoints = 1;
                    twoPoints = 1;
                }

                int keyTeamA = Math.Min(teamOneId, teamTwoId);
                int keyTeamB = Math.Max(teamOneId, teamTwoId);
                (int PointsA, int PointsB, int GdA, int GdB, int GfA, int GfB) existing =
                    headToHead.GetValueOrDefault((keyTeamA, keyTeamB));

                if (teamOneId == keyTeamA)
                {
                    existing.PointsA += onePoints;
                    existing.PointsB += twoPoints;
                    existing.GdA += oneScore - twoScore;
                    existing.GdB += twoScore - oneScore;
                    existing.GfA += oneScore;
                    existing.GfB += twoScore;
                }
                else
                {
                    existing.PointsA += twoPoints;
                    existing.PointsB += onePoints;
                    existing.GdA += twoScore - oneScore;
                    existing.GdB += oneScore - twoScore;
                    existing.GfA += twoScore;
                    existing.GfB += oneScore;
                }

                headToHead[(keyTeamA, keyTeamB)] = existing;
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

            // Within equal points / GD / GF, apply FIFA-style head-to-head among the tied cluster.
            standings = ApplyHeadToHeadTiebreakers(standings, headToHead);

            for (int index = 0; index < standings.Count; index++)
            {
                standings[index].Rank = index + 1;
            }

            return standings;
        }

        private static List<StandingDTO> ApplyHeadToHeadTiebreakers(
            List<StandingDTO> ordered,
            Dictionary<(int TeamA, int TeamB), (int PointsA, int PointsB, int GdA, int GdB, int GfA, int GfB)> headToHead)
        {
            List<StandingDTO> result = new List<StandingDTO>();
            int index = 0;
            while (index < ordered.Count)
            {
                int end = index + 1;
                while (end < ordered.Count
                    && ordered[end].Points == ordered[index].Points
                    && ordered[end].GoalDifference == ordered[index].GoalDifference
                    && ordered[end].GoalsFor == ordered[index].GoalsFor)
                {
                    end++;
                }

                List<StandingDTO> cluster = ordered.Skip(index).Take(end - index).ToList();
                if (cluster.Count > 1)
                {
                    cluster = cluster
                        .OrderByDescending(standing => standing.Points)
                        .ThenByDescending(standing => HeadToHeadPoints(standing.TeamId, cluster, headToHead))
                        .ThenByDescending(standing => HeadToHeadGoalDifference(standing.TeamId, cluster, headToHead))
                        .ThenByDescending(standing => HeadToHeadGoalsFor(standing.TeamId, cluster, headToHead))
                        .ThenBy(standing => standing.TeamName)
                        .ToList();
                }

                result.AddRange(cluster);
                index = end;
            }

            return result;
        }

        private static int HeadToHeadPoints(
            int teamId,
            List<StandingDTO> cluster,
            Dictionary<(int TeamA, int TeamB), (int PointsA, int PointsB, int GdA, int GdB, int GfA, int GfB)> headToHead)
        {
            int points = 0;
            foreach (StandingDTO other in cluster)
            {
                if (other.TeamId == teamId)
                {
                    continue;
                }

                int keyA = Math.Min(teamId, other.TeamId);
                int keyB = Math.Max(teamId, other.TeamId);
                if (!headToHead.TryGetValue((keyA, keyB), out (int PointsA, int PointsB, int GdA, int GdB, int GfA, int GfB) h2h))
                {
                    continue;
                }

                points += teamId == keyA ? h2h.PointsA : h2h.PointsB;
            }

            return points;
        }

        private static int HeadToHeadGoalDifference(
            int teamId,
            List<StandingDTO> cluster,
            Dictionary<(int TeamA, int TeamB), (int PointsA, int PointsB, int GdA, int GdB, int GfA, int GfB)> headToHead)
        {
            int gd = 0;
            foreach (StandingDTO other in cluster)
            {
                if (other.TeamId == teamId)
                {
                    continue;
                }

                int keyA = Math.Min(teamId, other.TeamId);
                int keyB = Math.Max(teamId, other.TeamId);
                if (!headToHead.TryGetValue((keyA, keyB), out (int PointsA, int PointsB, int GdA, int GdB, int GfA, int GfB) h2h))
                {
                    continue;
                }

                gd += teamId == keyA ? h2h.GdA : h2h.GdB;
            }

            return gd;
        }

        private static int HeadToHeadGoalsFor(
            int teamId,
            List<StandingDTO> cluster,
            Dictionary<(int TeamA, int TeamB), (int PointsA, int PointsB, int GdA, int GdB, int GfA, int GfB)> headToHead)
        {
            int goalsFor = 0;
            foreach (StandingDTO other in cluster)
            {
                if (other.TeamId == teamId)
                {
                    continue;
                }

                int keyA = Math.Min(teamId, other.TeamId);
                int keyB = Math.Max(teamId, other.TeamId);
                if (!headToHead.TryGetValue((keyA, keyB), out (int PointsA, int PointsB, int GdA, int GdB, int GfA, int GfB) h2h))
                {
                    continue;
                }

                goalsFor += teamId == keyA ? h2h.GfA : h2h.GfB;
            }

            return goalsFor;
        }
    }
}
