using Microsoft.AspNetCore.Http;
namespace Core.DTOs.Cities
{
    public class CityDTO
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int CountryId { get; set; }

    }
    public class UploadCityDto
    {
        public IFormFile? File { get; set; }
        public int CountryId { get; set; }
    }
}
