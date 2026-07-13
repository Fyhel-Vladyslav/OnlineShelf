using UserService.src.UserService.Repository.EfCore.Entities;

namespace UserService.src.UserService.Common.DTOs;

public class CreateUserDto
    {  
        public string Login { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public string? Avatar { get; set; }
    }

