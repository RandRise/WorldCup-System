using System.Security.Claims;
using Core.Helpers;
using Data.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace WorldCup_System.Tests.Helpers
{
    public class CurrentUserResolverTests
    {
        private readonly Mock<UserManager<User>> _userManagerMock;

        public CurrentUserResolverTests()
        {
            _userManagerMock = CreateUserManagerMock();
        }

        [Fact]
        public async Task ResolveUserIdAsync_WhenNameIdentifierClaimPresent_ReturnsUserIdWithoutLookup()
        {
            ClaimsPrincipal principal = CreatePrincipal(
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim(ClaimTypes.Email, "user@example.com"));

            long userId = await CurrentUserResolver.ResolveUserIdAsync(principal, _userManagerMock.Object);

            Assert.Equal(42, userId);
            _userManagerMock.Verify(userManager => userManager.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResolveUserIdAsync_WhenSubClaimPresent_ReturnsUserIdWithoutLookup()
        {
            ClaimsPrincipal principal = CreatePrincipal(new Claim("sub", "99"));

            long userId = await CurrentUserResolver.ResolveUserIdAsync(principal, _userManagerMock.Object);

            Assert.Equal(99, userId);
            _userManagerMock.Verify(userManager => userManager.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResolveUserIdAsync_WhenOnlyEmailClaim_LooksUpUserByEmail()
        {
            User user = new User
            {
                Id = 7,
                Email = "user@example.com",
                UserName = "user@example.com",
                Name = "User"
            };
            ClaimsPrincipal principal = CreatePrincipal(new Claim(ClaimTypes.Email, "user@example.com"));

            _userManagerMock
                .Setup(userManager => userManager.FindByEmailAsync("user@example.com"))
                .ReturnsAsync(user);

            long userId = await CurrentUserResolver.ResolveUserIdAsync(principal, _userManagerMock.Object);

            Assert.Equal(7, userId);
            _userManagerMock.Verify(userManager => userManager.FindByEmailAsync("user@example.com"), Times.Once);
        }

        [Fact]
        public async Task ResolveUserIdAsync_WhenOnlyJwtEmailClaim_LooksUpUserByEmail()
        {
            User user = new User
            {
                Id = 11,
                Email = "jwt@example.com",
                UserName = "jwt@example.com",
                Name = "Jwt User"
            };
            ClaimsPrincipal principal = CreatePrincipal(new Claim("email", "jwt@example.com"));

            _userManagerMock
                .Setup(userManager => userManager.FindByEmailAsync("jwt@example.com"))
                .ReturnsAsync(user);

            long userId = await CurrentUserResolver.ResolveUserIdAsync(principal, _userManagerMock.Object);

            Assert.Equal(11, userId);
        }

        [Fact]
        public async Task ResolveUserIdAsync_WhenIdentityClaimsMissing_ThrowsInvalidOperationException()
        {
            ClaimsPrincipal principal = CreatePrincipal();

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => CurrentUserResolver.ResolveUserIdAsync(principal, _userManagerMock.Object));

            Assert.Contains("identity claim is missing", exception.Message);
        }

        [Fact]
        public async Task ResolveUserIdAsync_WhenEmailUserNotFound_ThrowsKeyNotFoundException()
        {
            ClaimsPrincipal principal = CreatePrincipal(new Claim(ClaimTypes.Email, "missing@example.com"));

            _userManagerMock
                .Setup(userManager => userManager.FindByEmailAsync("missing@example.com"))
                .ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => CurrentUserResolver.ResolveUserIdAsync(principal, _userManagerMock.Object));
        }

        [Fact]
        public void TryResolveUserId_WhenNameIdentifierInvalid_ReturnsNull()
        {
            ClaimsPrincipal principal = CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, "not-a-number"));

            long? userId = CurrentUserResolver.TryResolveUserId(principal);

            Assert.Null(userId);
        }

        [Fact]
        public void TryResolveEmail_PrefersClaimTypesEmailOverJwtEmail()
        {
            ClaimsPrincipal principal = CreatePrincipal(
                new Claim(ClaimTypes.Email, "claim@example.com"),
                new Claim("email", "jwt@example.com"));

            string? email = CurrentUserResolver.TryResolveEmail(principal);

            Assert.Equal("claim@example.com", email);
        }

        private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
        {
            ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
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
