namespace ShelfsService.src.ShelfsService.Host.Features.Embeddings;

public sealed class EmbeddingBackfillOptions
{
    public const string SectionName = "Embeddings";

    /// <summary>Запускати backfill один раз після старту сервісу.</summary>
    public bool BackfillOnStartup { get; set; } = true;

    /// <summary>Затримка перед стартовим backfill: ParseImageService довго вантажить YOLO + CLIP.</summary>
    public int BackfillStartupDelaySeconds { get; set; } = 60;

    /// <summary>
    /// Поточна модель ембедингів у ParseImageService. Вектори іншої моделі backfill перераховує,
    /// бо скорер OutfitNetworkService їх ігнорує (embedding_model не збігається з моделлю скорера).
    /// </summary>
    public string CurrentModel { get; set; } = "patrickjohncyh/fashion-clip";
}

/// <summary>Одноразовий backfill ембедингів після старту (міграції на цей момент уже застосовані ShelfDatabaseInitializer).</summary>
public sealed class EmbeddingBackfillHostedService(
    IServiceScopeFactory scopeFactory,
    Microsoft.Extensions.Options.IOptions<EmbeddingBackfillOptions> options,
    ILogger<EmbeddingBackfillHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.BackfillOnStartup)
        {
            return;
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(options.Value.BackfillStartupDelaySeconds), stoppingToken);

            using var scope = scopeFactory.CreateScope();
            var backfill = scope.ServiceProvider.GetRequiredService<IEmbeddingBackfillService>();
            await backfill.RunAsync(userId: null, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // сервіс зупиняється
        }
        catch (Exception ex)
        {
            // Backfill — не критичний: сервіс працює далі, ембединги можна дорахувати ендпоінтом
            logger.LogError(ex, "Startup embedding backfill failed");
        }
    }
}
