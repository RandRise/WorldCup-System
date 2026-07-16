using System.ComponentModel.DataAnnotations;
using Data.Entities;

namespace Core.DTOs.Matches
{
    public class BracketDTO
    {
        public int WorldCupId { get; set; }
        public List<BracketRoundDTO> Rounds { get; set; } = new();
    }

    public class BracketRoundDTO
    {
        public MatchStage Stage { get; set; }
        public string StageName { get; set; } = string.Empty;
        public List<MatchDTO> Matches { get; set; } = new();
    }

    public class GenerateBracketDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "WorldCupId must be a positive value.")]
        public int WorldCupId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "StadiumId must be a positive value.")]
        public int StadiumId { get; set; }

        [Required(ErrorMessage = "FirstKickoff is required.")]
        public DateTime FirstKickoff { get; set; }
    }

    public class GenerateBracketResultDTO
    {
        public int MatchesCreated { get; set; }
        public string Message { get; set; } = string.Empty;
        public BracketDTO Bracket { get; set; } = new();
    }
}
