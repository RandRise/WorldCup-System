namespace Core.DTOs.Cities
{
    public class CityDTO
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int CountryId { get; set; }
    }
}
