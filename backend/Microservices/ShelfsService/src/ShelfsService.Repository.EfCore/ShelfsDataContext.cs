using Microsoft.EntityFrameworkCore;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;
using System;
using AttributeName = ShelfsService.src.ShelfsService.Repository.EfCore.Entities.AttributeName;


namespace ShelfsService.src.ShelfsService.Repository.EfCore;

public class ShelfsDataContext : DbContext
{
    public ShelfsDataContext(DbContextOptions<ShelfsDataContext> options)
        : base(options)
    {
    }
    public DbSet<Shelf> Shelfs { get; set; }
    public DbSet<Item> Items { get; set; }

    public DbSet<AttributesValue> AttributesValues { get; set; }
    public DbSet<AttributeName> Attributes { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("shelf_service");

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.HasOne(i => i.Shelf)
                  .WithMany(s => s.Items)
                  .HasForeignKey(i => i.ShelfId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AttributesValue>(value =>
        {
            value.HasKey(i => i.Id);

        });
        modelBuilder.Entity<AttributeName>(attribute =>
        {
            attribute.HasKey(i => i.Id);

            attribute.HasMany(a => a.Values)
                  .WithOne(v => v.Attribute)
                  .HasForeignKey(v => v.AttributeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Shelf>(entity =>
        {
            entity.HasKey(t => t.Id);
        });
    }
}
