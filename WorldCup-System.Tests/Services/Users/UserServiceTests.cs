using Core.DTOs.Users;
using Core.Services.Users;
using Data.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace WorldCup_System.Tests.Services.Users
{
    public class UserServiceTests
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _userManagerMock = CreateUserManagerMock();
            _userService = new UserService(_userManagerMock.Object);
        }

        [Fact]
        public void GetAllUsers_ReturnsMappedUserDtos()
        {
            List<User> users = new List<User>
            {
                new User { Id = 1, Name = "Alice", UserName = "alice@example.com", Email = "alice@example.com" },
                new User { Id = 2, Name = "Bob", UserName = "bob@example.com", Email = "bob@example.com" }
            };

            _userManagerMock
                .Setup(userManager => userManager.Users)
                .Returns(users.AsQueryable());

            List<UserDTO> result = _userService.GetAllUsers();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, userDto => userDto.Id == 1 && userDto.Name == "Alice");
            Assert.Contains(result, userDto => userDto.Id == 2 && userDto.Name == "Bob");
        }

        [Fact]
        public void GetAllUsers_WhenNoUsers_ReturnsEmptyList()
        {
            _userManagerMock
                .Setup(userManager => userManager.Users)
                .Returns(new List<User>().AsQueryable());

            List<UserDTO> result = _userService.GetAllUsers();

            Assert.Empty(result);
        }

        [Fact]
        public async Task CreateNewUser_WhenIdentityCreationSucceeds_CreatesUserWithExpectedFields()
        {
            CreateUserDto createUserDto = new CreateUserDto
            {
                Name = "Jane Doe",
                Email = "jane@example.com",
                Password = "SecurePass123!"
            };

            User? capturedUser = null;
            string? capturedPassword = null;

            _userManagerMock
                .Setup(userManager => userManager.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .Callback<User, string>((user, password) =>
                {
                    capturedUser = user;
                    capturedPassword = password;
                })
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock
                .Setup(userManager => userManager.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            await _userService.CreateNewUser(createUserDto);

            Assert.NotNull(capturedUser);
            Assert.Equal(createUserDto.Email, capturedUser.Email);
            Assert.Equal(createUserDto.Email, capturedUser.UserName);
            Assert.Equal(createUserDto.Name, capturedUser.Name);
            Assert.False(string.IsNullOrWhiteSpace(capturedUser.SecurityStamp));
            Assert.Equal(createUserDto.Password, capturedPassword);

            _userManagerMock.Verify(
                userManager => userManager.CreateAsync(It.IsAny<User>(), createUserDto.Password),
                Times.Once);
            _userManagerMock.Verify(
                userManager => userManager.AddToRoleAsync(It.IsAny<User>(), "User"),
                Times.Once);
        }

        [Fact]
        public async Task CreateNewUser_WhenIdentityCreationFails_ThrowsInvalidOperationException()
        {
            CreateUserDto createUserDto = new CreateUserDto
            {
                Name = "Jane Doe",
                Email = "jane@example.com",
                Password = "weak"
            };

            _userManagerMock
                .Setup(userManager => userManager.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak." }));

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.CreateNewUser(createUserDto));

            Assert.Contains("Password too weak", exception.Message);
        }

        [Fact]
        public async Task UpdateUser_WhenUserExists_UpdatesNameViaUserManager()
        {
            User existingUser = new User
            {
                Id = 10,
                Name = "Old Name",
                UserName = "user@example.com",
                Email = "user@example.com"
            };

            _userManagerMock
                .Setup(userManager => userManager.FindByIdAsync("10"))
                .ReturnsAsync(existingUser);

            _userManagerMock
                .Setup(userManager => userManager.UpdateAsync(existingUser))
                .ReturnsAsync(IdentityResult.Success);

            UpdateUserDto updateUserDto = new UpdateUserDto
            {
                Id = 10,
                Name = "New Name"
            };

            await _userService.UpdateUser(updateUserDto);

            Assert.Equal("New Name", existingUser.Name);
            _userManagerMock.Verify(userManager => userManager.UpdateAsync(existingUser), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_WhenUserNotFound_DoesNotUpdate()
        {
            _userManagerMock
                .Setup(userManager => userManager.FindByIdAsync("99"))
                .ReturnsAsync((User?)null);

            UpdateUserDto updateUserDto = new UpdateUserDto
            {
                Id = 99,
                Name = "New Name"
            };

            await _userService.UpdateUser(updateUserDto);

            _userManagerMock.Verify(userManager => userManager.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RemoveUser_WhenUserExists_DeletesViaUserManager()
        {
            User existingUser = new User
            {
                Id = 5,
                Name = "To Remove",
                UserName = "remove@example.com",
                Email = "remove@example.com"
            };

            _userManagerMock
                .Setup(userManager => userManager.FindByIdAsync("5"))
                .ReturnsAsync(existingUser);

            _userManagerMock
                .Setup(userManager => userManager.DeleteAsync(existingUser))
                .ReturnsAsync(IdentityResult.Success);

            RemoveUserDto removeUserDto = new RemoveUserDto { Id = 5 };

            await _userService.RemoveUser(removeUserDto);

            _userManagerMock.Verify(userManager => userManager.DeleteAsync(existingUser), Times.Once);
        }

        [Fact]
        public async Task RemoveUser_WhenUserNotFound_DoesNotDelete()
        {
            _userManagerMock
                .Setup(userManager => userManager.FindByIdAsync("404"))
                .ReturnsAsync((User?)null);

            RemoveUserDto removeUserDto = new RemoveUserDto { Id = 404 };

            await _userService.RemoveUser(removeUserDto);

            _userManagerMock.Verify(userManager => userManager.DeleteAsync(It.IsAny<User>()), Times.Never);
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
    }
}
