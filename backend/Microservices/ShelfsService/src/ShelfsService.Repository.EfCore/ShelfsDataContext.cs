using Microsoft.EntityFrameworkCore;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;
using System;


namespace ShelfsService.src.ShelfsService.Repository.EfCore;

public class ShelfsDataContext : DbContext
{
    public ShelfsDataContext(DbContextOptions<ShelfsDataContext> options)
        : base(options)
    {
    }
    public DbSet<Shelf> Shelfs { get; set; }
    public DbSet<ItemTag> ItemTags { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<TagType> TagTypes { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("shelf_service");

        modelBuilder.Entity<ItemTag>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasOne<Item>()
                  .WithMany(i => i.Tags)
                  .HasForeignKey(t => t.ItemId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.HasMany(i => i.Tags)
                  .WithOne()
                  .HasForeignKey(t => t.ItemId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Shelf)
                  .WithMany(s => s.Items)
                  .HasForeignKey(i => i.ShelfId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TagType>(entity =>
        {
            entity.HasKey(t => t.Id);
        });

        modelBuilder.Entity<Shelf>(entity =>
        {
            entity.HasKey(t => t.Id);
        });
    }
}
