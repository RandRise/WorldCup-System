namespace Core.Options
{
    public class MatchResultSyncOptions
    {
        public const string SectionName = "MatchResultSync";

        /// <summary>Provider key. v1 supports "FifaCalendar".</summary>
        public string Provider { get; set; } = "FifaCalendar";

        public string BaseUrl { get; set; } = "https://api.fifa.com/api/v3/calendar/matches";

        /// <summary>
        /// FIFA timeline API root. Path appended:
        /// {IdCompetition}/{IdSeason}/{ExternalStageId}/{ExternalMatchId}?language=
        /// </summary>
        public string TimelineBaseUrl { get; set; } = "https://api.fifa.com/api/v3/timelines";

        /// <summary>
        /// Competition / season ids for FIFA calendar and timeline URLs.
        /// Timeline path: timelines/{IdCompetition}/{IdSeason}/{ExternalStageId}/{ExternalMatchId}.
        /// </summary>
        public string IdCompetition { get; set; } = "17";

        public string IdSeason { get; set; } = "285023";

        public string Language { get; set; } = "en";

        /// <summary>
        /// Pause between mapped matches in <c>SyncFinishedResults</c> and <c>SyncScorersForWorldCup</c>
        /// (FIFA public API courtesy). 0 disables the delay. Applies after each attempt, including failures.
        /// </summary>
        public int BatchDelayMilliseconds { get; set; } = 250;
    }
}
