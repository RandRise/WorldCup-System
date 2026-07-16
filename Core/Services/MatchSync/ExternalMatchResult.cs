namespace Core.Services.MatchSync
{
    public class ExternalMatchResult
    {
        public required string ExternalMatchId { get; init; }
        public int HomeScore { get; init; }
        public int AwayScore { get; init; }
        public bool IsFinished { get; init; }
        public string? StatusLabel { get; init; }
        public string? HomeTeamName { get; init; }
        public string? AwayTeamName { get; init; }
        public string? HomeTeamCountryCode { get; init; }
        public string? AwayTeamCountryCode { get; init; }
    }
}
