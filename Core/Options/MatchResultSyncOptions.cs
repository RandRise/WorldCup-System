namespace Core.Options
{
    public class MatchResultSyncOptions
    {
        public const string SectionName = "MatchResultSync";

        /// <summary>Provider key. v1 supports "FifaCalendar".</summary>
        public string Provider { get; set; } = "FifaCalendar";

        public string BaseUrl { get; set; } = "https://api.fifa.com/api/v3/calendar/matches";

        public string IdCompetition { get; set; } = "17";

        public string IdSeason { get; set; } = "285023";

        public string Language { get; set; } = "en";
    }
}
