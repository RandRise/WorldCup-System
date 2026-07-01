using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Bets
{
    public class BetDTO
    {
        public int Id { get; set; }
        public long UserId { get; set; }
        public int MatchId { get; set; }
        public DateTime MatchDate { get; set; }
        public string? TeamOneName { get; set; }
        public string? TeamTwoName { get; set; }
        public bool IsDraw { get; set; }
        public int? PredictedTeamId { get; set; }
        public string? PredictedOutcome { get; set; }
        public bool IsResolved { get; set; }
        public int? PointsEarned { get; set; }
        public bool IsActive { get; set; }
    }

    public class PlaceBetDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "MatchId must be a positive value.")]
        public int MatchId { get; set; }

        public bool IsDraw { get; set; }

        public int? TeamId { get; set; }
    }

    public class LeaderboardEntryDTO
    {
        public int Rank { get; set; }
        public long UserId { get; set; }
        public string? UserName { get; set; }
        public int TotalPoints { get; set; }
        public int ResolvedBets { get; set; }
    }

    public class BettingRuleDTO
    {
        public required string Rule { get; set; }
        public required string Description { get; set; }
    }

    public class BettingRulesResponseDTO
    {
        public required string Summary { get; set; }
        public required IReadOnlyList<BettingRuleDTO> Rules { get; set; }
    }
}
