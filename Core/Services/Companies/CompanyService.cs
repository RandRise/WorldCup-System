using System.Security.Cryptography;
using Core.DTOs.Companies;
using Data.Entities;
using Data.Repos;
using Microsoft.AspNetCore.Identity;

namespace Core.Services.Companies
{
    public class CompanyService : ICompanyService
    {
        private const string CompanyAdminRole = "CompanyAdmin";
        private const int InviteCodeLength = 8;
        private const string InviteCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        private readonly IRepositoryManager _repository;
        private readonly UserManager<User> _userManager;

        public CompanyService(IRepositoryManager repository, UserManager<User> userManager)
        {
            _repository = repository;
            _userManager = userManager;
        }

        public async Task<CompanyDTO> CreateAsync(CreateCompanyDTO dto, long createdByUserId)
        {
            string name = dto.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name is required.");
            }

            string? slug = NormalizeSlug(dto.Slug);
            if (slug != null)
            {
                bool slugTaken = _repository.Company
                    .Find(company => company.Slug == slug)
                    .Any();
                if (slugTaken)
                {
                    throw new InvalidOperationException($"Slug '{slug}' is already in use.");
                }
            }

            string inviteCode = GenerateUniqueInviteCode();

            Company company = new Company
            {
                Name = name,
                Slug = slug,
                InviteCode = inviteCode,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            };

            _repository.Company.Create(company);
            await _repository.SaveAsync();

            return ToCompanyDto(company, memberCount: 0, includeInviteCode: true);
        }

        public async Task<JoinCompanyResultDTO> JoinAsync(JoinCompanyDTO dto, long userId)
        {
            User user = await GetUserOrThrowAsync(userId);

            if (user.CompanyId.HasValue)
            {
                throw new InvalidOperationException(
                    "You are already a member of a company. Leave is not supported in v1.");
            }

            string inviteCode = NormalizeInviteCode(dto.InviteCode);
            Company? company = _repository.Company
                .Find(c => c.InviteCode == inviteCode)
                .FirstOrDefault();

            if (company == null)
            {
                throw new KeyNotFoundException("Invite code was not found.");
            }

            int memberCountBefore = _repository.User
                .Find(u => u.CompanyId == company.Id)
                .Count();

            bool becameCompanyAdmin = false;
            bool roleAddedThisJoin = false;

            if (memberCountBefore == 0)
            {
                // Assign CompanyAdmin before membership so a failed role add cannot leave
                // a company with members but no admin.
                if (!await _userManager.IsInRoleAsync(user, CompanyAdminRole))
                {
                    IdentityResult roleResult = await _userManager.AddToRoleAsync(user, CompanyAdminRole);
                    if (!roleResult.Succeeded)
                    {
                        string errors = string.Join("; ", roleResult.Errors.Select(error => error.Description));
                        throw new InvalidOperationException($"Failed to assign CompanyAdmin: {errors}");
                    }

                    roleAddedThisJoin = true;
                }

                becameCompanyAdmin = true;
            }
            else if (await _userManager.IsInRoleAsync(user, CompanyAdminRole))
            {
                // Strip orphaned CompanyAdmin so a non-first joiner cannot manage this company.
                IdentityResult removeRoleResult = await _userManager.RemoveFromRoleAsync(user, CompanyAdminRole);
                if (!removeRoleResult.Succeeded)
                {
                    string errors = string.Join("; ", removeRoleResult.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"Failed to clear orphaned CompanyAdmin: {errors}");
                }
            }

            user.CompanyId = company.Id;
            IdentityResult updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                if (roleAddedThisJoin)
                {
                    await _userManager.RemoveFromRoleAsync(user, CompanyAdminRole);
                }

                string errors = string.Join("; ", updateResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to join company: {errors}");
            }

            int memberCount = memberCountBefore + 1;

            return new JoinCompanyResultDTO
            {
                Company = ToCompanyDto(company, memberCount, includeInviteCode: becameCompanyAdmin),
                BecameCompanyAdmin = becameCompanyAdmin,
                IsCompanyAdmin = becameCompanyAdmin
            };
        }

        public async Task<CompanyMineDTO> GetMineAsync(long userId)
        {
            User user = await GetUserOrThrowAsync(userId);
            bool isCompanyAdmin = await _userManager.IsInRoleAsync(user, CompanyAdminRole);

            if (!user.CompanyId.HasValue)
            {
                return new CompanyMineDTO
                {
                    Company = null,
                    IsCompanyAdmin = false,
                    BecameCompanyAdmin = false
                };
            }

            Company? company = _repository.Company
                .Find(c => c.Id == user.CompanyId.Value)
                .FirstOrDefault();

            if (company == null)
            {
                throw new KeyNotFoundException("Your company was not found.");
            }

            int memberCount = _repository.User
                .Find(u => u.CompanyId == company.Id)
                .Count();

            bool includeInvite = isCompanyAdmin || await _userManager.IsInRoleAsync(user, "Admin");

            return new CompanyMineDTO
            {
                Company = ToCompanyDto(company, memberCount, includeInviteCode: includeInvite),
                IsCompanyAdmin = isCompanyAdmin,
                BecameCompanyAdmin = false
            };
        }

