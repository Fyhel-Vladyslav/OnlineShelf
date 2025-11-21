using UserService.src.UserService.Repository.EfCore.Entities;

using Microsoft.EntityFrameworkCore;

namespace UserService.src.UserService.Repository.EfCore;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options)
        : base(options)
    {
    }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRoleLink> UserRoleLinks { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("user-service");

        // USER → USERROLES
        modelBuilder.Entity<User>()
            .HasMany(u => u.Roles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId);


        // ROLE → USERROLES
        modelBuilder.Entity<Role>()
            .HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId);

        // Composite key
        modelBuilder.Entity<UserRoleLink>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        // Seed Roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin", Description = "System administrator" },
            new Role { Id = 2, Name = "User", Description = "Regular user" },
            new Role { Id = 3, Name = "PremiumUser", Description = "Paid premium account" },
            new Role { Id = 4, Name = "Designer", Description = "Designer role" }
        );

        // Seed Users
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

        // Seed links: make admin an Admin
        modelBuilder.Entity<UserRoleLink>().HasData(
            new UserRoleLink
            {
                UserId = Guid.Parse("d1437f42-8b54-4a02-b0a5-800426cd7580"),
                RoleId = 1 // Admin
            }
        );
    }
}
