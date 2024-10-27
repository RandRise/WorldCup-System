using Core.DTOs.Users;
using Data.Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.Users
{

    public class UserService : IUserService
    {
        private readonly IRepositoryManager _repository;
        public UserService(IRepositoryManager repository)
        {
            _repository = repository;

        }

        public List<UserDTO> GetAllUsers()
        {
            var users = _repository.User.GetAllAsync();

            var userDto = users.Select(e => new UserDTO
            {
                Id = e.Id,
                Name = e.Name
            }).ToList();
            return userDto;
        }
        public async Task CreateNewUser(CreateUserDto user)
        {
            _repository.User.Create(new Data.Entities.User { Name = user.Name });
            await _repository.SaveAsync();
        }
        public async Task RemoveUser(RemoveUserDto removeUserDto)
        {

            var user = _repository.User.Find(e => e.Id == removeUserDto.Id).FirstOrDefault();
            if (user != null)
                _repository.User.Delete(user);
            await _repository.SaveAsync();

        }
    }
}
