using Core.DTOs.Users;
using Core.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        public UserController(IUserService userService,
            RoleManager<IdentityRole> roleManager,
            UserManager<IdentityUser> userManager,
            IConfiguration configuration)
        {
            _userService = userService;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }
        [HttpGet]
        public List<UserDTO> GetAllUsers()
        {
            var users = _userService.GetAllUsers();
            return users;
        }
        [HttpPost]
        public async Task CreateNewUser([FromBody] CreateUserDto userDTO)
        {

            IdentityUser user = new()
            {
                Email = userDTO.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = userDTO.Name,
            };
            var result = await _userManager.CreateAsync(user, userDTO.Password);
            if (!result.Succeeded)
                Console.WriteLine("User Creation Failed");
        }
        [Authorize]
        [HttpDelete]
        public async Task RemoveUser([FromBody] RemoveUserDto removeUserDto)
        {
            await _userService.RemoveUser(removeUserDto);
        }
        [HttpPost]
        public async Task UpdateUser([FromBody] UpdateUserDto updateUserDto)
        {
            await _userService.UpdateUser(updateUserDto);
        }

        private JwtSecurityToken GetToken(List<Claim> AUTHClaims)
        {
            var AUTHSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(3),
                claims: AUTHClaims,
                signingCredentials: new SigningCredentials(AUTHSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return token;

        }
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel userLogin)
        {
            var user = await _userManager.FindByEmailAsync(userLogin.Email);
            if (user != null && await _userManager.CheckPasswordAsync(user, userLogin.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var AUTHClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                foreach (var userRole in userRoles)
                {
                    AUTHClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var token = GetToken(AUTHClaims);

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo
                });
            }
            return Unauthorized();
        }
    }
}
