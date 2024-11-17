using Core.DTOs.Users;
using Data.Repos;
using Microsoft.AspNetCore.Identity;

namespace Core.Services.Users
{

    public class UserService : IUserService
    {
        private readonly IRepositoryManager _repository;
        private readonly UserManager<IdentityUser> _userManager;
        public UserService(IRepositoryManager repository, UserManager<IdentityUser> userManager)
        {
            _repository = repository;
            _userManager = userManager;
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
            
            var identityUser = new IdentityUser { UserName = user.Email, Email = user.Email };
            var result = await _userManager.CreateAsync(identityUser, user.Password);
            if (result.Succeeded)
            {
                Console.WriteLine("User Created Successfully");
            }
        }
        public async Task RemoveUser(RemoveUserDto removeUserDto)
        {

            var user = _repository.User.Find(e => e.Id == removeUserDto.Id).FirstOrDefault();
            if (user != null)
                _repository.User.Delete(user);
            await _repository.SaveAsync();

        }

        public async Task UpdateUser(UpdateUserDto updateUserDto)
        {
            var user = _repository.User.Find(e => e.Id == updateUserDto.Id).FirstOrDefault();
            if (user != null)
            {
                user.Name = updateUserDto.Name;
                _repository.User.Update(user);
                await _repository.SaveAsync();
            }
        }
    }



}
