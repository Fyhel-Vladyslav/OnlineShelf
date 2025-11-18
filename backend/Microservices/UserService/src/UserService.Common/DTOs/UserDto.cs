using UserService.src.UserService.Repository.EfCore.Entities;

namespace UserService.src.UserService.Common.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string Login { get; set; }
    public string Password { get; set; }
    public string Email { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<String> Roles { get; set; }
    public bool EmailVerified { get; set; }
    public int State { get; set; }
    public string? Avatar { get; set; }
}

