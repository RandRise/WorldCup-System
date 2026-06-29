using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Coaches
{
    public class CoachDTO
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int TeamId { get; set; }
    }

    public class AddCoachDTO
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(40, ErrorMessage = "Name cannot exceed 40 characters.")]
        public required string Name { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "TeamId must be a positive value.")]
        public int TeamId { get; set; }
    }

    public class UpdateCoachDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Id must be a positive value.")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(40, ErrorMessage = "Name cannot exceed 40 characters.")]
        public required string Name { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "TeamId must be a positive value.")]
        public int TeamId { get; set; }
    }
}
