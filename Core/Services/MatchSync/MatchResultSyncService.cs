using Core.DTOs.Bets;
using Core.DTOs.Matches;
using Core.Services.Bets;
using Core.Services.Knockout;
using Core.Services.Matches;
using Data.Entities;
using Data.Repos;

namespace Core.Services.MatchSync
{
    /// <summary>
    /// Post-match FT sync: fetch external score → idempotently apply Goal rows → resolve bets.
    /// Score-only for 1X2 betting; does not sync cards or team stats beyond ensuring TeamStats exist.
    /// </summary>
    public class MatchResultSyncService : IMatchResultSyncService
    {
        public const string PlaceholderScorerName = "Tournament Scorer";
        private const int PlaceholderScorerNumber = 99;
        private const string ForwardPositionName = "Forward";

        private readonly IRepositoryManager _repository;
        private readonly IExternalMatchResultProvider _resultProvider;
        private readonly IBetService _betService;
        private readonly IKnockoutService _knockoutService;
        private readonly IMatchService _matchService;

        public MatchResultSyncService(
            IRepositoryManager repository,
            IExternalMatchResultProvider resultProvider,
            IBetService betService,
            IKnockoutService knockoutService,
            IMatchService matchService)
        {
            _repository = repository;
            _resultProvider = resultProvider;
            _betService = betService;
            _knockoutService = knockoutService;
            _matchService = matchService;
        }

        public async Task SetExternalMatchId(SetExternalMatchIdDTO request)
        {
            string externalMatchId = request.ExternalMatchId.Trim();
            if (string.IsNullOrWhiteSpace(externalMatchId))
            {
                throw new ArgumentException("ExternalMatchId is required.");
            }

            Match match = await _repository.Match.GetByIdAsync(request.MatchId);

            Match? conflicting = _repository.Match
                .Find(existing =>
                    existing.ExternalMatchId == externalMatchId
                    && existing.Id != request.MatchId)
                .FirstOrDefault();
            if (conflicting != null)
            {
                throw new InvalidOperationException(
                    $"ExternalMatchId '{externalMatchId}' is already mapped to match {conflicting.Id}.");
            }

            match.ExternalMatchId = externalMatchId;
            _repository.Match.Update(match);
            await _repository.SaveAsync();
        }

        public async Task<SyncMatchResultDTO> SyncResult(int matchId, CancellationToken cancellationToken = default)
        {
            Match match = await _repository.Match.GetByIdAsync(matchId);
            if (string.IsNullOrWhiteSpace(match.ExternalMatchId))
            {
                throw new InvalidOperationException(
                    $"Match {matchId} has no ExternalMatchId. Map a FIFA IdMatch before syncing.");
            }

            if (!match.TeamOneId.HasValue || !match.TeamTwoId.HasValue)
            {
                throw new InvalidOperationException(
                    $"Cannot sync match {matchId} until both teams are set.");
            }

            ExternalMatchResult external = await _resultProvider.FetchResultAsync(
                match.ExternalMatchId,
                cancellationToken);

            if (!external.IsFinished)
            {
                return new SyncMatchResultDTO
                {
                    MatchId = matchId,
                    ExternalMatchId = match.ExternalMatchId,
                    Applied = false,
                    ScoreChanged = false,
                    Message = $"External match is not finished yet ({external.StatusLabel ?? "unknown status"})."
                };
            }

            (int teamOneScore, int teamTwoScore) = OrientScoresToLocalSides(match, external);

            (bool scoreChanged, int appliedTeamOneScore, int appliedTeamTwoScore) = await ApplyScoreIdempotentAsync(
                match,
                teamOneScore,
                teamTwoScore);

            (int betsResolved, ResolveBetsResultDTO? resolveResult, string? warning) =
                await TryResolveAndAdvance(matchId, match);

            string resolveNote = resolveResult != null
                ? $" Bet resolve: {betsResolved} update(s)."
                : " Bet resolve skipped (match not past full-time clock or stats incomplete).";

            string message = scoreChanged
                ? $"Applied FT score {appliedTeamOneScore}-{appliedTeamTwoScore} from external feed.{resolveNote}"
                : $"Score already matched FT {appliedTeamOneScore}-{appliedTeamTwoScore}.{resolveNote}";

            return new SyncMatchResultDTO
            {
                MatchId = matchId,
                ExternalMatchId = match.ExternalMatchId,
                Applied = true,
                ScoreChanged = scoreChanged,
                TeamOneScore = appliedTeamOneScore,
                TeamTwoScore = appliedTeamTwoScore,
                BetsResolved = betsResolved,
                Message = message,
                Warning = warning,
                ResolveResult = resolveResult
            };
        }

