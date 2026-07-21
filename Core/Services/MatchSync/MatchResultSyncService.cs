using Core.DTOs.Bets;
using Core.DTOs.Matches;
using Core.Options;
using Core.Services.Bets;
using Core.Services.Knockout;
using Core.Services.Matches;
using Data.Entities;
using Data.Repos;
using Microsoft.Extensions.Options;

namespace Core.Services.MatchSync
{
    /// <summary>
    /// Post-match FT sync: fetch external score → idempotently apply Goal rows → resolve bets →
    /// then (fail-soft) fetch FIFA timeline Goal! events and rewrite scorers when counts align.
    /// Also exposes scorers-only backfill (<see cref="SyncScorers"/>) that never changes FT or bets.
    /// Does not sync cards or team stats beyond ensuring TeamStats exist.
    /// </summary>
    public class MatchResultSyncService : IMatchResultSyncService
    {
        public const string PlaceholderScorerName = "Tournament Scorer";
        private const int PlaceholderScorerNumber = 99;
        private const string ForwardPositionName = "Forward";

        private readonly IRepositoryManager _repository;
        private readonly IExternalMatchResultProvider _resultProvider;
        private readonly IExternalMatchEventsProvider _eventsProvider;
        private readonly ITimelineScorerApplyService _scorerApplyService;
        private readonly IBetService _betService;
        private readonly IKnockoutService _knockoutService;
        private readonly IMatchService _matchService;
        private readonly MatchResultSyncOptions _options;

