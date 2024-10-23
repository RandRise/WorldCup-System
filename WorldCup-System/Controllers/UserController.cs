using Data.Repos;
using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class UserController : Controller
    {
        private readonly IRepositoryManager _repository;
        public UserController(IRepositoryManager repository) 
        { 
            _repository = repository;
        }
        [HttpGet]
        public  IQueryable GetAllUsers()
        {
            var users =  _repository.User.GetAllAsync();
            return users;
        }

    }
}
