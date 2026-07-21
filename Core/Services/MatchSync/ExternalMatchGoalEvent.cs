namespace Core.Services.MatchSync
{
    /// <summary>
    /// One goalscorer event from an external timeline feed (FIFA Goal! / Own Goal / Penalty Goal).
    /// </summary>
    public class ExternalMatchGoalEvent
    {
        public required string ExternalMatchId { get; init; }

        /// <summary>FIFA IdTeam of the player who scored (own-goal scorer is on the conceding side).</summary>
        public string? ExternalTeamId { get; init; }

        /// <summary>FIFA IdPlayer when present.</summary>
        public string? ExternalPlayerId { get; init; }

        /// <summary>Display name extracted from the event description (e.g. "Julian QUINONES").</summary>
        public string? PlayerDisplayName { get; init; }

        /// <summary>Minute offset from kickoff for TimeScored (e.g. 9 from "9'", 92 from "90'+2'").</summary>
        public int Minute { get; init; }

        /// <summary>True when the event increased the home side's FT score.</summary>
        public bool IsHomeSide { get; init; }

        public bool IsOwnGoal { get; init; }

        /// <summary>True when TypeLocalized indicates a penalty goal (still counts as a goal).</summary>
        public bool IsPenalty { get; init; }

        /// <summary>Raw TypeLocalized description (e.g. "Goal!").</summary>
        public string? EventTypeLabel { get; init; }
    }
}
