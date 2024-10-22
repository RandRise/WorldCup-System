namespace Data.Entities
{
    public class Country
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public string? Flag { get; set; }

        public required ICollection<City> Cities { get; set; }
        public required Team Team { get; set; }

    }
}
