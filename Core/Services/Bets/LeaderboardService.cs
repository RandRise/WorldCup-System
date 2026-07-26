using Core.DTOs.Bets;
using Data.Entities;
using Data.Repos;

namespace Core.Services.Bets
{
    public class LeaderboardService : ILeaderboardService
    {
        private readonly IRepositoryManager _repository;

        public LeaderboardService(IRepositoryManager repository)
        {
            _repository = repository;
        }

        public List<LeaderboardEntryDTO> GetLeaderboard(long? companyId, int? worldCupId = null)
        {
            return BuildLeaderboard(companyId, worldCupId);
        }

        public LeaderboardSummaryDTO GetMySummary(long userId, int? worldCupId = null)
        {
            User? user = _repository.User.GetAllAsync().FirstOrDefault(existingUser => existingUser.Id == userId);
            List<Bet> userBets = GetScopedBets(worldCupId)
                .Where(bet => bet.UserId == userId)
                .ToList();

            if (userBets.Count == 0)
            {
                return new LeaderboardSummaryDTO
                {
                    Rank = null,
                    UserId = userId,
                    UserName = user?.Name,
                    TotalPoints = 0,
                    ResolvedBets = 0,
                    ActiveBets = 0
                };
            }

            List<int> userBetIds = userBets.Select(bet => bet.Id).ToList();
            List<BetResult> userResults = _repository.BetResult
                .Find(result => userBetIds.Contains(result.BetId))
                .ToList();
            HashSet<int> resolvedBetIds = userResults.Select(result => result.BetId).ToHashSet();

            int totalPoints = userResults.Sum(result => result.Point);
            int resolvedBets = userResults.Count;
            int activeBets = userBets.Count(bet => !resolvedBetIds.Contains(bet.Id));

            // Rank is within the caller's company board only (null company → no rank).
            List<LeaderboardEntryDTO> leaderboard = BuildLeaderboard(user?.CompanyId, worldCupId);
            LeaderboardEntryDTO? entry = leaderboard.FirstOrDefault(existingEntry => existingEntry.UserId == userId);

            return new LeaderboardSummaryDTO
            {
                Rank = entry?.Rank,
                UserId = userId,
                UserName = user?.Name,
                TotalPoints = totalPoints,
                ResolvedBets = resolvedBets,
                ActiveBets = activeBets
            };
        }

        private List<LeaderboardEntryDTO> BuildLeaderboard(long? companyId, int? worldCupId)
        {
            if (!companyId.HasValue)
            {
                return new List<LeaderboardEntryDTO>();
            }

            List<User> companyUsers = _repository.User
                .Find(existingUser => existingUser.CompanyId == companyId.Value)
                .ToList();
            if (companyUsers.Count == 0)
            {
                return new List<LeaderboardEntryDTO>();
            }

            HashSet<long> companyUserIds = companyUsers.Select(existingUser => existingUser.Id).ToHashSet();
            List<Bet> scopedBets = GetScopedBets(worldCupId)
                .Where(bet => companyUserIds.Contains(bet.UserId))
                .ToList();
            if (scopedBets.Count == 0)
            {
                return new List<LeaderboardEntryDTO>();
            }

            List<int> betIds = scopedBets.Select(bet => bet.Id).ToList();
            List<BetResult> results = _repository.BetResult
                .Find(result => betIds.Contains(result.BetId))
                .ToList();

            Dictionary<long, User> usersById = companyUsers.ToDictionary(existingUser => existingUser.Id);
            Dictionary<long, LeaderboardEntryDTO> totals = new Dictionary<long, LeaderboardEntryDTO>();
            foreach (BetResult result in results)
            {
                Bet? bet = scopedBets.FirstOrDefault(existingBet => existingBet.Id == result.BetId);
                if (bet == null)
                {
                    continue;
                }

                if (!totals.TryGetValue(bet.UserId, out LeaderboardEntryDTO? entry))
                {
                    usersById.TryGetValue(bet.UserId, out User? user);
                    entry = new LeaderboardEntryDTO
                    {
                        UserId = bet.UserId,
                        UserName = user?.Name,
                        TotalPoints = 0,
                        ResolvedBets = 0
                    };
                    totals[bet.UserId] = entry;
                }

                entry.TotalPoints += result.Point;
                entry.ResolvedBets++;
            }

            List<LeaderboardEntryDTO> leaderboard = totals.Values
                .OrderByDescending(entry => entry.TotalPoints)
                .ThenByDescending(entry => entry.ResolvedBets)
                .ThenBy(entry => entry.UserName)
                .ToList();

            HashSet<long> rankedUserIds = totals.Keys.ToHashSet();
            foreach (Bet bet in scopedBets)
            {
                if (rankedUserIds.Contains(bet.UserId))
                {
                    continue;
                }

                usersById.TryGetValue(bet.UserId, out User? user);
                leaderboard.Add(new LeaderboardEntryDTO
                {
                    UserId = bet.UserId,
                    UserName = user?.Name,
                    TotalPoints = 0,
                    ResolvedBets = 0,
                });
                rankedUserIds.Add(bet.UserId);
            }

            leaderboard = leaderboard
                .OrderByDescending(entry => entry.TotalPoints)
                .ThenByDescending(entry => entry.ResolvedBets)
                .ThenBy(entry => entry.UserName)
                .ToList();

            for (int index = 0; index < leaderboard.Count; index++)
            {
                leaderboard[index].Rank = index + 1;
            }

            return leaderboard;
        }

        private List<Bet> GetScopedBets(int? worldCupId)
        {
            if (!worldCupId.HasValue)
            {
                return _repository.Bet.GetAllAsync().ToList();
            }

            HashSet<int> matchIds = GetMatchIdsForWorldCup(worldCupId.Value);
            if (matchIds.Count == 0)
            {
                return new List<Bet>();
            }

            return _repository.Bet
                .Find(bet => matchIds.Contains(bet.MatchId))
                .ToList();
        }

        private HashSet<int> GetMatchIdsForWorldCup(int worldCupId)
        {
            List<int> groupIds = _repository.Group
                .Find(group => group.WorldCupId == worldCupId)
                .Select(group => group.Id)
                .ToList();
            if (groupIds.Count == 0)
            {
                return new HashSet<int>();
            }

            List<int> teamIds = _repository.Team
                .Find(team => groupIds.Contains(team.GroupId))
                .Select(team => team.Id)
                .ToList();
            if (teamIds.Count == 0)
            {
                return new HashSet<int>();
            }

            return _repository.Match
                .Find(match =>
                    match.TeamOneId.HasValue
                    && match.TeamTwoId.HasValue
                    && teamIds.Contains(match.TeamOneId.Value)
                    && teamIds.Contains(match.TeamTwoId.Value))
                .Select(match => match.Id)
                .ToHashSet();
        }
    }
}
