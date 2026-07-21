namespace Core.Services.MatchSync
{
    /// <summary>
    /// Maps a FIFA timeline goalscorer (IdPlayer + display name) to a local squad <c>Player</c>.
    /// Caller chooses the squad team: scoring side for regular goals; conceding side for own goals.
    /// </summary>
    public interface ITimelinePlayerResolver
    {
        /// <summary>
        /// Resolve order: ExternalPlayerId → exact name → normalized name → create named player.
        /// Never returns the Tournament Scorer placeholder when a real display name is available.
        /// Ambiguous normalized matches throw; they are not randomly assigned.
        /// </summary>
        Task<TimelinePlayerResolveResult> ResolveAsync(
            int teamId,
            string? externalPlayerId,
            string? playerDisplayName,
            CancellationToken cancellationToken = default);
    }
}
