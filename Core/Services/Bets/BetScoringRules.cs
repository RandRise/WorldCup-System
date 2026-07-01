using Core.DTOs.Bets;

namespace Core.Services.Bets
{
    /// <summary>
    /// WorldCup prediction pool scoring rules.
    /// Users pick a team win or a draw before 90-minute kickoff.
    /// </summary>
    public static class BetScoringRules
    {
        public const int CorrectOutcomePoints = 3;
        public const int IncorrectPredictionPoints = 0;
        public const int MatchDurationMinutes = 90;

        public const string DrawOutcomeLabel = "Draw";

        public const string Summary =
            "Pick the match result before kickoff: either team to win or a draw after 90 minutes. " +
            "A correct prediction earns 3 points; any incorrect prediction earns 0 points. " +
            "One bet per user per match.";

        public static IReadOnlyList<BettingRuleDTO> GetRules()
        {
            return new List<BettingRuleDTO>
            {
                new BettingRuleDTO
                {
                    Rule = "One bet per user per match",
                    Description = "Each user may place exactly one prediction per scheduled match."
                },
                new BettingRuleDTO
                {
                    Rule = "Bet before kickoff",
                    Description = "Predictions must be submitted before the match start time."
                },
                new BettingRuleDTO
                {
                    Rule = "Pick a team win or a draw",
                    Description = "Predict either participating team to win, or a draw after 90 minutes."
                },
                new BettingRuleDTO
                {
                    Rule = $"Correct outcome: {CorrectOutcomePoints} points",
                    Description = "If the final result matches your prediction, you earn 3 points."
                },
                new BettingRuleDTO
                {
                    Rule = $"Incorrect prediction: {IncorrectPredictionPoints} points",
                    Description = "If the final result does not match your prediction, you earn 0 points."
                },
                new BettingRuleDTO
                {
                    Rule = "Resolution after full time",
                    Description = $"Bets are scored only after {MatchDurationMinutes} minutes of play have elapsed and the final result is known from recorded goals."
                }
            };
        }
    }
}