        public MatchResultSyncService(
            IRepositoryManager repository,
            IExternalMatchResultProvider resultProvider,
            IExternalMatchEventsProvider eventsProvider,
            ITimelineScorerApplyService scorerApplyService,
            IBetService betService,
            IKnockoutService knockoutService,
            IMatchService matchService,
            IOptions<MatchResultSyncOptions> options)
        {
            _repository = repository;
            _resultProvider = resultProvider;
            _eventsProvider = eventsProvider;
            _scorerApplyService = scorerApplyService;
            _betService = betService;
            _knockoutService = knockoutService;
            _matchService = matchService;
            _options = options.Value;
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
            if (request.ExternalStageId != null)
            {
                string trimmedStageId = request.ExternalStageId.Trim();
                match.ExternalStageId = string.IsNullOrWhiteSpace(trimmedStageId) ? null : trimmedStageId;
            }

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

            await PersistExternalStageIdIfNeededAsync(match, external.ExternalStageId);

            if (!external.IsFinished)
            {
                return new SyncMatchResultDTO
                {
                    MatchId = matchId,
                    ExternalMatchId = match.ExternalMatchId,
                    ExternalStageId = match.ExternalStageId,
                    Applied = false,
                    ScoreChanged = false,
                    Message = $"External match is not finished yet ({external.StatusLabel ?? "unknown status"})."
                };
            }

            (int teamOneScore, int teamTwoScore, bool homeMapsToTeamOne) =
                OrientScoresToLocalSides(match, external);

            (bool scoreChanged, int appliedTeamOneScore, int appliedTeamTwoScore) = await ApplyScoreIdempotentAsync(
                match,
                teamOneScore,
                teamTwoScore);

            (int betsResolved, ResolveBetsResultDTO? resolveResult, string? warning) =
                await TryResolveAndAdvance(matchId, match);

            (string scorerStatus, int scorerGoalsUpdated, string scorerMessage, string? scorerWarning) =
                await TryApplyTimelineScorersAsync(match, homeMapsToTeamOne, cancellationToken);

            string resolveNote = resolveResult != null
                ? $" Bet resolve: {betsResolved} update(s)."
                : " Bet resolve skipped (match not past full-time clock or stats incomplete).";

            string message = scoreChanged
                ? $"Applied FT score {appliedTeamOneScore}-{appliedTeamTwoScore} from external feed.{resolveNote}"
                : $"Score already matched FT {appliedTeamOneScore}-{appliedTeamTwoScore}.{resolveNote}";
            message += $" Scorers: {scorerMessage}";

            return new SyncMatchResultDTO
            {
                MatchId = matchId,
                ExternalMatchId = match.ExternalMatchId,
                ExternalStageId = match.ExternalStageId,
                Applied = true,
                ScoreChanged = scoreChanged,
                TeamOneScore = appliedTeamOneScore,
                TeamTwoScore = appliedTeamTwoScore,
                BetsResolved = betsResolved,
                Message = message,
                Warning = MergeWarnings(warning, scorerWarning),
                ResolveResult = resolveResult,
                ScorerStatus = scorerStatus,
                ScorerGoalsUpdated = scorerGoalsUpdated,
                ScorerMessage = scorerMessage
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
                .OrderBy(match => match.Date)
                .ThenBy(match => match.Id)
                .ToList();

            List<SyncMatchResultDTO> results = new List<SyncMatchResultDTO>();
            int appliedCount = 0;
            int totalBetsResolved = 0;
            int scorersApplied = 0;
            int scorerWarnings = 0;

            for (int index = 0; index < mappedMatches.Count; index++)
            {
                Match match = mappedMatches[index];
                try
                {
                    SyncMatchResultDTO result = await SyncResult(match.Id, cancellationToken);
                    results.Add(result);
                    if (result.Applied)
                    {
                        appliedCount++;
                        totalBetsResolved += result.BetsResolved;
                    }

                    if (result.ScorerStatus == SyncScorerStatuses.Applied)
                    {
                        scorersApplied++;
                    }
                    else if (result.ScorerStatus == SyncScorerStatuses.Warning)
                    {
                        scorerWarnings++;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    string? externalMatchId = match.ExternalMatchId;
                    string? externalStageId = match.ExternalStageId;
                    try
                    {
                        Match refreshed = await _repository.Match.GetByIdAsync(match.Id);
                        externalMatchId = refreshed.ExternalMatchId;
                        externalStageId = refreshed.ExternalStageId;
                    }
                    catch
                    {
                        // Fall back to the in-memory mapping if refresh fails.
                    }

                    results.Add(new SyncMatchResultDTO
                    {
                        MatchId = match.Id,
                        ExternalMatchId = externalMatchId,
                        ExternalStageId = externalStageId,
                        Applied = false,
                        Message = ex.Message
                    });
                }

                if (_options.BatchDelayMilliseconds > 0 && index < mappedMatches.Count - 1)
                {
                    await Task.Delay(_options.BatchDelayMilliseconds, cancellationToken);
                }
            }

            return new SyncFinishedResultsDTO
            {
                WorldCupId = worldCupId,
                MatchesAttempted = mappedMatches.Count,
                MatchesApplied = appliedCount,
                TotalBetsResolved = totalBetsResolved,
                ScorersApplied = scorersApplied,
                ScorerWarnings = scorerWarnings,
                Message =
                    $"Attempted {mappedMatches.Count} mapped match(es); applied {appliedCount}; " +
                    $"resolved {totalBetsResolved} bet update(s); " +
                    $"scorers applied {scorersApplied}; scorer warnings {scorerWarnings}.",
                Results = results
            };
        }

        /// <summary>
        /// Scorers-only: uses calendar for finished + home/away orientation (and stage persist), then timeline apply.
        /// Does not rewrite Goal counts, resolve bets, or advance knockout.
        /// </summary>
        public async Task<SyncMatchResultDTO> SyncScorers(int matchId, CancellationToken cancellationToken = default)
        {
            Match match = await _repository.Match.GetByIdAsync(matchId);
            if (string.IsNullOrWhiteSpace(match.ExternalMatchId))
            {
                throw new InvalidOperationException(
                    $"Match {matchId} has no ExternalMatchId. Map a FIFA IdMatch before syncing scorers.");
            }

            if (!match.TeamOneId.HasValue || !match.TeamTwoId.HasValue)
            {
                throw new InvalidOperationException(
                    $"Cannot sync scorers for match {matchId} until both teams are set.");
            }

            ExternalMatchResult external = await _resultProvider.FetchResultAsync(
                match.ExternalMatchId,
                cancellationToken);

            await PersistExternalStageIdIfNeededAsync(match, external.ExternalStageId);

            if (!external.IsFinished)
            {
                return new SyncMatchResultDTO
                {
                    MatchId = matchId,
                    ExternalMatchId = match.ExternalMatchId,
                    ExternalStageId = match.ExternalStageId,
                    Applied = false,
                    ScoreChanged = false,
                    Message =
                        $"External match is not finished yet ({external.StatusLabel ?? "unknown status"}). " +
                        "Scorers not attempted."
                };
            }

            (_, _, bool homeMapsToTeamOne) = OrientScoresToLocalSides(match, external);
            (int teamOneScore, int teamTwoScore) = GetLocalGoalCounts(match);

            (string scorerStatus, int scorerGoalsUpdated, string scorerMessage, string? scorerWarning) =
                await TryApplyTimelineScorersForBackfillAsync(match, homeMapsToTeamOne, cancellationToken);

            bool applied =
                scorerStatus == SyncScorerStatuses.Applied
                || scorerStatus == SyncScorerStatuses.AlreadyMatched;

            return new SyncMatchResultDTO
            {
                MatchId = matchId,
                ExternalMatchId = match.ExternalMatchId,
                ExternalStageId = match.ExternalStageId,
                Applied = applied,
                ScoreChanged = false,
                TeamOneScore = teamOneScore,
                TeamTwoScore = teamTwoScore,
                BetsResolved = 0,
                Message =
                    $"Scorers-only sync (FT unchanged {teamOneScore}-{teamTwoScore}). Scorers: {scorerMessage}",
                Warning = scorerWarning,
                ScorerStatus = scorerStatus,
                ScorerGoalsUpdated = scorerGoalsUpdated,
                ScorerMessage = scorerMessage
            };
        }

        public async Task<SyncFinishedResultsDTO> SyncScorersForWorldCup(
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
                .OrderBy(match => match.Date)
                .ThenBy(match => match.Id)
                .ToList();

            List<SyncMatchResultDTO> results = new List<SyncMatchResultDTO>();
            int appliedCount = 0;
            int scorersApplied = 0;
            int scorerWarnings = 0;
            int alreadyMatched = 0;
            int skipped = 0;

            for (int index = 0; index < mappedMatches.Count; index++)
            {
                Match match = mappedMatches[index];
                try
                {
                    SyncMatchResultDTO result = await SyncScorers(match.Id, cancellationToken);
                    results.Add(result);
                    if (result.Applied)
                    {
                        appliedCount++;
                    }

                    if (result.ScorerStatus == SyncScorerStatuses.Applied)
                    {
                        scorersApplied++;
                    }
                    else if (result.ScorerStatus == SyncScorerStatuses.AlreadyMatched)
                    {
                        alreadyMatched++;
                    }
                    else if (result.ScorerStatus == SyncScorerStatuses.Warning)
                    {
                        scorerWarnings++;
                    }
                    else if (result.ScorerStatus == SyncScorerStatuses.Skipped
                        || result.ScorerStatus == null)
                    {
                        skipped++;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    string? externalMatchId = match.ExternalMatchId;
                    string? externalStageId = match.ExternalStageId;
                    try
                    {
                        Match refreshed = await _repository.Match.GetByIdAsync(match.Id);
                        externalMatchId = refreshed.ExternalMatchId;
                        externalStageId = refreshed.ExternalStageId;
                    }
                    catch
                    {
                        // Fall back to the in-memory mapping if refresh fails.
                    }

                    results.Add(new SyncMatchResultDTO
                    {
                        MatchId = match.Id,
                        ExternalMatchId = externalMatchId,
                        ExternalStageId = externalStageId,
                        Applied = false,
                        ScoreChanged = false,
                        Message = ex.Message,
                        ScorerStatus = SyncScorerStatuses.Warning,
                        ScorerMessage = "not applied (see message)."
                    });
                    scorerWarnings++;
                }

                if (_options.BatchDelayMilliseconds > 0 && index < mappedMatches.Count - 1)
                {
                    await Task.Delay(_options.BatchDelayMilliseconds, cancellationToken);
                }
            }

            return new SyncFinishedResultsDTO
            {
                WorldCupId = worldCupId,
                MatchesAttempted = mappedMatches.Count,
                MatchesApplied = appliedCount,
                TotalBetsResolved = 0,
                ScorersApplied = scorersApplied,
                ScorerWarnings = scorerWarnings,
                Message =
                    $"Scorers-only: attempted {mappedMatches.Count} mapped match(es); " +
                    $"applied/matched {appliedCount} (scorers updated {scorersApplied}, already matched {alreadyMatched}); " +
                    $"skipped {skipped}; scorer warnings {scorerWarnings}.",
                Results = results
            };
        }

        /// <summary>
        /// Local FT Goal counts for TeamOne/TeamTwo (not calendar scores). Used by scorers-only backfill reporting.
        /// </summary>
        private (int TeamOneScore, int TeamTwoScore) GetLocalGoalCounts(Match match)
        {
            int teamOneId = match.TeamOneId!.Value;
            int teamTwoId = match.TeamTwoId!.Value;

            TeamStats? teamOneStats = _repository.TeamStats
                .Find(teamStats => teamStats.MatchId == match.Id && teamStats.TeamId == teamOneId)
                .FirstOrDefault();
            TeamStats? teamTwoStats = _repository.TeamStats
                .Find(teamStats => teamStats.MatchId == match.Id && teamStats.TeamId == teamTwoId)
                .FirstOrDefault();

            if (teamOneStats == null || teamTwoStats == null)
            {
                return (0, 0);
            }

            List<int> statIds = new List<int> { teamOneStats.Id, teamTwoStats.Id };
            List<Goal> existingGoals = _repository.Goal
                .Find(goal => statIds.Contains(goal.TeamStatsId))
                .ToList();

            int teamOneScore = existingGoals.Count(goal => goal.TeamStatsId == teamOneStats.Id);
            int teamTwoScore = existingGoals.Count(goal => goal.TeamStatsId == teamTwoStats.Id);
            return (teamOneScore, teamTwoScore);
        }

        /// <summary>
        /// Stores FIFA IdStage when calendar returns one and the match is missing it or has a different value.
        /// Timeline URLs need IdStage alongside IdMatch; competition/season stay in MatchResultSync config.
        /// </summary>
        private async Task PersistExternalStageIdIfNeededAsync(Match match, string? externalStageId)
        {
            if (string.IsNullOrWhiteSpace(externalStageId))
            {
                return;
            }

            string trimmedStageId = externalStageId.Trim();
            if (string.Equals(match.ExternalStageId, trimmedStageId, StringComparison.Ordinal))
            {
                return;
            }

            match.ExternalStageId = trimmedStageId;
            _repository.Match.Update(match);
            await _repository.SaveAsync();
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

        /// <summary>
        /// Fail-soft timeline scorer step. Calendar FT / bets already succeeded; never throw from here.
        /// </summary>
        private async Task<(string Status, int GoalsUpdated, string Message, string? Warning)> TryApplyTimelineScorersAsync(
            Match match,
            bool homeMapsToTeamOne,
            CancellationToken cancellationToken)
        {
            return await TryApplyTimelineScorersCoreAsync(
                match,
                homeMapsToTeamOne,
                skipWhenNoTimelineGoals: false,
                cancellationToken);
        }

        /// <summary>
        /// Backfill scorer step: same fail-soft rules, but skips when timeline has no Goal! events
        /// (does not treat empty timeline vs existing Goal rows as a count-mismatch warning).
        /// </summary>
        private async Task<(string Status, int GoalsUpdated, string Message, string? Warning)> TryApplyTimelineScorersForBackfillAsync(
            Match match,
            bool homeMapsToTeamOne,
            CancellationToken cancellationToken)
        {
            return await TryApplyTimelineScorersCoreAsync(
                match,
                homeMapsToTeamOne,
                skipWhenNoTimelineGoals: true,
                cancellationToken);
        }

        private async Task<(string Status, int GoalsUpdated, string Message, string? Warning)> TryApplyTimelineScorersCoreAsync(
            Match match,
            bool homeMapsToTeamOne,
            bool skipWhenNoTimelineGoals,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(match.ExternalMatchId))
            {
                return (
                    SyncScorerStatuses.Skipped,
                    0,
                    "skipped (no ExternalMatchId).",
                    null);
            }

            if (string.IsNullOrWhiteSpace(match.ExternalStageId))
            {
                return (
                    SyncScorerStatuses.Skipped,
                    0,
                    "skipped (no ExternalStageId; map stage or re-sync calendar to persist IdStage).",
                    null);
            }

            try
            {
                ExternalMatchEvents events = await _eventsProvider.FetchGoalEventsAsync(
                    match.ExternalMatchId,
                    match.ExternalStageId,
                    cancellationToken);

                if (skipWhenNoTimelineGoals
                    && (events.Goals == null || events.Goals.Count == 0))
                {
                    return (
                        SyncScorerStatuses.Skipped,
                        0,
                        "skipped (no timeline Goal! events).",
                        null);
                }

                TimelineScorerApplyResult applyResult = await _scorerApplyService.ApplyAsync(
                    match.Id,
                    events,
                    homeMapsToTeamOne,
                    cancellationToken);

                if (applyResult.Applied)
                {
                    return (
                        SyncScorerStatuses.Applied,
                        applyResult.GoalsUpdated,
                        applyResult.Message,
                        applyResult.Warning);
                }

                if (applyResult.AlreadyMatched)
                {
                    return (
                        SyncScorerStatuses.AlreadyMatched,
                        applyResult.GoalsUpdated,
                        applyResult.Message,
                        applyResult.Warning);
                }

                return (
                    SyncScorerStatuses.Skipped,
                    applyResult.GoalsUpdated,
                    applyResult.Message,
                    applyResult.Warning);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return (
                    SyncScorerStatuses.Warning,
                    0,
                    "not applied (see warning).",
                    $"Scorer sync warning: {ex.Message}");
            }
        }

        private static string? MergeWarnings(string? existing, string? next)
        {
            if (string.IsNullOrWhiteSpace(next))
            {
                return existing;
            }

            if (string.IsNullOrWhiteSpace(existing))
            {
                return next;
            }

            return $"{existing} {next}";
        }

        private (int TeamOneScore, int TeamTwoScore, bool HomeMapsToTeamOne) OrientScoresToLocalSides(
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
                return (external.HomeScore, external.AwayScore, true);
            }

            if (homeIsTeamTwo && awayIsTeamOne)
            {
                return (external.AwayScore, external.HomeScore, false);
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
                // Equality only — substring contains ("Iran"/"Ukraine", "Niger"/"Nigeria") mis-maps sides.
                if (NormalizeName(candidate) == normalizedLocal)
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

            List<Player> teamOneScorers = GetSquadScorers(teamOneId);
            List<Player> teamTwoScorers = GetSquadScorers(teamTwoId);

            for (int index = 0; index < homeScore; index++)
            {
                Player teamOneScorer = await ResolveScorerAsync(teamOneId, teamOneScorers, index);
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
                Player teamTwoScorer = await ResolveScorerAsync(teamTwoId, teamTwoScorers, index);
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

        /// <summary>
        /// Prefers seeded squad forwards, then any non-placeholder player.
        /// Empty when the team only has (or lacks) Tournament Scorer placeholders.
        /// Excludes placeholder by name only — jersey 99 may be a real squad player.
        /// </summary>
        private List<Player> GetSquadScorers(int teamId)
        {
            int? forwardPositionId = _repository.PlayerPosition
                .Find(position => position.Name == ForwardPositionName)
                .Select(position => (int?)position.Id)
                .FirstOrDefault();

            List<Player> squad = _repository.Player
                .Find(player =>
                    player.TeamId == teamId
                    && player.Name != PlaceholderScorerName)
                .OrderBy(player => player.Number)
                .ToList();

            if (squad.Count == 0)
            {
                return squad;
            }

            if (!forwardPositionId.HasValue)
            {
                return squad;
            }

            List<Player> forwards = squad
                .Where(player => player.PositionId == forwardPositionId.Value)
                .ToList();
            return forwards.Count > 0 ? forwards : squad;
        }

        private async Task<Player> ResolveScorerAsync(int teamId, List<Player> squadScorers, int goalIndex)
        {
            if (squadScorers.Count > 0)
            {
                return squadScorers[goalIndex % squadScorers.Count];
            }

            return await EnsurePlaceholderScorerAsync(teamId);
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
