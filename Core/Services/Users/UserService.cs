using Core.DTOs.Users;
using Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace Core.Services.Users
{

    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        public UserService(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public List<UserDTO> GetAllUsers()
        {
            List<UserDTO> userDto = _userManager.Users.Select(e => new UserDTO
            {
                Id = e.Id,
                Name = e.Name,
                Email = e.Email
            }).ToList();
            return userDto;
        }
        public async Task CreateNewUser(CreateUserDto user)
        {
            
            var identityUser = new User
            {
                Email = user.Email,
                UserName = user.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                Name = user.Name
            };
            var result = await _userManager.CreateAsync(identityUser, user.Password);
            if (!result.Succeeded)
            {
                string errors = string.Join("; ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"User creation failed: {errors}");
            }

            await _userManager.AddToRoleAsync(identityUser, "User");
        }
        public async Task RemoveUser(RemoveUserDto removeUserDto)
        {
            User? user = await _userManager.FindByIdAsync(removeUserDto.Id.ToString());
            if (user != null)
            {
                IdentityResult result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    string errors = string.Join("; ", result.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"User deletion failed: {errors}");
                }
            }
        }

        public async Task UpdateUser(UpdateUserDto updateUserDto)
        {
            User? user = await _userManager.FindByIdAsync(updateUserDto.Id.ToString());
            if (user != null)
            {
                user.Name = updateUserDto.Name;
                IdentityResult result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    string errors = string.Join("; ", result.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"User update failed: {errors}");
                }
            }
        }
    }



}
