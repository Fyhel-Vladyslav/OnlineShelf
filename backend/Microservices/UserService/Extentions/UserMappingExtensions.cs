
using UserService.src.UserService.Common.DTOs;
using UserService.src.UserService.Repository.EfCore.Entities;
namespace UserService.Extentions;

// Make sure you import the namespace where your User entity lives

public static class UserMappingExtensions
{
    // 'this User user' makes this method an extension method on the User class
    public static UserDto ToDto(this User user)
    {
        // Null check for safety
        if (user == null)
        {
            return null;
        }

        return new UserDto
        {
            Id = user.Id,
            Login = user.Login,
            Email = user.Email,
            DateCreated = user.DateCreated,
            UpdatedAt = user.UpdatedAt,
            Avatar = user.Avatar,
            EmailVerified = user.EmailVerified,
            State = user.State,
            Roles = user.Roles.Select(r => r.Role.ToString()).ToList()
        };
    }
}