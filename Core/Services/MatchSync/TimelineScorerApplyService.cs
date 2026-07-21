using Data.Entities;
using Data.Repos;

namespace Core.Services.MatchSync
{
    /// <summary>
    /// In-place Goal attribution from FIFA timeline events when per-side counts match FT Goal rows.
    /// Count mismatch fails hard (no Goal changes). Bet resolve / knockout are intentionally not invoked.
    /// </summary>
    public class TimelineScorerApplyService : ITimelineScorerApplyService
    {
        private readonly IRepositoryManager _repository;
        private readonly ITimelinePlayerResolver _playerResolver;

        public TimelineScorerApplyService(
            IRepositoryManager repository,
            ITimelinePlayerResolver playerResolver)
        {
            _repository = repository;
            _playerResolver = playerResolver;
        }

        public async Task<TimelineScorerApplyResult> ApplyAsync(
            int matchId,
            ExternalMatchEvents events,
            bool homeMapsToTeamOne,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(events);

            Match match = await _repository.Match.GetByIdAsync(matchId);
            if (!match.TeamOneId.HasValue || !match.TeamTwoId.HasValue)
            {
                throw new InvalidOperationException(
                    $"Cannot apply timeline scorers for match {matchId} until both teams are set.");
            }

            if (!string.IsNullOrWhiteSpace(match.ExternalMatchId)
                && !string.IsNullOrWhiteSpace(events.ExternalMatchId)
                && !string.Equals(
                    match.ExternalMatchId.Trim(),
                    events.ExternalMatchId.Trim(),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Timeline ExternalMatchId '{events.ExternalMatchId}' does not match " +
                    $"match {matchId} mapping '{match.ExternalMatchId}'.");
            }

            int teamOneId = match.TeamOneId.Value;
            int teamTwoId = match.TeamTwoId.Value;

            TeamStats teamOneStats = RequireTeamStats(matchId, teamOneId);
            TeamStats teamTwoStats = RequireTeamStats(matchId, teamTwoId);

            List<int> statIds = new List<int> { teamOneStats.Id, teamTwoStats.Id };
            List<Goal> existingGoals = _repository.Goal
                .Find(goal => statIds.Contains(goal.TeamStatsId))
                .ToList();

            List<Goal> teamOneGoals = existingGoals
                .Where(goal => goal.TeamStatsId == teamOneStats.Id)
                .OrderBy(goal => goal.TimeScored)
                .ThenBy(goal => goal.Id)
                .ToList();
            List<Goal> teamTwoGoals = existingGoals
                .Where(goal => goal.TeamStatsId == teamTwoStats.Id)
                .OrderBy(goal => goal.TimeScored)
                .ThenBy(goal => goal.Id)
                .ToList();

            IReadOnlyList<ExternalMatchGoalEvent> allEvents =
                events.Goals ?? Array.Empty<ExternalMatchGoalEvent>();

            List<ExternalMatchGoalEvent> teamOneEvents = FilterEventsForLocalSide(
                allEvents,
                homeMapsToTeamOne,
                forTeamOne: true);
            List<ExternalMatchGoalEvent> teamTwoEvents = FilterEventsForLocalSide(
                allEvents,
                homeMapsToTeamOne,
                forTeamOne: false);

            if (teamOneEvents.Count != teamOneGoals.Count || teamTwoEvents.Count != teamTwoGoals.Count)
            {
                throw new InvalidOperationException(
                    $"Timeline goal counts do not match existing Goal rows for match {matchId}. " +
                    $"Timeline TeamOne/TeamTwo={teamOneEvents.Count}/{teamTwoEvents.Count}; " +
                    $"existing={teamOneGoals.Count}/{teamTwoGoals.Count}. " +
                    "Leaving scorers unchanged (calendar FT remains authoritative).");
            }

            if (teamOneGoals.Count == 0 && teamTwoGoals.Count == 0)
            {
                return new TimelineScorerApplyResult
                {
                    MatchId = matchId,
                    Applied = false,
                    AlreadyMatched = true,
                    GoalsUpdated = 0,
                    Message = $"No goals to attribute for match {matchId}."
                };
            }

            List<string> warnings = new List<string>();
            int goalsUpdated = 0;

            goalsUpdated += await ApplySideAsync(
                match,
                teamOneGoals,
                teamOneEvents,
                creditedTeamId: teamOneId,
                opponentTeamId: teamTwoId,
                warnings,
                cancellationToken);

            goalsUpdated += await ApplySideAsync(
                match,
                teamTwoGoals,
                teamTwoEvents,
                creditedTeamId: teamTwoId,
                opponentTeamId: teamOneId,
                warnings,
                cancellationToken);

            if (goalsUpdated > 0)
            {
                await _repository.SaveAsync();
            }

            string? warning = warnings.Count == 0
                ? null
                : string.Join("; ", warnings.Distinct(StringComparer.Ordinal));

            if (goalsUpdated == 0)
            {
                return new TimelineScorerApplyResult
                {
                    MatchId = matchId,
                    Applied = false,
                    AlreadyMatched = true,
                    GoalsUpdated = 0,
                    Message = $"Scorers already match timeline for match {matchId}.",
                    Warning = warning
                };
            }

            return new TimelineScorerApplyResult
            {
                MatchId = matchId,
                Applied = true,
                AlreadyMatched = false,
                GoalsUpdated = goalsUpdated,
                Message = $"Updated {goalsUpdated} goal scorer(s) from timeline for match {matchId}.",
                Warning = warning
            };
        }