        public async Task<CompanyDTO> RotateInviteCodeAsync(
            long userId,
            bool isPlatformAdmin,
            long? companyIdOverride)
        {
            Company company = await ResolveManagedCompanyAsync(userId, isPlatformAdmin, companyIdOverride);
            company.InviteCode = GenerateUniqueInviteCode();
            _repository.Company.Update(company);
            await _repository.SaveAsync();

            int memberCount = _repository.User
                .Find(u => u.CompanyId == company.Id)
                .Count();

            return ToCompanyDto(company, memberCount, includeInviteCode: true);
        }

        public async Task<List<CompanyMemberDTO>> GetMembersAsync(
            long userId,
            bool isPlatformAdmin,
            long? companyIdOverride)
        {
            Company company = await ResolveManagedCompanyAsync(userId, isPlatformAdmin, companyIdOverride);

            return _repository.User
                .Find(u => u.CompanyId == company.Id)
                .OrderBy(u => u.Name)
                .Select(u => new CompanyMemberDTO
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email
                })
                .ToList();
        }

        public Task<List<CompanyDTO>> GetAllAsync()
        {
            List<Company> companies = _repository.Company
                .GetAllAsync()
                .OrderBy(c => c.Name)
                .ToList();

            List<CompanyDTO> result = new List<CompanyDTO>(companies.Count);
            foreach (Company company in companies)
            {
                int memberCount = _repository.User
                    .Find(u => u.CompanyId == company.Id)
                    .Count();
                result.Add(ToCompanyDto(company, memberCount, includeInviteCode: true));
            }

            return Task.FromResult(result);
        }

        private async Task<Company> ResolveManagedCompanyAsync(
            long userId,
            bool isPlatformAdmin,
            long? companyIdOverride)
        {
            // Platform Admin with explicit companyId may manage any company.
            if (isPlatformAdmin && companyIdOverride.HasValue)
            {
                Company? company = _repository.Company
                    .Find(c => c.Id == companyIdOverride.Value)
                    .FirstOrDefault();

                if (company == null)
                {
                    throw new KeyNotFoundException($"Company {companyIdOverride.Value} was not found.");
                }

                return company;
            }

            User user = await GetUserOrThrowAsync(userId);
            bool isCompanyAdmin = await _userManager.IsInRoleAsync(user, CompanyAdminRole);

            if (isCompanyAdmin && user.CompanyId.HasValue)
            {
                if (companyIdOverride.HasValue && companyIdOverride.Value != user.CompanyId.Value)
                {
                    if (!isPlatformAdmin)
                    {
                        throw new UnauthorizedAccessException(
                            "CompanyAdmin may only manage their own company.");
                    }

                    Company? overrideCompany = _repository.Company
                        .Find(c => c.Id == companyIdOverride.Value)
                        .FirstOrDefault();

                    if (overrideCompany == null)
                    {
                        throw new KeyNotFoundException($"Company {companyIdOverride.Value} was not found.");
                    }

                    return overrideCompany;
                }

                Company? ownCompany = _repository.Company
                    .Find(c => c.Id == user.CompanyId.Value)
                    .FirstOrDefault();

                if (ownCompany == null)
                {
                    throw new KeyNotFoundException("Your company was not found.");
                }

                return ownCompany;
            }

            if (isPlatformAdmin)
            {
                throw new ArgumentException(
                    "CompanyId is required for platform Admin when not managing as CompanyAdmin.");
            }

            throw new UnauthorizedAccessException("CompanyAdmin role is required.");
        }

        private async Task<User> GetUserOrThrowAsync(long userId)
        {
            User? user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new KeyNotFoundException("Authenticated user was not found.");
            }

            return user;
        }

        private string GenerateUniqueInviteCode()
        {
            for (int attempt = 0; attempt < 32; attempt++)
            {
                string code = CreateInviteCode();
                bool exists = _repository.Company
                    .Find(c => c.InviteCode == code)
                    .Any();
                if (!exists)
                {
                    return code;
                }
            }

            throw new InvalidOperationException("Unable to generate a unique invite code.");
        }

        private static string CreateInviteCode()
        {
            Span<char> buffer = stackalloc char[InviteCodeLength];
            for (int i = 0; i < InviteCodeLength; i++)
            {
                int index = RandomNumberGenerator.GetInt32(InviteCodeAlphabet.Length);
                buffer[i] = InviteCodeAlphabet[index];
            }

            return new string(buffer);
        }

        private static string NormalizeInviteCode(string inviteCode)
        {
            if (string.IsNullOrWhiteSpace(inviteCode))
            {
                throw new ArgumentException("InviteCode is required.");
            }

            return inviteCode.Trim().ToUpperInvariant();
        }

        private static string? NormalizeSlug(string? slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return null;
            }

            return slug.Trim().ToLowerInvariant();
        }

        private static CompanyDTO ToCompanyDto(Company company, int memberCount, bool includeInviteCode)
        {
            return new CompanyDTO
            {
                Id = company.Id,
                Name = company.Name,
                Slug = company.Slug,
                InviteCode = includeInviteCode ? company.InviteCode : null,
                CreatedAt = company.CreatedAt,
                MemberCount = memberCount
            };
        }
    }
}
