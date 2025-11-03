namespace UserService.src.Data;

using Microsoft.EntityFrameworkCore;
using UserService.src.Models;

// Renamed the class to avoid the circular dependency issue
public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options)
        : base(options)
    {
    }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("user-service");

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = Guid.Parse("d1437f42-8b54-4a02-b0a5-800426cd7580"),
                Login = "admin",
                Email = "admin@example.com",
                PasswordHash = "AQAAAAIAAYagAAAAEI4cRC2MXQNNcCJCh2m1w+g452KVBfKKuXgrlc9nn8HCk64K8egBH3B8iRp0XSw5IQ==",
                DateCreated = new DateTime(2025, 10, 19, 18, 51, 04, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 10, 19, 18, 51, 04, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = Guid.Parse("2c9d6f42-8b54-4a02-b0a5-800426cd7581"),
                Login = "test",
                Email = "test@example.com",
                PasswordHash = "AQAAAAIAAYagAAAAEHkSP0s7Jv4tRM3HgLxrhmEkgwlxzDn+XHbbZjIVWUnGYIZWVEY5Zgmh0gpl6JAF4Q==",
                DateCreated = new DateTime(2025, 10, 19, 18, 51, 04, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 10, 19, 18, 51, 04, 0, DateTimeKind.Utc)
            }
        );
    }
}
