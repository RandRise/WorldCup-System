using Microsoft.AspNetCore.Http;

namespace Core.DTOs.Countries
{
    public class CountryDTO
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? flag { get; set; }
    }
    public class UploadCountryDto
    {
        public IFormFile? File { get; set; }
    }
}
