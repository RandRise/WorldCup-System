using Core.DTOs.Bets;
using Data.Entities;
using Data.Repos;

namespace Core.Services.Bets
{
    public class BetService : IBetService
    {
        private readonly IRepositoryManager _repository;

        public BetService(IRepositoryManager repository)
        {
            _repository = repository;
        }

        public async Task PlaceBet(long userId, PlaceBetDTO betDto)
        {
            Match match = await _repository.Match.GetByIdAsync(betDto.MatchId);

            if (match.Date <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("Cannot place a bet after the match has started.");
            }

            ValidatePrediction(betDto, match);

            bool existingBet = _repository.Bet
                .Find(bet => bet.UserId == userId && bet.MatchId == betDto.MatchId)
                .Any();
            if (existingBet)
            {
                throw new InvalidOperationException("You have already placed a bet on this match.");
            }

            Bet bet = new Bet
            {
                UserId = userId,
                MatchId = betDto.MatchId,
                IsDraw = betDto.IsDraw,
                TeamId = betDto.IsDraw ? null : betDto.TeamId,
                Results = new List<BetResult>()
            };

            _repository.Bet.Create(bet);
            await _repository.SaveAsync();
        }

        public List<BetDTO> GetUserBets(long userId)
        {
            List<Bet> bets = _repository.Bet
                .Find(bet => bet.UserId == userId)
                .OrderByDescending(bet => bet.MatchId)
                .ToList();

            return BuildBetDtos(bets);
        }

        public List<BetDTO> GetActiveBets(long userId)
        {
            List<Bet> bets = _repository.Bet
                .Find(bet => bet.UserId == userId)
                .ToList();

            if (bets.Count == 0)
            {
                return new List<BetDTO>();
            }

            List<int> betIds = bets.Select(bet => bet.Id).ToList();
            HashSet<int> resolvedBetIds = _repository.BetResult
                .Find(result => betIds.Contains(result.BetId))
                .Select(result => result.BetId)
                .ToHashSet();

            List<Bet> activeBets = bets
                .Where(bet => !resolvedBetIds.Contains(bet.Id))
                .ToList();

            return BuildBetDtos(activeBets);
        }

        public BetDTO? GetMyBetForMatch(long userId, int matchId)
        {
            Bet? bet = _repository.Bet
                .Find(existingBet => existingBet.UserId == userId && existingBet.MatchId == matchId)
                .FirstOrDefault();
            if (bet == null)
            {
                return null;
            }

            return BuildBetDtos(new List<Bet> { bet }).FirstOrDefault();
        }

        public async Task<int> ResolveBetsForMatch(int matchId)
        {
            Match match = await _repository.Match.GetByIdAsync(matchId);
            DateTime fullTime = match.Date.AddMinutes(BetScoringRules.MatchDurationMinutes);

            if (fullTime > DateTime.UtcNow)
            {
                throw new InvalidOperationException(
                    $"Cannot resolve bets until {BetScoringRules.MatchDurationMinutes} minutes of play have elapsed.");
            }

            int? winnerTeamId = GetMatchWinnerTeamId(match);
            List<Bet> bets = _repository.Bet.Find(bet => bet.MatchId == matchId).ToList();
            if (bets.Count == 0)
            {
                return 0;
            }

            List<int> betIds = bets.Select(bet => bet.Id).ToList();
            Dictionary<int, BetResult> existingResults = _repository.BetResult
                .Find(result => betIds.Contains(result.BetId))
                .ToDictionary(result => result.BetId, result => result);

            int resolvedCount = 0;
            foreach (Bet bet in bets)
            {
                int points = CalculatePoints(bet, winnerTeamId);
                if (existingResults.TryGetValue(bet.Id, out BetResult? existingResult))
                {
                    if (existingResult.Point != points)
                    {
                        existingResult.Point = points;
                        _repository.BetResult.Update(existingResult);
                        resolvedCount++;
                    }

                    continue;
                }

                BetResult betResult = new BetResult
                {
                    BetId = bet.Id,
                    Point = points
                };

                _repository.BetResult.Create(betResult);
                resolvedCount++;
            }

            if (resolvedCount > 0)
            {
                await _repository.SaveAsync();
            }

            return resolvedCount;
        }

        public BettingRulesResponseDTO GetScoringRules()
        {
            return new BettingRulesResponseDTO
            {
                Summary = BetScoringRules.Summary,
                Rules = BetScoringRules.GetRules()
            };
        }

