namespace Core.DTOs.Seeding
{
    public class TournamentDataResetResultDTO
    {
        public int BetResultsRemoved { get; set; }
        public int BetsRemoved { get; set; }
        public int GoalsRemoved { get; set; }
        public int CardsRemoved { get; set; }
        public int TeamStatsRemoved { get; set; }
        public int MatchesRemoved { get; set; }
        public int PlayersRemoved { get; set; }
        public int CoachesRemoved { get; set; }
        public int TeamsRemoved { get; set; }
        public int GroupsRemoved { get; set; }
        public int WorldCupsRemoved { get; set; }
        public int StadiumsRemoved { get; set; }
        public int CitiesRemoved { get; set; }
        public int CountriesRemoved { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
