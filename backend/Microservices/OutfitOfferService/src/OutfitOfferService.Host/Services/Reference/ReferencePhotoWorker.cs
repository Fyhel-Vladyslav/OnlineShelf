using Microsoft.Extensions.Options;

namespace OutfitOfferService.src.OutfitOfferService.Host.Services.Reference;

/// <summary>Фоновий споживач черги еталонних фото. Кількість паралельних циклів задає ReferenceProcessing:Parallelism.</summary>
public sealed class ReferencePhotoWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ReferenceProcessingOptions> options,
    ILogger<ReferencePhotoWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Синхронна частина ExecuteAsync виконується під час старту хоста — одразу віддаємо керування
        await Task.Yield();

        var loops = Math.Max(1, options.Value.Parallelism);
        logger.LogInformation("Reference photo worker started, parallelism {Parallelism}", loops);
        await Task.WhenAll(Enumerable.Range(0, loops).Select(_ => RunLoopAsync(stoppingToken)));
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        var settings = options.Value;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ReferencePhotoProcessor>();
                if (!await processor.ProcessNextAsync(ct))
                {
                    await Task.Delay(TimeSpan.FromSeconds(settings.PollIntervalSeconds), ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Найімовірніше БД недоступна; фото не втрачається, бо замок спливе
                logger.LogError(ex, "Reference photo worker iteration failed");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(settings.ErrorBackoffSeconds), ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
