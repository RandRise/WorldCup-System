namespace Core.Services.MatchSync
{
    /// <summary>
    /// Rewrites existing Goal rows with real FIFA timeline scorers when goal counts align.
    /// Does not change FT score totals, TeamStats points, bets, or knockout advancement.
    /// </summary>
    public interface ITimelineScorerApplyService
    {
        /// <summary>
        /// Idempotently update <c>PlayerId</c>, <c>TimeScored</c>, and <c>IsOwnGoal</c> on existing Goal rows
        /// when timeline home/away goal counts match existing Goal counts (after orientation).
        /// When counts disagree, throws — leaves Goal rows unchanged (calendar FT stays authoritative).
        /// </summary>
        /// <param name="matchId">Local match id.</param>
        /// <param name="events">Parsed timeline goal events (must already be fetched).</param>
        /// <param name="homeMapsToTeamOne">
        /// True when FIFA home side is local <c>TeamOne</c> (same orientation as Phase 10 calendar sync).
        /// </param>
        Task<TimelineScorerApplyResult> ApplyAsync(
            int matchId,
            ExternalMatchEvents events,
            bool homeMapsToTeamOne,
            CancellationToken cancellationToken = default);
    }
}
