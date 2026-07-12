using System.ComponentModel.DataAnnotations;
using UserService.src.UserService.Common;

namespace UserService.src.UserService.Repository.EfCore.Entities;
public class User
{
    [Required]
    public required Guid Id { get; init; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string PasswordHash { get; set; }


    [Required]
    [MaxLength(100)]
    public string Login { get; set; }
    public bool EmailVerified { get; set; }
    public int State { get; set; }
    public string? Avatar { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public List<UserRoleLink> Roles { get; set; } = new();


    //public IList<AttributeValue> AttributeValues { get; init; } = [];



}