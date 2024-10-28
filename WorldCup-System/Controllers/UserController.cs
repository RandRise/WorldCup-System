using Core.DTOs.Users;
using Core.Services.Users;
using Data.Entities;
using Data.Repos;
using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
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
            await _userService.CreateNewUser(userDTO);
        }
        [HttpDelete]
        public async Task RemoveUser([FromBody]RemoveUserDto removeUserDto)
        {
            await _userService.RemoveUser(removeUserDto);
        }
        [HttpPost]
        public async Task UpdateUser([FromBody]UpdateUserDto updateUserDto)
        {
            await _userService.UpdateUser(updateUserDto);
        }

    }
}
