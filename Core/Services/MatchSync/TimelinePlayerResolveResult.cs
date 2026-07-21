using Data.Entities;

namespace Core.Services.MatchSync
{
    /// <summary>Outcome of resolving a FIFA timeline scorer to a local <see cref="Player"/>.</summary>
    public class TimelinePlayerResolveResult
    {
        public required Player Player { get; init; }

        public TimelinePlayerMatchMethod MatchMethod { get; init; }

        /// <summary>Non-fatal note (e.g. ExternalPlayerId backfilled onto a name match).</summary>
        public string? Warning { get; init; }

        public bool WasCreated => MatchMethod == TimelinePlayerMatchMethod.Created;
    }
}
