using Core.DTOs.Goals;
using Data.Entities;
using Data.Repos;

namespace Core.Services.Goals
{
    public class GoalService : IGoalService
    {
        private readonly IRepositoryManager _repository;

        public GoalService(IRepositoryManager repository)
        {
            _repository = repository;
        }

        public List<GoalDTO> GetGoalsByMatch(int matchId)
        {
            Match? match = _repository.Match.Find(existingMatch => existingMatch.Id == matchId).FirstOrDefault();
            if (match == null)
            {
                return new List<GoalDTO>();
            }

            List<TeamStats> stats = _repository.TeamStats.Find(teamStats => teamStats.MatchId == matchId).ToList();
            List<int> statIds = stats.Select(teamStats => teamStats.Id).ToList();
            if (statIds.Count == 0)
            {
                return new List<GoalDTO>();
            }

            List<Goal> goals = _repository.Goal.Find(goal => statIds.Contains(goal.TeamStatsId)).ToList();
            List<Player> players = _repository.Player.GetAllAsync().ToList();

            return goals.Select(goal =>
            {
                TeamStats? owningStats = stats.FirstOrDefault(teamStats => teamStats.Id == goal.TeamStatsId);
                int minute = (int)Math.Floor((goal.TimeScored - match.Date).TotalMinutes);
                return new GoalDTO
                {
                    Id = goal.Id,
                    MatchId = matchId,
                    TeamId = owningStats?.TeamId ?? 0,
                    PlayerId = goal.PlayerId,
                    PlayerName = players.FirstOrDefault(player => player.Id == goal.PlayerId)?.Name,
                    Minute = minute < 0 ? 0 : minute,
                    IsOwnGoal = goal.IsOwnGoal != 0
                };
            })
            .OrderBy(goal => goal.Minute)
            .ToList();
        }

        public async Task AddGoal(AddGoalDTO goalDto)
        {
            Match match = await _repository.Match.GetByIdAsync(goalDto.MatchId);

            if (goalDto.TeamId != match.TeamOneId && goalDto.TeamId != match.TeamTwoId)
            {
                throw new InvalidOperationException("The specified team is not part of this match.");
            }

            Player player = await _repository.Player.GetByIdAsync(goalDto.PlayerId);
            int opponentId = goalDto.TeamId == match.TeamOneId ? match.TeamTwoId : match.TeamOneId;

            if (goalDto.IsOwnGoal)
            {
                if (player.TeamId != opponentId)
                {
                    throw new InvalidOperationException(
                        "An own goal must be credited to the opponent of the player who scored it.");
                }
            }
            else if (player.TeamId != goalDto.TeamId)
            {
                throw new InvalidOperationException("The scorer must belong to the team credited with the goal.");
            }

            TeamStats stats = ResolveStats(goalDto.MatchId, goalDto.TeamId);

            Goal goal = new Goal
            {
                PlayerId = goalDto.PlayerId,
                TeamStatsId = stats.Id,
                TimeScored = match.Date.AddMinutes(goalDto.Minute),
                IsOwnGoal = goalDto.IsOwnGoal ? 1 : 0
            };

            _repository.Goal.Create(goal);
            await _repository.SaveAsync();
        }

        public async Task DeleteGoal(int id)
        {
            Goal goal = await _repository.Goal.GetByIdAsync(id);
            _repository.Goal.Delete(goal);
            await _repository.SaveAsync();
        }

        private TeamStats ResolveStats(int matchId, int teamId)
        {
            TeamStats? stats = _repository.TeamStats
                .Find(teamStats => teamStats.MatchId == matchId && teamStats.TeamId == teamId)
                .FirstOrDefault();
            if (stats == null)
            {
                throw new InvalidOperationException(
                    $"No team stats record exists for team {teamId} in match {matchId}.");
            }

            return stats;
        }
    }
}
