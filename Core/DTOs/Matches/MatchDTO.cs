using System.ComponentModel.DataAnnotations;
using Data.Entities;

namespace Core.DTOs.Matches
{
    public class MatchDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public MatchStage Stage { get; set; } = MatchStage.Group;
        public string StageName { get; set; } = "Group";
        public int StadiumId { get; set; }
        public string? StadiumName { get; set; }
        public int? TeamOneId { get; set; }
        public string? TeamOneName { get; set; }
        public int? TeamTwoId { get; set; }
        public string? TeamTwoName { get; set; }
        public int? FeederMatchOneId { get; set; }
        public int? FeederMatchTwoId { get; set; }
        public int TeamOneScore { get; set; }
        public int TeamTwoScore { get; set; }
        public string Status { get; set; } = "Scheduled";
        public bool CanBet { get; set; }
        public string? ExternalMatchId { get; set; }
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

        [EnumDataType(typeof(MatchStage), ErrorMessage = "Stage must be a valid match stage.")]
        public MatchStage Stage { get; set; } = MatchStage.Group;
    }

    public class LiveMatchSnapshotDTO
    {
        public int MatchId { get; set; }
        public string Status { get; set; } = "Scheduled";
        public int TeamOneScore { get; set; }
        public int TeamTwoScore { get; set; }
        public int? CurrentMinute { get; set; }
    }

    public class LiveEventSnapshotDTO
    {
        public int MatchId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public int Minute { get; set; }
        public string? PlayerName { get; set; }
        public string? TeamName { get; set; }
    }

    public class LiveSnapshotDTO
    {
        public List<LiveMatchSnapshotDTO> Matches { get; set; } = new();
        public List<LiveEventSnapshotDTO> RecentEvents { get; set; } = new();
    }

    public class UpdateMatchDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Id must be a positive value.")]
        public int Id { get; set; }

        /// <summary>Null keeps a knockout TBD slot empty. Group stage requires both teams.</summary>
        [Range(1, int.MaxValue, ErrorMessage = "TeamOneId must be a positive value when set.")]
        public int? TeamOneId { get; set; }

        /// <summary>Null keeps a knockout TBD slot empty. Group stage requires both teams.</summary>
        [Range(1, int.MaxValue, ErrorMessage = "TeamTwoId must be a positive value when set.")]
        public int? TeamTwoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "StadiumId must be a positive value.")]
        public int StadiumId { get; set; }

        [Required(ErrorMessage = "Date is required.")]
        public DateTime Date { get; set; }

        [EnumDataType(typeof(MatchStage), ErrorMessage = "Stage must be a valid match stage.")]
        public MatchStage Stage { get; set; } = MatchStage.Group;
    }
}
