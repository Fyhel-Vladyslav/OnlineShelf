using UserService.src.UserService.Repository.EfCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace UserService.src.UserService.Repository.EfCore;
public static class DatabaseSeeder
{
    public static async Task SeedAsync(DataContext db)
    {
        // Ensure database and schema exist
        await db.Database.MigrateAsync();

        await SeedRoles(db);
        await SeedUsers(db);
        await SeedUserRoleLinks(db);

        await db.SaveChangesAsync();
    }

    private static async Task SeedRoles(DataContext db)
    {
        if (await db.Roles.AnyAsync()) return;

        db.Roles.AddRange(
            new Role { Id = 1, Name = "Admin", Description = "System administrator" },
            new Role { Id = 2, Name = "User", Description = "Regular user" },
            new Role { Id = 3, Name = "PremiumUser", Description = "Paid premium account" },
            new Role { Id = 4, Name = "Designer", Description = "Designer role" }
        );
    }

    private static async Task SeedUsers(DataContext db)
    {
        if (await db.Users.AnyAsync()) return;

        db.Users.AddRange(
            new User
            {
                Id = Guid.Parse("d1437f42-8b54-4a02-b0a5-800426cd7580"),
                Login = "admin",
                Email = "admin@example.com",
                PasswordHash = "AQAAAAIAAYagAAAAEI4cRC2MXQNNcCJCh2m1w+g452KVBfKKuXgrlc9nn8HCk64K8egBH3B8iRp0XSw5IQ==",
                DateCreated = new DateTime(2025, 10, 19, 18, 51, 04, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 10, 19, 18, 51, 04, DateTimeKind.Utc)
            },
            new User
            {
                Id = Guid.Parse("2c9d6f42-8b54-4a02-b0a5-800426cd7581"),
                Login = "test",
                Email = "test@example.com",
                PasswordHash = "AQAAAAIAAYagAAAAEHkSP0s7Jv4tRM3HgLxrhmEkgwlxzDn+XHbbZjIVWUnGYIZWVEY5Zgmh0gpl6JAF4Q==",
                DateCreated = new DateTime(2025, 10, 19, 18, 51, 04, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 10, 19, 18, 51, 04, DateTimeKind.Utc)
            }
        );
    }

    private static async Task SeedUserRoleLinks(DataContext db)
    {
        if (await db.Roles.AnyAsync()) return;

        db.UserRoleLinks.Add(new UserRoleLink
        {
            UserId = Guid.Parse("d1437f42-8b54-4a02-b0a5-800426cd7580"),
            RoleId = 1
        });
    }
}

