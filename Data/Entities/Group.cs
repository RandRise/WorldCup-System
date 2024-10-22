namespace Data.Entities
{
    public class Group
    {
        public int Id { get; set; }

        public required string Name { get; set; }
        public int WorldCupId { get; set; }
        public WorldCup WorldCup { get; set; }
        public required ICollection<Team> Teams { get; set; }
    }
}
