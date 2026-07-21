using System.ComponentModel.DataAnnotations;
using Core.DTOs.Bets;

namespace Core.DTOs.Matches
{
    public class SetExternalMatchIdDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "MatchId must be a positive value.")]
        public int MatchId { get; set; }

        [Required(ErrorMessage = "ExternalMatchId is required.")]
        [MaxLength(64, ErrorMessage = "ExternalMatchId must be at most 64 characters.")]
        public string ExternalMatchId { get; set; } = string.Empty;

        /// <summary>Optional FIFA IdStage for timeline URLs. When omitted, existing ExternalStageId is kept.</summary>
        [MaxLength(64, ErrorMessage = "ExternalStageId must be at most 64 characters.")]
        public string? ExternalStageId { get; set; }
    }

    public class ExternalMatchResultDTO
    {
        public string ExternalMatchId { get; set; } = string.Empty;
        public string? ExternalStageId { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public bool IsFinished { get; set; }
        public string? StatusLabel { get; set; }
    }

    /// <summary>Scorer step outcomes on <see cref="SyncMatchResultDTO.ScorerStatus"/>.</summary>
    public static class SyncScorerStatuses
    {
        public const string Applied = "Applied";
        public const string AlreadyMatched = "AlreadyMatched";
        public const string Skipped = "Skipped";
        public const string Warning = "Warning";
    }

    public class SyncMatchResultDTO
    {
        public int MatchId { get; set; }
        public string? ExternalMatchId { get; set; }
        public string? ExternalStageId { get; set; }
        public bool Applied { get; set; }
        public bool ScoreChanged { get; set; }
        public int TeamOneScore { get; set; }
        public int TeamTwoScore { get; set; }
        public int BetsResolved { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Warning { get; set; }
        public ResolveBetsResultDTO? ResolveResult { get; set; }

        /// <summary>
        /// Timeline scorer step: <see cref="SyncScorerStatuses.Applied"/>,
        /// <see cref="SyncScorerStatuses.AlreadyMatched"/>,
        /// <see cref="SyncScorerStatuses.Skipped"/>,
        /// <see cref="SyncScorerStatuses.Warning"/>,
        /// or null when scorers were not attempted (e.g. external not finished).
        /// </summary>
        public string? ScorerStatus { get; set; }

        public int ScorerGoalsUpdated { get; set; }

        public string? ScorerMessage { get; set; }
    }

    public class SyncFinishedResultsDTO
    {
        public int WorldCupId { get; set; }
        public int MatchesAttempted { get; set; }
        public int MatchesApplied { get; set; }
        public int TotalBetsResolved { get; set; }
        public int ScorersApplied { get; set; }
        public int ScorerWarnings { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<SyncMatchResultDTO> Results { get; set; } = new();
    }
}
