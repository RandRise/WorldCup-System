using Core.DTOs.Users;
using Core.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Data.Entities;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<long>> _roleManager;
        private readonly IConfiguration _configuration;
        public UserController(IUserService userService,
            RoleManager<IdentityRole<long>> roleManager,
            UserManager<User> userManager,
            IConfiguration configuration)
        {
            _userService = userService;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public List<UserDTO> GetAllUsers()
        {
            var users = _userService.GetAllUsers();
            return users;
        }
        [HttpPost]
        public async Task CreateNewUser([FromBody] CreateUserDto userDTO)
        {
            await _userService.CreateNewUser(userDTO);
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete]
        public async Task RemoveUser([FromBody] RemoveUserDto removeUserDto)
        {
            await _userService.RemoveUser(removeUserDto);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task UpdateUser([FromBody] UpdateUserDto updateUserDto)
        {
            await _userService.UpdateUser(updateUserDto);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetMe()
        {
            try
            {
                long userId = await ResolveCurrentUserIdAsync();
                CurrentUserDTO currentUser = await _userService.GetCurrentUser(userId);
                return Ok(currentUser);
            }
            catch (Exception ex)
            {
                return ApiErrorHelper.FromException(ex);
            }
        }

        private async Task<long> ResolveCurrentUserIdAsync()
        {
            string? email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Authenticated user email claim is missing.");
            }

            User? user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                throw new KeyNotFoundException("Authenticated user was not found.");
            }

            return user.Id;
        }

        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            string? jwtSecret = _configuration["JWT:Secret"];
            if (string.IsNullOrWhiteSpace(jwtSecret))
            {
                throw new InvalidOperationException("JWT:Secret is not configured.");
            }

            SymmetricSecurityKey authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return token;

        }
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel userLogin)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(userLogin.Email!);
                if (user == null)
                {
                    return Unauthorized("Invalid email or password.");
                }

                if (!await _userManager.CheckPasswordAsync(user, userLogin.Password))
                {
                    return Unauthorized("Invalid email or password.");
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var token = GetToken(authClaims);
                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo
                });
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred during login.");
            }
        }
    }
}
