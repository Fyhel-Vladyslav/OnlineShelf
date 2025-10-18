using System.ComponentModel.DataAnnotations;

namespace UserService.Models;
public class User
{
    // Primary Key for the database
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    // Store a hash of the password, NEVER the plain password!
    [Required]
    public string PasswordHash { get; set; }

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}