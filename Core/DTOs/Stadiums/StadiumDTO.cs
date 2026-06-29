using Microsoft.AspNetCore.Http;

namespace Core.DTOs.Stadiums
{
    public class StadiumDTO
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int CityId { get; set; }
    }
    public class UpdateStadiumDto : StadiumDTO
    {
        public int Id { get; set; }
    }

    public class UploadStadiumDto
    {
        public IFormFile? File { get; set; }
    }
}
