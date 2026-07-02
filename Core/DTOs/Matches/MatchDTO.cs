using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Matches
{
    public class MatchDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int StadiumId { get; set; }
        public string? StadiumName { get; set; }
        public int TeamOneId { get; set; }
        public string? TeamOneName { get; set; }
        public int TeamTwoId { get; set; }
        public string? TeamTwoName { get; set; }
        public int TeamOneScore { get; set; }
        public int TeamTwoScore { get; set; }
        public string Status { get; set; } = "Scheduled";
        public bool CanBet { get; set; }
    }

    public class MatchDetailDTO : MatchDTO
    {
        public MatchTeamStatsDTO? TeamOneStats { get; set; }
        public MatchTeamStatsDTO? TeamTwoStats { get; set; }
    }

    public class MatchTeamStatsDTO
    {
        public int TeamStatsId { get; set; }
        public int TeamId { get; set; }
        public string? TeamName { get; set; }
        public int Possession { get; set; }
        public int Shots { get; set; }
        public int ShotsOnTarget { get; set; }
        public int Score { get; set; }
        public List<MatchGoalDTO> Goals { get; set; } = new();
        public List<MatchCardDTO> Cards { get; set; } = new();
    }

    public class MatchGoalDTO
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public string? PlayerName { get; set; }
        public int Minute { get; set; }
        public bool IsOwnGoal { get; set; }
    }

    public class MatchCardDTO
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public string? PlayerName { get; set; }
        public int Minute { get; set; }
        public string? Type { get; set; }
    }

    public class AddMatchDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "TeamOneId must be a positive value.")]
        public int TeamOneId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "TeamTwoId must be a positive value.")]
        public int TeamTwoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "StadiumId must be a positive value.")]
        public int StadiumId { get; set; }

        [Required(ErrorMessage = "Date is required.")]
        public DateTime Date { get; set; }
    }

    public class UpdateMatchDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Id must be a positive value.")]
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "TeamOneId must be a positive value.")]
        public int TeamOneId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "TeamTwoId must be a positive value.")]
        public int TeamTwoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "StadiumId must be a positive value.")]
        public int StadiumId { get; set; }

        [Required(ErrorMessage = "Date is required.")]
        public DateTime Date { get; set; }
    }
}
