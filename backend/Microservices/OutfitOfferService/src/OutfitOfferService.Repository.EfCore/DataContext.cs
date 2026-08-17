using Microsoft.EntityFrameworkCore;
using OutfitOfferService.src.OutfitOfferService.Repository.EfCore.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;

namespace OutfitOfferService.src.OutfitOfferService.Repository.EfCore
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }
        public DbSet<OutfitGenerationRequest> OutfitGenerationRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            //modelBuilder.HasDefaultSchema("oufit_offer_service");

            // USER → USERROLES  
            //modelBuilder.Entity<User>()
            //    .HasMany(u => u.Roles)
            //    .WithOne(ur => ur.User)
            //    .HasForeignKey(ur => ur.UserId);
        }
    }
}