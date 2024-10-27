using Core.DTOs.Users;

namespace Core.Services.Users
{
    public interface IUserService
    {
        public List<UserDTO> GetAllUsers();
    }
}
