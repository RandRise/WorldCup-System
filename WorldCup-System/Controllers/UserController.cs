using Core.DTOs.Users;
using Core.Services.Users;
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
        public  List<UserDTO> GetAllUsers()
        {
            var users = _userService.GetAllUsers();
            return users;
        }

    }
}
