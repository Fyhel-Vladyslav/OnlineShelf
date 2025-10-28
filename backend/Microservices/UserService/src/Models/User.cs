using System.ComponentModel.DataAnnotations;

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
    public required string Login { get; init; }
    public bool EmailVerified { get; set; }
    public int State { get; set; }
    public string? Avatar { get; init; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; } = DateTime.UtcNow;


    //public IList<AttributeValue> AttributeValues { get; init; } = [];



}