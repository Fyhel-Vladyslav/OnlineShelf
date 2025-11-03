using System.ComponentModel.DataAnnotations;
using UserService.src.Common;

namespace UserService.src.Models;
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

    public UserRole Role { get; set; } = UserRole.Guest;


    //public IList<AttributeValue> AttributeValues { get; init; } = [];



}