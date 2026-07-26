using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core.DTOs.Companies;
using Core.Helpers;
using Core.Services.Companies;
using Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CompanyController : Controller
    {
        private readonly ICompanyService _companyService;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        public CompanyController(
            ICompanyService companyService,
            UserManager<User> userManager,
            IConfiguration configuration)
        {
            _companyService = companyService;
            _userManager = userManager;
            _configuration = configuration;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCompanyDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                long userId = await ResolveCurrentUserIdAsync();
                CompanyDTO company = await _companyService.CreateAsync(dto, userId);
                return Ok(company);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Join([FromBody] JoinCompanyDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                long userId = await ResolveCurrentUserIdAsync();
                JoinCompanyResultDTO result = await _companyService.JoinAsync(dto, userId);

                // Flatten token like User/Login. Token build must not turn a successful join into an error.
                string? token = null;
                DateTime? expiration = null;
                if (result.BecameCompanyAdmin)
                {
                    try
                    {
                        (string jwt, DateTime expires) = await BuildTokenAsync(userId);
                        token = jwt;
                        expiration = expires;
                    }
                    catch
                    {
                        // Membership + CompanyAdmin already persisted; client can re-login.
                    }
                }

                return Ok(new
                {
                    company = result.Company,
                    becameCompanyAdmin = result.BecameCompanyAdmin,
                    isCompanyAdmin = result.IsCompanyAdmin,
                    token,
                    expiration
                });
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Mine()
        {
            try
            {
                long userId = await ResolveCurrentUserIdAsync();
                CompanyMineDTO mine = await _companyService.GetMineAsync(userId);
                return Ok(mine);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "CompanyAdmin,Admin")]
        [HttpPost]
        public async Task<IActionResult> RotateInviteCode([FromBody] RotateInviteCodeDTO? dto)
        {
            try
            {
                long userId = await ResolveCurrentUserIdAsync();
                bool isPlatformAdmin = User.IsInRole("Admin");
                CompanyDTO company = await _companyService.RotateInviteCodeAsync(
                    userId,
                    isPlatformAdmin,
                    dto?.CompanyId);
                return Ok(company);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "CompanyAdmin,Admin")]
        [HttpGet]
        public async Task<IActionResult> Members([FromQuery] long? companyId = null)
        {
            try
            {
                long userId = await ResolveCurrentUserIdAsync();
                bool isPlatformAdmin = User.IsInRole("Admin");
                List<CompanyMemberDTO> members = await _companyService.GetMembersAsync(
                    userId,
                    isPlatformAdmin,
                    companyId);
                return Ok(members);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                List<CompanyDTO> companies = await _companyService.GetAllAsync();
                return Ok(companies);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        private Task<long> ResolveCurrentUserIdAsync()
        {
            return CurrentUserResolver.ResolveUserIdAsync(User, _userManager);
        }

        private async Task<(string Token, DateTime Expiration)> BuildTokenAsync(long userId)
        {
            User? user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new KeyNotFoundException("Authenticated user was not found.");
            }

            IList<string> userRoles = await _userManager.GetRolesAsync(user);
            List<Claim> authClaims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (string userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            JwtSecurityToken securityToken = GetToken(authClaims);
            return (new JwtSecurityTokenHandler().WriteToken(securityToken), securityToken.ValidTo);
        }

        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            string? jwtSecret = _configuration["JWT:Secret"];
            if (string.IsNullOrWhiteSpace(jwtSecret))
            {
                throw new InvalidOperationException("JWT:Secret is not configured.");
            }

            SymmetricSecurityKey authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

            return new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256));
        }
    }
}
