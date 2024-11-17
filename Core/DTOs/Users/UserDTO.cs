using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.Users
{
    public class UserDTO
    {
        public int Id { get; set; }
        public required string Name { get; set; }

    }
    public class CreateUserDto
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
    public class RemoveUserDto
    {
        public required int Id { get; set; }

    }
    public class UpdateUserDto
    {
        public required int Id { get; set; }
        public required string Name { get; set; }

    }
    public class LoginModel
    {
        public required string? Email { get; set; }
        public required string? Password { get; set; }
    }
}
