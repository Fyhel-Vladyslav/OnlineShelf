using Microsoft.EntityFrameworkCore;
using OutfitOfferService.src.OutfitOfferService.Repository.EfCore;

namespace OutfitOfferService.Extentions;

public static class DbInitializationExtention
{
    /// <summary>Реєструє створення схеми і міграції БД OutfitOfferService під час старту хоста.</summary>
    public static IServiceCollection AddOfferDatabaseInitialization(this IServiceCollection services)
    {
        services.AddHostedService<OfferDatabaseInitializer>();
        return services;
    }

    private sealed class OfferDatabaseInitializer(IServiceProvider serviceProvider, ILogger<OfferDatabaseInitializer> logger)
        : IHostedService
    {
        // БД у docker compose може піднятися пізніше за сервіс, тому чекаємо її, а не падаємо одразу
        private const int MaxAttempts = 30;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    using var scope = serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<DataContext>();

                    // Схема має існувати до міграцій: у ній лежить і таблиця історії міграцій
                    await db.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS {DataContext.Schema};", cancellationToken);
                    await db.Database.MigrateAsync(cancellationToken);
                    return;
                }
                catch (Exception ex) when (attempt < MaxAttempts && !cancellationToken.IsCancellationRequested)
                {
                    logger.LogWarning(ex, "Database is not ready (attempt {Attempt} of {Max}), retrying", attempt, MaxAttempts);
                    await Task.Delay(RetryDelay, cancellationToken);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
