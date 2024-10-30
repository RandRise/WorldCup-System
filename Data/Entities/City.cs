namespace Data.Entities
{
    public class City
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public int CountryId { get; set; }

        public  Country Country { get; set; }

        public ICollection<Stadium> Stadiums { get; set; } = [];

    }
}
