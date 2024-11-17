using Core.DTOs.Users;

namespace Core.Services.Users
{
    public interface IUserService
    {
        public List<UserDTO> GetAllUsers();
        Task CreateNewUser(CreateUserDto userDTO);
        Task RemoveUser(RemoveUserDto removeUserDto);
        Task UpdateUser(UpdateUserDto updateUserDto);

    }
}
