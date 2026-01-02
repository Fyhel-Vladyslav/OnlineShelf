using Microsoft.EntityFrameworkCore;
using ShelfsService.src.ShelfsService.Repository.EfCore;

namespace ShelfsService.Extentions;
   public static class DbInitializationExtention
{
    /// <summary>
    /// Реєструє ініціалізацію БД (schema + migrations + seed)
    /// </summary>
    public static IServiceCollection AddShelfDatabaseInitialization(
        this IServiceCollection services)
    {
        services.AddHostedService<ShelfDatabaseInitializer>();
        return services;
    }

    /// <summary>
    /// Hosted service, який відповідає за ініціалізацію БД
    /// </summary>
    private sealed class ShelfDatabaseInitializer : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostEnvironment _environment;

        public ShelfDatabaseInitializer(
            IServiceProvider serviceProvider,
            IHostEnvironment environment)
        {
            _serviceProvider = serviceProvider;
            _environment = environment;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ShelfsDataContext>();

            // 1️⃣ Schema (must exist BEFORE migrations)
            await db.Database.ExecuteSqlRawAsync(
                "CREATE SCHEMA IF NOT EXISTS shelf_service;",
                cancellationToken
            );

            // 2️⃣ Migrations
            await db.Database.MigrateAsync(cancellationToken);

            // 3️⃣ Seed (only dev / test)
            if (_environment.IsDevelopment())
            {
                await DatabaseSeeder.SeedAsync(db);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}