        public async Task<SyncFinishedResultsDTO> SyncFinishedResults(
            int worldCupId,
            CancellationToken cancellationToken = default)
        {
            List<MatchDTO> fixtures = _matchService.GetFixturesByWorldCup(worldCupId);
            HashSet<int> fixtureIds = fixtures.Select(fixture => fixture.Id).ToHashSet();

            List<Match> mappedMatches = _repository.Match
                .Find(match =>
                    match.ExternalMatchId != null
                    && match.ExternalMatchId != string.Empty
                    && fixtureIds.Contains(match.Id))
                .ToList();

            List<SyncMatchResultDTO> results = new List<SyncMatchResultDTO>();
            int appliedCount = 0;
            int totalBetsResolved = 0;

            foreach (Match match in mappedMatches.OrderBy(match => match.Date).ThenBy(match => match.Id))
            {
                try
                {
                    SyncMatchResultDTO result = await SyncResult(match.Id, cancellationToken);
                    results.Add(result);
                    if (result.Applied)
                    {
                        appliedCount++;
                        totalBetsResolved += result.BetsResolved;
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new SyncMatchResultDTO
                    {
                        MatchId = match.Id,
                        ExternalMatchId = match.ExternalMatchId,
                        Applied = false,
                        Message = ex.Message
                    });
                }
            }

            return new SyncFinishedResultsDTO
            {
                WorldCupId = worldCupId,
                MatchesAttempted = mappedMatches.Count,
                MatchesApplied = appliedCount,
                TotalBetsResolved = totalBetsResolved,
                Message =
                    $"Attempted {mappedMatches.Count} mapped match(es); applied {appliedCount}; " +
                    $"resolved {totalBetsResolved} bet update(s).",
                Results = results
            };
        }

        private static readonly Dictionary<string, string> ExternalTeamNameAliases =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Korea Republic"] = "South Korea",
                ["Czechia"] = "Czech Republic",
                ["USA"] = "United States",
                ["USMNT"] = "United States",
                ["Türkiye"] = "Turkey",
                ["Turkiye"] = "Turkey"
            };

        private (int TeamOneScore, int TeamTwoScore) OrientScoresToLocalSides(
            Match match,
            ExternalMatchResult external)
        {
            string teamOneName = ResolveLocalTeamName(match.TeamOneId!.Value);
            string teamTwoName = ResolveLocalTeamName(match.TeamTwoId!.Value);

            bool homeIsTeamOne = TeamNamesMatch(teamOneName, external.HomeTeamName, external.HomeTeamCountryCode);
            bool homeIsTeamTwo = TeamNamesMatch(teamTwoName, external.HomeTeamName, external.HomeTeamCountryCode);
            bool awayIsTeamOne = TeamNamesMatch(teamOneName, external.AwayTeamName, external.AwayTeamCountryCode);
            bool awayIsTeamTwo = TeamNamesMatch(teamTwoName, external.AwayTeamName, external.AwayTeamCountryCode);

            if (homeIsTeamOne && awayIsTeamTwo)
            {
                return (external.HomeScore, external.AwayScore);
            }

            if (homeIsTeamTwo && awayIsTeamOne)
            {
                return (external.AwayScore, external.HomeScore);
            }

            throw new InvalidOperationException(
                $"External sides do not match local teams for match {match.Id}. " +
                $"Local: '{teamOneName}' vs '{teamTwoName}'. " +
                $"External: '{external.HomeTeamName}' ({external.HomeTeamCountryCode}) vs " +
                $"'{external.AwayTeamName}' ({external.AwayTeamCountryCode}).");
        }

        private string ResolveLocalTeamName(int teamId)
        {
            Team team = _repository.Team.Find(existing => existing.Id == teamId).FirstOrDefault()
                ?? throw new InvalidOperationException($"Team {teamId} was not found.");
            Country? country = _repository.Country.Find(existing => existing.Id == team.CountryId).FirstOrDefault();
            if (country == null || string.IsNullOrWhiteSpace(country.Name))
            {
                throw new InvalidOperationException($"Country for team {teamId} was not found.");
            }

            return country.Name;
        }

