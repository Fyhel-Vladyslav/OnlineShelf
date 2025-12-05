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
        modelBuilder.HasDefaultSchema("user_service");

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
    }
}
