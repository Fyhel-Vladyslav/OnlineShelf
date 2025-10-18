namespace UserService.Data;

using Microsoft.EntityFrameworkCore;
using UserService.Models;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options)
        : base(options)
    {
    }

    // This property represents the 'Users' table in the database
    public DbSet<User> Users { get; set; }
}