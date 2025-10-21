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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("user-service");

        // This is the Data Seeding part
        modelBuilder.Entity<User>().HasData(
new User
{
    Id = 1,
    Username = "admin",
    Email = "admin@example.com",
    PasswordHash = "hashed_password_1",
    DateCreated = new DateTime(2025, 10, 19, 18, 51, 4, 799, DateTimeKind.Utc).AddTicks(1223)
},
    new User
    {
        Id = 2,
        Username = "test",
        Email = "test@example.com",
        PasswordHash = "hashed_password_2",
        DateCreated = new DateTime(2025, 10, 19, 18, 51, 4, 799, DateTimeKind.Utc).AddTicks(2513)
    }
        );
    }

}