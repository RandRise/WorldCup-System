using Core.DTOs.Companies;

namespace Core.Services.Companies
{
    public interface ICompanyService
    {
        Task<CompanyDTO> CreateAsync(CreateCompanyDTO dto, long createdByUserId);
        Task<JoinCompanyResultDTO> JoinAsync(JoinCompanyDTO dto, long userId);
        Task<CompanyMineDTO> GetMineAsync(long userId);
        Task<CompanyDTO> RotateInviteCodeAsync(long userId, bool isPlatformAdmin, long? companyIdOverride);
        Task<List<CompanyMemberDTO>> GetMembersAsync(long userId, bool isPlatformAdmin, long? companyIdOverride);
        Task<List<CompanyDTO>> GetAllAsync();
    }
}
