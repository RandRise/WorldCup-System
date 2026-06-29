using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Players
{
    public class PlayerDTO
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int Number { get; set; }
        public int TeamId { get; set; }
        public int PositionId { get; set; }
        public string? PositionName { get; set; }
    }

    public class AddPlayerDTO
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(64, ErrorMessage = "Name cannot exceed 64 characters.")]
        public required string Name { get; set; }

        [Range(1, 99, ErrorMessage = "Number must be between 1 and 99.")]
        public int Number { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "TeamId must be a positive value.")]
        public int TeamId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "PositionId must be a positive value.")]
        public int PositionId { get; set; }
    }

    public class UpdatePlayerDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Id must be a positive value.")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(64, ErrorMessage = "Name cannot exceed 64 characters.")]
        public required string Name { get; set; }

        [Range(1, 99, ErrorMessage = "Number must be between 1 and 99.")]
        public int Number { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "TeamId must be a positive value.")]
        public int TeamId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "PositionId must be a positive value.")]
        public int PositionId { get; set; }
    }
}
