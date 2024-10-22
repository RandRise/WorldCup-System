namespace Data.Entities
{
    public class Stadium
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public int CityId { get; set; }

        public City City { get; set; }
        public ICollection<Match> Match { get; set; }


    }
}
