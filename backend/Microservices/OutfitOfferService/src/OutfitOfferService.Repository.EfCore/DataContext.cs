using Microsoft.EntityFrameworkCore;
using OutfitOfferService.src.OutfitOfferService.Repository.EfCore.Entities;

namespace OutfitOfferService.src.OutfitOfferService.Repository.EfCore
{
    public class DataContext : DbContext
    {
        public const string Schema = "outfit_offer_service";

        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }

        public DbSet<ReferenceBatch> ReferenceBatches { get; set; }
        public DbSet<ReferencePhoto> ReferencePhotos { get; set; }
        public DbSet<ReferenceOutfit> ReferenceOutfits { get; set; }
        public DbSet<ReferenceItem> ReferenceItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema(Schema);

            modelBuilder.Entity<ReferenceBatch>(b =>
            {
                b.Property(x => x.Name).HasMaxLength(200);
                b.HasIndex(x => x.OwnerUserId);
                b.HasMany(x => x.Photos).WithOne(x => x.Batch).HasForeignKey(x => x.BatchId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ReferencePhoto>(p =>
            {
                p.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                p.Property(x => x.ContentHash).HasMaxLength(64);
                p.Property(x => x.FileName).HasMaxLength(260);
                p.Property(x => x.BigImage).HasMaxLength(100);
                p.Property(x => x.SmallImage).HasMaxLength(100);
                p.Property(x => x.LastError).HasMaxLength(2000);
                // Дубль того самого файла в межах власника не створює нової обробки
                p.HasIndex(x => new { x.OwnerUserId, x.ContentHash }).IsUnique();
                // Вибірка наступного фото для воркера і лічильники партії
                p.HasIndex(x => new { x.Status, x.NextAttemptAt });
                p.HasIndex(x => x.BatchId);
                p.HasOne(x => x.Outfit).WithOne(x => x.Photo).HasForeignKey<ReferenceOutfit>(x => x.PhotoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ReferenceOutfit>(o =>
            {
                o.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                o.Property(x => x.EmbeddingModel).HasMaxLength(200);
                o.HasIndex(x => x.PhotoId).IsUnique();
                o.HasIndex(x => x.Status);
                o.HasMany(x => x.Items).WithOne(x => x.Outfit).HasForeignKey(x => x.OutfitId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ReferenceItem>(i =>
            {
                i.Property(x => x.Slot).HasConversion<string>().HasMaxLength(20);
                i.Property(x => x.ColorMain).HasMaxLength(20);
                i.Property(x => x.ColorSecond).HasMaxLength(20);
                i.Property(x => x.Embedding).HasColumnType("real[]");
                i.HasIndex(x => x.OutfitId);
            });
        }
    }
}
