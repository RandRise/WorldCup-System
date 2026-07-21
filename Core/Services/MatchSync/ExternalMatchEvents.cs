namespace Core.Services.MatchSync
{
    /// <summary>
    /// Timeline goal events for one external match. Score sync is separate (calendar provider).
    /// </summary>
    public class ExternalMatchEvents
    {
        public required string ExternalMatchId { get; init; }

        public string? ExternalStageId { get; init; }

        public IReadOnlyList<ExternalMatchGoalEvent> Goals { get; init; } = Array.Empty<ExternalMatchGoalEvent>();
    }
}