        private static void ValidatePrediction(PlaceBetDTO betDto, Match match)
        {
            if (betDto.IsDraw)
            {
                if (betDto.TeamId.HasValue)
                {
                    throw new InvalidOperationException("TeamId must not be set when predicting a draw.");
                }

                return;
            }

            if (!betDto.TeamId.HasValue)
            {
                throw new InvalidOperationException("TeamId is required when predicting a team win.");
            }

            if (betDto.TeamId.Value != match.TeamOneId && betDto.TeamId.Value != match.TeamTwoId)
            {
                throw new InvalidOperationException("Predicted team must be one of the teams playing in this match.");
            }
        }

        private static int CalculatePoints(Bet bet, int? winnerTeamId)
        {
            if (bet.IsDraw)
            {
                return winnerTeamId == null
                    ? BetScoringRules.CorrectOutcomePoints
                    : BetScoringRules.IncorrectPredictionPoints;
            }

            return winnerTeamId.HasValue && bet.TeamId == winnerTeamId.Value
                ? BetScoringRules.CorrectOutcomePoints
                : BetScoringRules.IncorrectPredictionPoints;
        }

        private List<BetDTO> BuildBetDtos(List<Bet> bets)
        {
            if (bets.Count == 0)
            {
                return new List<BetDTO>();
            }

            List<int> matchIds = bets.Select(bet => bet.MatchId).Distinct().ToList();
            List<Match> matches = _repository.Match
                .Find(match => matchIds.Contains(match.Id))
                .ToList();
            List<Team> teams = _repository.Team.GetAllAsync().ToList();
            List<Country> countries = _repository.Country.GetAllAsync().ToList();

            List<int> betIds = bets.Select(bet => bet.Id).ToList();
            List<BetResult> results = _repository.BetResult
                .Find(result => betIds.Contains(result.BetId))
                .ToList();

            return bets.Select(bet =>
            {
                Match? match = matches.FirstOrDefault(existingMatch => existingMatch.Id == bet.MatchId);
                BetResult? betResult = results.FirstOrDefault(result => result.BetId == bet.Id);
                bool isResolved = betResult != null;

                return new BetDTO
                {
                    Id = bet.Id,
                    UserId = bet.UserId,
                    MatchId = bet.MatchId,
                    MatchDate = match?.Date ?? DateTime.MinValue,
                    TeamOneName = match == null ? null : ResolveTeamName(match.TeamOneId, teams, countries),
                    TeamTwoName = match == null ? null : ResolveTeamName(match.TeamTwoId, teams, countries),
                    IsDraw = bet.IsDraw,
                    PredictedTeamId = bet.TeamId,
                    PredictedOutcome = bet.IsDraw
                        ? BetScoringRules.DrawOutcomeLabel
                        : ResolveTeamName(bet.TeamId, teams, countries),
                    IsResolved = isResolved,
                    PointsEarned = betResult?.Point,
                    IsActive = !isResolved
                };
            }).ToList();
        }

        private int? GetMatchWinnerTeamId(Match match)
        {
            List<TeamStats> stats = _repository.TeamStats
                .Find(teamStats => teamStats.MatchId == match.Id)
                .ToList();
            List<int> statIds = stats.Select(teamStats => teamStats.Id).ToList();
            List<Goal> goals = statIds.Count > 0
                ? _repository.Goal.Find(goal => statIds.Contains(goal.TeamStatsId)).ToList()
                : new List<Goal>();

            TeamStats? teamOneStats = stats.FirstOrDefault(teamStats => teamStats.TeamId == match.TeamOneId);
            TeamStats? teamTwoStats = stats.FirstOrDefault(teamStats => teamStats.TeamId == match.TeamTwoId);
            if (teamOneStats == null || teamTwoStats == null)
            {
                throw new InvalidOperationException(
                    "Cannot resolve bets: match team statistics are incomplete.");
            }

            int teamOneScore = goals.Count(goal => goal.TeamStatsId == teamOneStats.Id);
            int teamTwoScore = goals.Count(goal => goal.TeamStatsId == teamTwoStats.Id);

            if (teamOneScore > teamTwoScore)
            {
                return match.TeamOneId;
            }

            if (teamTwoScore > teamOneScore)
            {
                return match.TeamTwoId;
            }

            return null;
        }

        private static string? ResolveTeamName(int? teamId, List<Team> teams, List<Country> countries)
        {
            if (!teamId.HasValue)
            {
                return null;
            }

            Team? team = teams.FirstOrDefault(existingTeam => existingTeam.Id == teamId.Value);
            if (team == null)
            {
                return null;
            }

            return countries.FirstOrDefault(country => country.Id == team.CountryId)?.Name;
        }
    }
}
