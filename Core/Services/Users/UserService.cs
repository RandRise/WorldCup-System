﻿using Core.DTOs.Users;
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
        public UserService (IRepositoryManager repository)
        {
            _repository = repository;

        }

        public List<UserDTO> GetAllUsers()
        {
            var users = _repository.User.GetAllAsync();
            _repository.User.Create()
            var userDto = users.Select(e => new UserDTO
            {
                Id = e.Id,
                Name = e.Name
            }).ToList();
            return userDto;
        }
    }
}
