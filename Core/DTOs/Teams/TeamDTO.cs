using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Teams
{
    public class TeamDTO
    {
        public int Id { get; set; }
        public int CountryId { get; set; }
        public string? CountryName { get; set; }
        public int GroupId { get; set; }
        public string? GroupName { get; set; }
    }

    public class AddTeamDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "CountryId must be a positive value.")]
        public int CountryId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "GroupId must be a positive value.")]
        public int GroupId { get; set; }
    }

    public class UpdateTeamDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Id must be a positive value.")]
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CountryId must be a positive value.")]
        public int CountryId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "GroupId must be a positive value.")]
        public int GroupId { get; set; }
    }

    public class AssignTeamToGroupDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "TeamId must be a positive value.")]
        public int TeamId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "GroupId must be a positive value.")]
        public int GroupId { get; set; }
    }
}
