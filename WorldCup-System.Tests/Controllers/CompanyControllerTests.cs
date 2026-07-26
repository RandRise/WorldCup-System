using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Core.DTOs.Companies;
using Core.Services.Companies;
using Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using WorldCup_System.Controllers;

namespace WorldCup_System.Tests.Controllers
{
    /// <summary>
    /// Phase 13 — CompanyController unit tests (Create / Join / Mine / Rotate / Members / GetAll).
    /// </summary>
    public class CompanyControllerTests
    {
        private readonly Mock<ICompanyService> _companyServiceMock;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly IConfiguration _configuration;
        private readonly CompanyController _companyController;

        public CompanyControllerTests()
        {
            _companyServiceMock = new Mock<ICompanyService>();
            _userManagerMock = CreateUserManagerMock();
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JWT:Secret"] = "ThisIsATestSecretKeyThatIsLongEnoughForHmacSha256!",
                    ["JWT:ValidIssuer"] = "test-issuer",
                    ["JWT:ValidAudience"] = "test-audience"
                })
                .Build();

            _companyController = new CompanyController(
                _companyServiceMock.Object,
                _userManagerMock.Object,
                _configuration);

            _companyController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public async Task Create_WhenServiceSucceeds_ReturnsOkWithCompany()
        {
            SetUserClaims(new Claim(ClaimTypes.NameIdentifier, "42"));
            CreateCompanyDTO dto = new CreateCompanyDTO { Name = "Acme Pool", Slug = "acme-pool" };
            CompanyDTO company = new CompanyDTO
            {
                Id = 7,
                Name = "Acme Pool",
                Slug = "acme-pool",
                InviteCode = "ABCD2345",
                MemberCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            _companyServiceMock
                .Setup(service => service.CreateAsync(dto, 42))
                .ReturnsAsync(company);

            IActionResult actionResult = await _companyController.Create(dto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            CompanyDTO result = Assert.IsType<CompanyDTO>(okResult.Value);
            Assert.Equal(7, result.Id);
            Assert.Equal("ABCD2345", result.InviteCode);
            _companyServiceMock.Verify(service => service.CreateAsync(dto, 42), Times.Once);
        }

        [Fact]
        public async Task Create_WhenModelInvalid_ReturnsBadRequest()
        {
            _companyController.ModelState.AddModelError("Name", "Name is required.");
            CreateCompanyDTO dto = new CreateCompanyDTO { Name = "" };

            IActionResult actionResult = await _companyController.Create(dto);

            Assert.IsType<BadRequestObjectResult>(actionResult);
            _companyServiceMock.Verify(
                service => service.CreateAsync(It.IsAny<CreateCompanyDTO>(), It.IsAny<long>()),
                Times.Never);
        }

        [Fact]
        public async Task Create_WhenServiceThrowsInvalidOperation_ReturnsBadRequest()
        {
            SetUserClaims(new Claim(ClaimTypes.NameIdentifier, "42"));
            CreateCompanyDTO dto = new CreateCompanyDTO { Name = "Other", Slug = "taken" };

            _companyServiceMock
                .Setup(service => service.CreateAsync(dto, 42))
                .ThrowsAsync(new InvalidOperationException("Slug 'taken' is already in use."));

            IActionResult actionResult = await _companyController.Create(dto);

            BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Contains("already in use", badRequest.Value?.ToString());
        }

        [Fact]
        public async Task Join_WhenBecameCompanyAdmin_ReturnsFlatShapeWithToken()
        {
            SetUserClaims(new Claim(ClaimTypes.NameIdentifier, "5"));
            User user = new User
            {
                Id = 5,
                Email = "admin@example.com",
                UserName = "admin@example.com",
                Name = "Admin"
            };

            JoinCompanyDTO dto = new JoinCompanyDTO { InviteCode = "JOINCODE" };
            JoinCompanyResultDTO joinResult = new JoinCompanyResultDTO
            {
                Company = new CompanyDTO
                {
                    Id = 10,
                    Name = "Acme",
                    InviteCode = "JOINCODE",
                    MemberCount = 1,
                    CreatedAt = DateTime.UtcNow
                },
                BecameCompanyAdmin = true,
                IsCompanyAdmin = true
            };

            _companyServiceMock.Setup(service => service.JoinAsync(dto, 5)).ReturnsAsync(joinResult);
            _userManagerMock.Setup(manager => manager.FindByIdAsync("5")).ReturnsAsync(user);
            _userManagerMock
                .Setup(manager => manager.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "User", "CompanyAdmin" });

            IActionResult actionResult = await _companyController.Join(dto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.NotNull(okResult.Value);

            bool becameCompanyAdmin = GetAnonymousBool(okResult.Value!, "becameCompanyAdmin");
            bool isCompanyAdmin = GetAnonymousBool(okResult.Value!, "isCompanyAdmin");
            string? token = GetAnonymousString(okResult.Value!, "token");
            object? company = GetAnonymousProperty(okResult.Value!, "company");

            Assert.True(becameCompanyAdmin);
            Assert.True(isCompanyAdmin);
            Assert.False(string.IsNullOrWhiteSpace(token));
            Assert.NotNull(company);

            JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            Assert.Contains(jwt.Claims, claim => claim.Value == "CompanyAdmin");
        }

        [Fact]
        public async Task Join_WhenNotCompanyAdmin_ReturnsNullToken()
        {
            SetUserClaims(new Claim(ClaimTypes.NameIdentifier, "5"));
            JoinCompanyDTO dto = new JoinCompanyDTO { InviteCode = "JOINCODE" };
            JoinCompanyResultDTO joinResult = new JoinCompanyResultDTO
            {
                Company = new CompanyDTO
                {
                    Id = 10,
                    Name = "Acme",
                    InviteCode = null,
                    MemberCount = 2,
                    CreatedAt = DateTime.UtcNow
                },
                BecameCompanyAdmin = false,
                IsCompanyAdmin = false
            };

            _companyServiceMock.Setup(service => service.JoinAsync(dto, 5)).ReturnsAsync(joinResult);

            IActionResult actionResult = await _companyController.Join(dto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.False(GetAnonymousBool(okResult.Value!, "becameCompanyAdmin"));
            Assert.Null(GetAnonymousString(okResult.Value!, "token"));
            _userManagerMock.Verify(manager => manager.GetRolesAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Join_WhenTokenBuildFails_StillReturnsOkWithNullToken()
        {
            SetUserClaims(new Claim(ClaimTypes.NameIdentifier, "5"));
            JoinCompanyDTO dto = new JoinCompanyDTO { InviteCode = "JOINCODE" };
            JoinCompanyResultDTO joinResult = new JoinCompanyResultDTO
            {
                Company = new CompanyDTO
                {
                    Id = 10,
                    Name = "Acme",
                    InviteCode = "JOINCODE",
                    MemberCount = 1,
                    CreatedAt = DateTime.UtcNow
                },
                BecameCompanyAdmin = true,
                IsCompanyAdmin = true
            };

            _companyServiceMock.Setup(service => service.JoinAsync(dto, 5)).ReturnsAsync(joinResult);
            // BuildTokenAsync looks up the user again; null forces fail-soft path.
            _userManagerMock.Setup(manager => manager.FindByIdAsync("5")).ReturnsAsync((User?)null);

            IActionResult actionResult = await _companyController.Join(dto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.True(GetAnonymousBool(okResult.Value!, "becameCompanyAdmin"));
            Assert.Null(GetAnonymousString(okResult.Value!, "token"));
            Assert.Null(GetAnonymousProperty(okResult.Value!, "expiration"));
        }

        [Fact]
        public async Task Join_WhenInviteInvalid_ReturnsNotFound()
        {
            SetUserClaims(new Claim(ClaimTypes.NameIdentifier, "5"));
            JoinCompanyDTO dto = new JoinCompanyDTO { InviteCode = "NOPECODE" };

            _companyServiceMock
                .Setup(service => service.JoinAsync(dto, 5))
                .ThrowsAsync(new KeyNotFoundException("Invite code was not found."));

            IActionResult actionResult = await _companyController.Join(dto);

            NotFoundObjectResult notFound = Assert.IsType<NotFoundObjectResult>(actionResult);
            Assert.Contains("Invite code", notFound.Value?.ToString());
        }

        [Fact]
        public async Task Join_WhenModelInvalid_ReturnsBadRequest()
        {
            _companyController.ModelState.AddModelError("InviteCode", "InviteCode is required.");
            JoinCompanyDTO dto = new JoinCompanyDTO { InviteCode = "" };

            IActionResult actionResult = await _companyController.Join(dto);

            Assert.IsType<BadRequestObjectResult>(actionResult);
            _companyServiceMock.Verify(
                service => service.JoinAsync(It.IsAny<JoinCompanyDTO>(), It.IsAny<long>()),
                Times.Never);
        }

        [Fact]
        public async Task Mine_WhenAuthenticated_ReturnsMineDto()
        {
            SetUserClaims(new Claim(ClaimTypes.NameIdentifier, "5"));
            CompanyMineDTO mine = new CompanyMineDTO
            {
                Company = new CompanyDTO
                {
                    Id = 10,
                    Name = "Acme",
                    InviteCode = "SECRET01",
                    MemberCount = 3,
                    CreatedAt = DateTime.UtcNow
                },
                IsCompanyAdmin = true,
                BecameCompanyAdmin = false
            };

            _companyServiceMock.Setup(service => service.GetMineAsync(5)).ReturnsAsync(mine);

            IActionResult actionResult = await _companyController.Mine();

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            CompanyMineDTO result = Assert.IsType<CompanyMineDTO>(okResult.Value);
            Assert.Equal(10, result.Company!.Id);
            Assert.True(result.IsCompanyAdmin);
        }

        [Fact]
        public async Task Mine_WhenCompanyMissing_ReturnsNotFound()
        {
            SetUserClaims(new Claim(ClaimTypes.NameIdentifier, "5"));
            _companyServiceMock
                .Setup(service => service.GetMineAsync(5))
                .ThrowsAsync(new KeyNotFoundException("Your company was not found."));

            IActionResult actionResult = await _companyController.Mine();

            Assert.IsType<NotFoundObjectResult>(actionResult);
        }

        [Fact]
        public async Task RotateInviteCode_CompanyAdmin_PassesNullOverride()
        {
            SetUserClaims(
                new Claim(ClaimTypes.NameIdentifier, "5"),
                new Claim(ClaimTypes.Role, "CompanyAdmin"));

            CompanyDTO company = new CompanyDTO
            {
                Id = 10,
                Name = "Acme",
                InviteCode = "NEWCODE1",
                MemberCount = 2,
                CreatedAt = DateTime.UtcNow
            };

            _companyServiceMock
                .Setup(service => service.RotateInviteCodeAsync(5, false, null))
                .ReturnsAsync(company);

            IActionResult actionResult = await _companyController.RotateInviteCode(null);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            CompanyDTO result = Assert.IsType<CompanyDTO>(okResult.Value);
            Assert.Equal("NEWCODE1", result.InviteCode);
            _companyServiceMock.Verify(
                service => service.RotateInviteCodeAsync(5, false, null),
                Times.Once);
        }

        [Fact]
        public async Task RotateInviteCode_PlatformAdmin_PassesCompanyIdAndIsPlatformAdmin()
        {
            SetUserClaims(
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Admin"));

            RotateInviteCodeDTO dto = new RotateInviteCodeDTO { CompanyId = 20 };
            CompanyDTO company = new CompanyDTO
            {
                Id = 20,
                Name = "Globex",
                InviteCode = "ROTATED1",
                MemberCount = 1,
                CreatedAt = DateTime.UtcNow
            };

            _companyServiceMock
                .Setup(service => service.RotateInviteCodeAsync(1, true, 20))
                .ReturnsAsync(company);

            IActionResult actionResult = await _companyController.RotateInviteCode(dto);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal(20, Assert.IsType<CompanyDTO>(okResult.Value).Id);
            _companyServiceMock.Verify(
                service => service.RotateInviteCodeAsync(1, true, 20),
                Times.Once);
        }

        [Fact]
        public async Task RotateInviteCode_WhenUnauthorized_ReturnsForbidden()
        {
            SetUserClaims(
                new Claim(ClaimTypes.NameIdentifier, "5"),
                new Claim(ClaimTypes.Role, "CompanyAdmin"));

            _companyServiceMock
                .Setup(service => service.RotateInviteCodeAsync(5, false, 20))
                .ThrowsAsync(new UnauthorizedAccessException("CompanyAdmin may only manage their own company."));

            IActionResult actionResult = await _companyController.RotateInviteCode(
                new RotateInviteCodeDTO { CompanyId = 20 });

            ObjectResult forbidden = Assert.IsType<ObjectResult>(actionResult);
            Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
            Assert.Contains("own company", forbidden.Value?.ToString());
        }

        [Fact]
        public async Task Members_CompanyAdmin_PassesNullCompanyId()
        {
            SetUserClaims(
                new Claim(ClaimTypes.NameIdentifier, "5"),
                new Claim(ClaimTypes.Role, "CompanyAdmin"));

            List<CompanyMemberDTO> members = new List<CompanyMemberDTO>
            {
                new CompanyMemberDTO { Id = 1, Name = "Alice", Email = "a@ex.com" },
                new CompanyMemberDTO { Id = 2, Name = "Bob", Email = "b@ex.com" }
            };

            _companyServiceMock
                .Setup(service => service.GetMembersAsync(5, false, null))
                .ReturnsAsync(members);

            IActionResult actionResult = await _companyController.Members();

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            List<CompanyMemberDTO> result = Assert.IsType<List<CompanyMemberDTO>>(okResult.Value);
            Assert.Equal(2, result.Count);
            _companyServiceMock.Verify(
                service => service.GetMembersAsync(5, false, null),
                Times.Once);
        }

        [Fact]
        public async Task Members_PlatformAdmin_PassesCompanyIdOverride()
        {
            SetUserClaims(
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Admin"));

            List<CompanyMemberDTO> members = new List<CompanyMemberDTO>
            {
                new CompanyMemberDTO { Id = 9, Name = "Eve", Email = "e@ex.com" }
            };

            _companyServiceMock
                .Setup(service => service.GetMembersAsync(1, true, 20))
                .ReturnsAsync(members);

            IActionResult actionResult = await _companyController.Members(companyId: 20);

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            List<CompanyMemberDTO> result = Assert.IsType<List<CompanyMemberDTO>>(okResult.Value);
            Assert.Single(result);
            _companyServiceMock.Verify(
                service => service.GetMembersAsync(1, true, 20),
                Times.Once);
        }

        [Fact]
        public async Task Members_WhenUnauthorized_ReturnsForbidden()
        {
            SetUserClaims(new Claim(ClaimTypes.NameIdentifier, "5"));

            _companyServiceMock
                .Setup(service => service.GetMembersAsync(5, false, null))
                .ThrowsAsync(new UnauthorizedAccessException("CompanyAdmin role is required."));

            IActionResult actionResult = await _companyController.Members();

            ObjectResult forbidden = Assert.IsType<ObjectResult>(actionResult);
            Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        }

        [Fact]
        public async Task GetAll_ReturnsCompaniesFromService()
        {
            List<CompanyDTO> companies = new List<CompanyDTO>
            {
                new CompanyDTO
                {
                    Id = 1,
                    Name = "Alpha Co",
                    InviteCode = "ALPHA001",
                    MemberCount = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new CompanyDTO
                {
                    Id = 2,
                    Name = "Beta Co",
                    InviteCode = "BETA0001",
                    MemberCount = 0,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _companyServiceMock.Setup(service => service.GetAllAsync()).ReturnsAsync(companies);

            IActionResult actionResult = await _companyController.GetAll();

            OkObjectResult okResult = Assert.IsType<OkObjectResult>(actionResult);
            List<CompanyDTO> result = Assert.IsType<List<CompanyDTO>>(okResult.Value);
            Assert.Equal(2, result.Count);
            Assert.Equal("Alpha Co", result[0].Name);
            _companyServiceMock.Verify(service => service.GetAllAsync(), Times.Once);
        }

        private void SetUserClaims(params Claim[] claims)
        {
            ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuth");
            _companyController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
        }

        private static object? GetAnonymousProperty(object response, string propertyName)
        {
            System.Reflection.PropertyInfo? property = response.GetType().GetProperty(propertyName);
            Assert.NotNull(property);
            return property!.GetValue(response);
        }

        private static string? GetAnonymousString(object response, string propertyName)
        {
            return GetAnonymousProperty(response, propertyName) as string;
        }

        private static bool GetAnonymousBool(object response, string propertyName)
        {
            object? value = GetAnonymousProperty(response, propertyName);
            Assert.IsType<bool>(value);
            return (bool)value!;
        }

        private static Mock<UserManager<User>> CreateUserManagerMock()
        {
            Mock<IUserStore<User>> userStoreMock = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(
                userStoreMock.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);
        }
    }
}
