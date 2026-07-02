using System.Security.Claims;
using Core.DTOs.Users;
using Core.Services.Users;
using Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole<long>>> _roleManagerMock;
        private readonly IConfiguration _configuration;
        private readonly UserController _userController;

        public UserControllerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _userManagerMock = CreateUserManagerMock();
            _roleManagerMock = CreateRoleManagerMock();
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JWT:Secret"] = "ThisIsATestSecretKeyThatIsLongEnoughForHmacSha256!",
                    ["JWT:ValidIssuer"] = "test-issuer",
                    ["JWT:ValidAudience"] = "test-audience"
                })
                .Build();

            _userController = new UserController(
                _userServiceMock.Object,
                _roleManagerMock.Object,
                _userManagerMock.Object,
                _configuration);
        }

        [Fact]
        public void GetAllUsers_ReturnsUsersFromService()
        {
            List<UserDTO> users = new List<UserDTO>
            {
                new UserDTO { Id = 1, Name = "Alice", Email = "alice@example.com" }
            };

            _userServiceMock.Setup(userService => userService.GetAllUsers()).Returns(users);

            List<UserDTO> result = _userController.GetAllUsers();

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
            _userServiceMock.Verify(userService => userService.GetAllUsers(), Times.Once);
        }

        [Fact]
        public async Task Login_WhenCredentialsAreValid_ReturnsOkWithToken()
        {
            User user = new User
            {
                Id = 1,
                Email = "user@example.com",
                UserName = "user@example.com",
                Name = "User"
            };

            LoginModel loginModel = new LoginModel
            {
                Email = "user@example.com",
                Password = "Password123!"
            };

            _userManagerMock
                .Setup(userManager => userManager.FindByEmailAsync(loginModel.Email))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(userManager => userManager.CheckPasswordAsync(user, loginModel.Password))
                .ReturnsAsync(true);

            _userManagerMock
                .Setup(userManager => userManager.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "User" });

            IActionResult actionResult = await _userController.Login(loginModel);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task Login_WhenEmailNotFound_ReturnsUnauthorized()
        {
            LoginModel loginModel = new LoginModel
            {
                Email = "missing@example.com",
                Password = "Password123!"
            };

            _userManagerMock
                .Setup(userManager => userManager.FindByEmailAsync(loginModel.Email))
                .ReturnsAsync((User?)null);

            IActionResult actionResult = await _userController.Login(loginModel);

            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(actionResult);
            Assert.Equal("Invalid email or password.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task Login_WhenPasswordIsInvalid_ReturnsUnauthorized()
        {
            User user = new User
            {
                Id = 2,
                Email = "user@example.com",
                UserName = "user@example.com",
                Name = "User"
            };

            LoginModel loginModel = new LoginModel
            {
                Email = "user@example.com",
                Password = "WrongPassword"
            };

            _userManagerMock
                .Setup(userManager => userManager.FindByEmailAsync(loginModel.Email))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(userManager => userManager.CheckPasswordAsync(user, loginModel.Password))
                .ReturnsAsync(false);

            IActionResult actionResult = await _userController.Login(loginModel);

            UnauthorizedObjectResult unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(actionResult);
            Assert.Equal("Invalid email or password.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task RemoveUser_DelegatesToUserService()
        {
            RemoveUserDto removeUserDto = new RemoveUserDto { Id = 5 };

            await _userController.RemoveUser(removeUserDto);

            _userServiceMock.Verify(userService => userService.RemoveUser(removeUserDto), Times.Once);
        }

        [Fact]
        public async Task GetMe_WhenAuthenticated_ReturnsCurrentUserProfile()
        {
            User user = new User
            {
                Id = 12,
                Email = "user@example.com",
                UserName = "user@example.com",
                Name = "User"
            };
            CurrentUserDTO currentUser = new CurrentUserDTO
            {
                Id = 12,
                Name = "User",
                Email = "user@example.com",
                Roles = new List<string> { "User" }
            };

            SetUserClaims("user@example.com");
            _userManagerMock.Setup(userManager => userManager.FindByEmailAsync("user@example.com")).ReturnsAsync(user);
            _userServiceMock.Setup(userService => userService.GetCurrentUser(12)).ReturnsAsync(currentUser);

            IActionResult actionResult = await _userController.GetMe();

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            CurrentUserDTO result = Assert.IsType<CurrentUserDTO>(okResult.Value);
            Assert.Equal(12, result.Id);
            Assert.Equal("User", result.Name);
        }

        private void SetUserClaims(string email)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email)
            };
            ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuth");
            _userController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
        }

        private static Mock<UserManager<User>> CreateUserManagerMock()
        {
            Mock<IUserStore<User>> userStoreMock = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(
                userStoreMock.Object,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        private static Mock<RoleManager<IdentityRole<long>>> CreateRoleManagerMock()
        {
            Mock<IRoleStore<IdentityRole<long>>> roleStoreMock = new Mock<IRoleStore<IdentityRole<long>>>();
            return new Mock<RoleManager<IdentityRole<long>>>(
                roleStoreMock.Object,
                null,
                null,
                null,
                null);
        }
    }
}
