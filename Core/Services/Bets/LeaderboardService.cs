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

        public List<LeaderboardEntryDTO> GetLeaderboard()
        {
            List<BetResult> results = _repository.BetResult.GetAllAsync().ToList();
            if (results.Count == 0)
            {
                return new List<LeaderboardEntryDTO>();
            }

            List<int> betIds = results.Select(result => result.BetId).Distinct().ToList();
            List<Bet> bets = _repository.Bet
                .Find(bet => betIds.Contains(bet.Id))
                .ToList();
            List<User> users = _repository.User.GetAllAsync().ToList();

            Dictionary<long, LeaderboardEntryDTO> totals = new Dictionary<long, LeaderboardEntryDTO>();
            foreach (BetResult result in results)
            {
                Bet? bet = bets.FirstOrDefault(existingBet => existingBet.Id == result.BetId);
                if (bet == null)
                {
                    continue;
                }

                if (!totals.TryGetValue(bet.UserId, out LeaderboardEntryDTO? entry))
                {
                    User? user = users.FirstOrDefault(existingUser => existingUser.Id == bet.UserId);
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

            for (int index = 0; index < leaderboard.Count; index++)
            {
                leaderboard[index].Rank = index + 1;
            }

            return leaderboard;
        }
    }
}
