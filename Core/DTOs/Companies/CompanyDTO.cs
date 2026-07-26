using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Companies
{
    public class CompanyDTO
    {
        public long Id { get; set; }
        public required string Name { get; set; }
        public string? Slug { get; set; }
        /// <summary>Present for Admin / CompanyAdmin of this company; otherwise null.</summary>
        public string? InviteCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }
    }

    public class CreateCompanyDTO
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(128, ErrorMessage = "Name cannot exceed 128 characters.")]
        public required string Name { get; set; }

        [StringLength(64, ErrorMessage = "Slug cannot exceed 64 characters.")]
        [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Slug must be lowercase URL-safe (a-z, 0-9, hyphens).")]
        public string? Slug { get; set; }
    }

    public class JoinCompanyDTO
    {
        [Required(ErrorMessage = "InviteCode is required.")]
        [StringLength(32, MinimumLength = 4, ErrorMessage = "InviteCode must be between 4 and 32 characters.")]
        public required string InviteCode { get; set; }
    }

    public class RotateInviteCodeDTO
    {
        /// <summary>Optional Admin override. CompanyAdmin always rotates their own company.</summary>
        [Range(1, long.MaxValue, ErrorMessage = "CompanyId must be a positive value.")]
        public long? CompanyId { get; set; }
    }

    public class CompanyMineDTO
    {
        public CompanyDTO? Company { get; set; }
        public bool IsCompanyAdmin { get; set; }
        public bool BecameCompanyAdmin { get; set; }
    }

    public class CompanyMemberDTO
    {
        public long Id { get; set; }
        public required string Name { get; set; }
        public string? Email { get; set; }
    }

    public class JoinCompanyResultDTO
    {
        public required CompanyDTO Company { get; set; }
        public bool BecameCompanyAdmin { get; set; }
        public bool IsCompanyAdmin { get; set; }
    }
}
