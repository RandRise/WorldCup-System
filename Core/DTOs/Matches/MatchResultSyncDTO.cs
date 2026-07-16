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
    }

    public class ExternalMatchResultDTO
    {
        public string ExternalMatchId { get; set; } = string.Empty;
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public bool IsFinished { get; set; }
        public string? StatusLabel { get; set; }
    }

    public class SyncMatchResultDTO
    {
        public int MatchId { get; set; }
        public string? ExternalMatchId { get; set; }
        public bool Applied { get; set; }
        public bool ScoreChanged { get; set; }
        public int TeamOneScore { get; set; }
        public int TeamTwoScore { get; set; }
        public int BetsResolved { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Warning { get; set; }
        public ResolveBetsResultDTO? ResolveResult { get; set; }
    }

    public class SyncFinishedResultsDTO
    {
        public int WorldCupId { get; set; }
        public int MatchesAttempted { get; set; }
        public int MatchesApplied { get; set; }
        public int TotalBetsResolved { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<SyncMatchResultDTO> Results { get; set; } = new();
    }
}
