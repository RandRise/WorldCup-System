namespace Core.DTOs.Seeding
{
    public class DemoSeedResultDTO
    {
        public int CountriesAdded { get; set; }
        public int CitiesAdded { get; set; }
        public int StadiumsAdded { get; set; }
        public int WorldCupId { get; set; }
        public int GroupsAdded { get; set; }
        public int TeamsAdded { get; set; }
        public bool AlreadySeeded { get; set; }
        public required string Message { get; set; }
    }
}
