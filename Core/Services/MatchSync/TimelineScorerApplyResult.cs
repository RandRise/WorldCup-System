namespace Core.Services.MatchSync
{
    /// <summary>Outcome of applying FIFA timeline scorers onto existing Goal rows.</summary>
    public class TimelineScorerApplyResult
    {
        public int MatchId { get; init; }

        /// <summary>True when at least one Goal row was updated.</summary>
        public bool Applied { get; init; }

        /// <summary>True when counts aligned and PlayerId / minute / own-goal already matched timeline.</summary>
        public bool AlreadyMatched { get; init; }

        public int GoalsUpdated { get; init; }

        public required string Message { get; init; }

        /// <summary>Non-fatal notes (e.g. player ExternalPlayerId backfill warnings).</summary>
        public string? Warning { get; init; }
    }
}
