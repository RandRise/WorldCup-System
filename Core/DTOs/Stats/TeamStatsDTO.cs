using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Stats
{
    public class TeamStatsDTO
    {
        public int TeamStatsId { get; set; }
        public int MatchId { get; set; }
        public int TeamId { get; set; }
        public string? TeamName { get; set; }
        public int Possession { get; set; }
        public int Shots { get; set; }
        public int ShotsOnTarget { get; set; }
        public int Score { get; set; }
    }

    public class UpdateTeamStatsDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "MatchId must be a positive value.")]
        public int MatchId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "TeamId must be a positive value.")]
        public int TeamId { get; set; }

        [Range(0, 100, ErrorMessage = "Possession must be between 0 and 100.")]
        public int Possession { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Shots cannot be negative.")]
        public int Shots { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "ShotsOnTarget cannot be negative.")]
        public int ShotsOnTarget { get; set; }
    }
}
