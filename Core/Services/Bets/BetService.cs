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

            if (ToUtc(match.Date) <= DateTime.UtcNow)
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

        public List<BetDTO> GetMyBetsForWorldCup(long userId, int worldCupId)
        {
            List<int> matchIds = GetMatchIdsForWorldCup(worldCupId);
            if (matchIds.Count == 0)
            {
                return new List<BetDTO>();
            }

            List<Bet> bets = _repository.Bet
                .Find(bet => bet.UserId == userId && matchIds.Contains(bet.MatchId))
                .ToList();

            return BuildBetDtos(bets);
        }

        public async Task<ResolveBetsResultDTO> ResolveBetsForMatch(int matchId)
        {
            Match match = await _repository.Match.GetByIdAsync(matchId);
            DateTime fullTime = ToUtc(match.Date).AddMinutes(BetScoringRules.MatchDurationMinutes);

            if (fullTime > DateTime.UtcNow)
            {
                throw new InvalidOperationException(
                    $"Cannot resolve bets until {BetScoringRules.MatchDurationMinutes} minutes of play have elapsed.");
            }

            return await ResolveBetsForMatchCore(matchId, match);
        }

        public async Task TryAutoResolveFinishedMatch(int matchId)
        {
            Match match = await _repository.Match.GetByIdAsync(matchId);
            DateTime fullTime = ToUtc(match.Date).AddMinutes(BetScoringRules.MatchDurationMinutes);
            if (fullTime > DateTime.UtcNow)
            {
                return;
            }

            try
            {
                await ResolveBetsForMatchCore(matchId, match);
            }
            catch (InvalidOperationException)
            {
                // Stats may be incomplete until both team rows exist.
            }
        }

        public async Task<ResolveBetsWorldCupResultDTO> ResolveBetsForWorldCup(int worldCupId)
        {
            List<int> matchIds = GetMatchIdsForWorldCup(worldCupId);
            List<ResolveBetsResultDTO> matchResults = new List<ResolveBetsResultDTO>();
            int totalResolved = 0;
            int matchesProcessed = 0;

            foreach (int matchId in matchIds)
            {
                Match match = await _repository.Match.GetByIdAsync(matchId);
                DateTime fullTime = ToUtc(match.Date).AddMinutes(BetScoringRules.MatchDurationMinutes);
                if (fullTime > DateTime.UtcNow)
                {
                    continue;
                }

                try
                {
                    ResolveBetsResultDTO result = await ResolveBetsForMatchCore(matchId, match);
                    if (result.ResolvedCount > 0)
                    {
                        matchResults.Add(result);
                        totalResolved += result.ResolvedCount;
                    }

                    matchesProcessed++;
                }
                catch (InvalidOperationException)
                {
                    // Skip matches that cannot be resolved yet.
                }
            }

            return new ResolveBetsWorldCupResultDTO
            {
                WorldCupId = worldCupId,
                MatchesProcessed = matchesProcessed,
                TotalResolvedCount = totalResolved,
                Message = $"{totalResolved} bet(s) resolved across {matchesProcessed} finished match(es).",
                MatchResults = matchResults
            };
        }

        private async Task<ResolveBetsResultDTO> ResolveBetsForMatchCore(int matchId, Match match)
        {
            int? winnerTeamId = GetMatchWinnerTeamId(match);
            List<Bet> bets = _repository.Bet.Find(bet => bet.MatchId == matchId).ToList();
            if (bets.Count == 0)
            {
                return new ResolveBetsResultDTO
                {
                    MatchId = matchId,
                    ResolvedCount = 0,
                    Message = "No bets to resolve for this match.",
                    UserBreakdown = new List<BetResolveUserDTO>()
                };
            }

            List<User> users = _repository.User.GetAllAsync().ToList();
            List<Team> teams = _repository.Team.GetAllAsync().ToList();
            List<Country> countries = _repository.Country.GetAllAsync().ToList();

            List<int> betIds = bets.Select(bet => bet.Id).ToList();
            Dictionary<int, BetResult> existingResults = _repository.BetResult
                .Find(result => betIds.Contains(result.BetId))
                .ToDictionary(result => result.BetId, result => result);

            int resolvedCount = 0;
            List<BetResolveUserDTO> breakdown = new List<BetResolveUserDTO>();
            foreach (Bet bet in bets)
            {
                int points = CalculatePoints(bet, winnerTeamId);
                int? previousPoints = null;
                bool changed = false;

                if (existingResults.TryGetValue(bet.Id, out BetResult? existingResult))
                {
                    previousPoints = existingResult.Point;
                    if (existingResult.Point != points)
                    {
                        existingResult.Point = points;
                        _repository.BetResult.Update(existingResult);
                        resolvedCount++;
                        changed = true;
                    }
                }
                else
                {
                    BetResult betResult = new BetResult
                    {
                        BetId = bet.Id,
                        Point = points
                    };

                    _repository.BetResult.Create(betResult);
                    resolvedCount++;
                    changed = true;
                }

                if (changed || previousPoints == null)
                {
                    User? user = users.FirstOrDefault(existingUser => existingUser.Id == bet.UserId);
                    breakdown.Add(new BetResolveUserDTO
                    {
                        UserId = bet.UserId,
                        UserName = user?.Name,
                        PredictedOutcome = bet.IsDraw
                            ? BetScoringRules.DrawOutcomeLabel
                            : ResolveTeamName(bet.TeamId, teams, countries),
                        PointsAwarded = points,
                        PreviousPoints = previousPoints
                    });
                }
            }

            if (resolvedCount > 0)
            {
                await _repository.SaveAsync();
            }

            return new ResolveBetsResultDTO
            {
                MatchId = matchId,
                ResolvedCount = resolvedCount,
                Message = $"{resolvedCount} bet(s) resolved.",
                UserBreakdown = breakdown
            };
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
            if (!match.TeamOneId.HasValue || !match.TeamTwoId.HasValue)
            {
                throw new InvalidOperationException("Cannot place bets until both teams are set for this match.");
            }

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

        private List<int> GetMatchIdsForWorldCup(int worldCupId)
        {
            List<int> groupIds = _repository.Group
                .Find(group => group.WorldCupId == worldCupId)
                .Select(group => group.Id)
                .ToList();
            if (groupIds.Count == 0)
            {
                return new List<int>();
            }

            List<int> teamIds = _repository.Team
                .Find(team => groupIds.Contains(team.GroupId))
                .Select(team => team.Id)
                .ToList();
            if (teamIds.Count == 0)
            {
                return new List<int>();
            }

            return _repository.Match
                .Find(match =>
                    match.TeamOneId.HasValue
                    && match.TeamTwoId.HasValue
                    && teamIds.Contains(match.TeamOneId.Value)
                    && teamIds.Contains(match.TeamTwoId.Value))
                .Select(match => match.Id)
                .ToList();
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
            DateTime regulationEnd = ToUtc(match.Date).AddMinutes(BetScoringRules.MatchDurationMinutes);
            List<Goal> goals = statIds.Count > 0
                ? _repository.Goal
                    .Find(goal => statIds.Contains(goal.TeamStatsId))
                    .Where(goal => ToUtc(goal.TimeScored) <= regulationEnd)
                    .ToList()
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

        private static DateTime ToUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                : value.ToUniversalTime();
        }
    }
}