        private async Task<int> ApplySideAsync(
            Match match,
            List<Goal> existingGoals,
            List<ExternalMatchGoalEvent> timelineEvents,
            int creditedTeamId,
            int opponentTeamId,
            List<string> warnings,
            CancellationToken cancellationToken)
        {
            int updated = 0;

            for (int index = 0; index < existingGoals.Count; index++)
            {
                Goal goal = existingGoals[index];
                ExternalMatchGoalEvent timelineEvent = timelineEvents[index];

                int playerTeamId = timelineEvent.IsOwnGoal ? opponentTeamId : creditedTeamId;
                TimelinePlayerResolveResult resolveResult = await _playerResolver.ResolveAsync(
                    playerTeamId,
                    timelineEvent.ExternalPlayerId,
                    timelineEvent.PlayerDisplayName,
                    cancellationToken);

                if (!string.IsNullOrWhiteSpace(resolveResult.Warning))
                {
                    warnings.Add(resolveResult.Warning);
                }

                int minute = Math.Max(0, timelineEvent.Minute);
                DateTime timeScored = match.Date.AddMinutes(minute);
                int isOwnGoal = timelineEvent.IsOwnGoal ? 1 : 0;

                bool changed =
                    goal.PlayerId != resolveResult.Player.Id
                    || goal.TimeScored != timeScored
                    || goal.IsOwnGoal != isOwnGoal;

                if (!changed)
                {
                    continue;
                }

                goal.PlayerId = resolveResult.Player.Id;
                goal.TimeScored = timeScored;
                goal.IsOwnGoal = isOwnGoal;
                _repository.Goal.Update(goal);
                updated++;
            }

            return updated;
        }

        /// <summary>
        /// Keep FIFA timeline order (provider already walks the Event array chronologically).
        /// Do not re-sort by player id/name — same-minute goals must stay paired with Goal rows by index.
        /// </summary>
        private static List<ExternalMatchGoalEvent> FilterEventsForLocalSide(
            IReadOnlyList<ExternalMatchGoalEvent> allEvents,
            bool homeMapsToTeamOne,
            bool forTeamOne)
        {
            bool wantHomeSide = forTeamOne ? homeMapsToTeamOne : !homeMapsToTeamOne;

            return allEvents
                .Where(goalEvent => goalEvent.IsHomeSide == wantHomeSide)
                .ToList();
        }

        private TeamStats RequireTeamStats(int matchId, int teamId)
        {
            TeamStats? stats = _repository.TeamStats
                .Find(teamStats => teamStats.MatchId == matchId && teamStats.TeamId == teamId)
                .FirstOrDefault();
            if (stats == null)
            {
                throw new InvalidOperationException(
                    $"TeamStats missing for match {matchId} team {teamId}. Apply FT score sync before scorers.");
            }

            return stats;
        }
    }
}