        private static bool TeamNamesMatch(string localName, string? externalName, string? externalCountryCode)
        {
            if (string.IsNullOrWhiteSpace(localName))
            {
                return false;
            }

            string normalizedLocal = NormalizeName(localName);
            foreach (string candidate in ExpandExternalNames(externalName, externalCountryCode))
            {
                string normalizedCandidate = NormalizeName(candidate);
                if (normalizedCandidate == normalizedLocal
                    || normalizedCandidate.Contains(normalizedLocal)
                    || normalizedLocal.Contains(normalizedCandidate))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<string> ExpandExternalNames(string? externalName, string? externalCountryCode)
        {
            if (!string.IsNullOrWhiteSpace(externalName))
            {
                yield return externalName;
                if (ExternalTeamNameAliases.TryGetValue(externalName.Trim(), out string? aliased))
                {
                    yield return aliased;
                }
            }

            if (!string.IsNullOrWhiteSpace(externalCountryCode)
                && ExternalTeamNameAliases.TryGetValue(externalCountryCode.Trim(), out string? codeAlias))
            {
                yield return codeAlias;
            }
        }

        private static string NormalizeName(string value)
        {
            return new string(value
                .Trim()
                .ToLowerInvariant()
                .Where(character => !char.IsWhiteSpace(character) && character != '.' && character != '-')
                .ToArray());
        }

        private async Task<(bool ScoreChanged, int TeamOneScore, int TeamTwoScore)> ApplyScoreIdempotentAsync(
            Match match,
            int homeScore,
            int awayScore)
        {
            if (homeScore < 0 || awayScore < 0)
            {
                throw new InvalidOperationException("External scores cannot be negative.");
            }

            int teamOneId = match.TeamOneId!.Value;
            int teamTwoId = match.TeamTwoId!.Value;

            TeamStats teamOneStats = await EnsureTeamStatsAsync(match.Id, teamOneId);
            TeamStats teamTwoStats = await EnsureTeamStatsAsync(match.Id, teamTwoId);

            List<int> statIds = new List<int> { teamOneStats.Id, teamTwoStats.Id };
            List<Goal> existingGoals = _repository.Goal
                .Find(goal => statIds.Contains(goal.TeamStatsId))
                .ToList();

            int currentHome = existingGoals.Count(goal => goal.TeamStatsId == teamOneStats.Id);
            int currentAway = existingGoals.Count(goal => goal.TeamStatsId == teamTwoStats.Id);
            if (currentHome == homeScore && currentAway == awayScore)
            {
                return (false, homeScore, awayScore);
            }

            foreach (Goal goal in existingGoals)
            {
                _repository.Goal.Delete(goal);
            }

            Player teamOneScorer = await EnsurePlaceholderScorerAsync(teamOneId);
            Player teamTwoScorer = await EnsurePlaceholderScorerAsync(teamTwoId);

            for (int index = 0; index < homeScore; index++)
            {
                _repository.Goal.Create(new Goal
                {
                    PlayerId = teamOneScorer.Id,
                    TeamStatsId = teamOneStats.Id,
                    TimeScored = match.Date.AddMinutes(Math.Min(1 + index, BetScoringRules.MatchDurationMinutes)),
                    IsOwnGoal = 0
                });
            }

            for (int index = 0; index < awayScore; index++)
            {
                _repository.Goal.Create(new Goal
                {
                    PlayerId = teamTwoScorer.Id,
                    TeamStatsId = teamTwoStats.Id,
                    TimeScored = match.Date.AddMinutes(Math.Min(1 + index, BetScoringRules.MatchDurationMinutes)),
                    IsOwnGoal = 0
                });
            }

            await _repository.SaveAsync();
            return (true, homeScore, awayScore);
        }

        private async Task<(int BetsResolved, ResolveBetsResultDTO? ResolveResult, string? Warning)> TryResolveAndAdvance(
            int matchId,
            Match match)
        {
            ResolveBetsResultDTO? resolveResult = null;
            int betsResolved = 0;
            string? warning = null;

            DateTime fullTime = match.Date.AddMinutes(BetScoringRules.MatchDurationMinutes);
            if (fullTime <= DateTime.UtcNow)
            {
                try
                {
                    resolveResult = await _betService.ResolveBetsForMatch(matchId);
                    betsResolved = resolveResult.ResolvedCount;
                }
                catch (InvalidOperationException)
                {
                    // Incomplete stats or transient state — leave unresolved for a later pass.
                }
            }

            try
            {
                await _knockoutService.TryAdvanceFromMatch(matchId);
            }
            catch (InvalidOperationException ex)
            {
                warning = ex.Message;
            }

            return (betsResolved, resolveResult, warning);
        }

        private async Task<TeamStats> EnsureTeamStatsAsync(int matchId, int teamId)
        {
            TeamStats? stats = _repository.TeamStats
                .Find(teamStats => teamStats.MatchId == matchId && teamStats.TeamId == teamId)
                .FirstOrDefault();
            if (stats != null)
            {
                return stats;
            }

            TeamStats created = new TeamStats
            {
                MatchId = matchId,
                TeamId = teamId,
                Possession = 50,
                Shots = 0,
                ShotsOnTarget = 0,
                Points = 0
            };
            _repository.TeamStats.Create(created);
            await _repository.SaveAsync();
            return created;
        }

        private async Task<Player> EnsurePlaceholderScorerAsync(int teamId)
        {
            Player? existing = _repository.Player
                .Find(player => player.TeamId == teamId && player.Name == PlaceholderScorerName)
                .FirstOrDefault();
            if (existing != null)
            {
                return existing;
            }

            PlayerPosition? forward = _repository.PlayerPosition
                .Find(position => position.Name == ForwardPositionName)
                .FirstOrDefault();
            if (forward == null)
            {
                throw new InvalidOperationException(
                    $"Player position '{ForwardPositionName}' is not seeded; cannot create placeholder scorers.");
            }

            Player created = new Player
            {
                Name = PlaceholderScorerName,
                Number = PlaceholderScorerNumber,
                TeamId = teamId,
                PositionId = forward.Id
            };
            _repository.Player.Create(created);
            await _repository.SaveAsync();
            return created;
        }
    }
}
