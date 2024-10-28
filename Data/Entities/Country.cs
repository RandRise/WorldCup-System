namespace Data.Entities
{
    public class Country
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public string? Flag { get; set; }

        public  ICollection<City> Cities { get; set; }
        public  Team Team { get; set; }

    }
}
