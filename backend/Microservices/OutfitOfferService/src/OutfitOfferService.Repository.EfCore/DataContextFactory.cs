using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OutfitOfferService.src.OutfitOfferService.Repository.EfCore
{
    /// <summary>Для `dotnet ef migrations add`: створює контекст без запуску всього хоста (gRPC-клієнтів, погоди).</summary>
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseNpgsql("Host=localhost;Database=onlineShelf_db;Username=admin;Password=admin", npgsql =>
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", DataContext.Schema))
                .Options;
            return new DataContext(options);
        }
    }
}
