namespace Core.DTOs.Users
{
    public class UserDTO
    {
        public required long Id { get; set; }
        public required string Name { get; set; }
        public string? Email { get; set; }
    }
    public class CreateUserDto
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
    public class RemoveUserDto
    {
        public required long Id { get; set; }

    }
    public class UpdateUserDto
    {
        public required long Id { get; set; }
        public required string Name { get; set; }

    }
    public class LoginModel
    {
        public required string? Email { get; set; }
        public required string? Password { get; set; }
    }
    
}
