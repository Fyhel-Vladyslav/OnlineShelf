using System.Diagnostics;
using System.Net.Http.Json;
using Grpc.Core;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;
using OutfitOfferService.src.OutfitOfferService.Common;
using OutfitOfferService.src.OutfitOfferService.Common.Interfaces;
using OutfitOfferService.src.OutfitOfferService.Common.Models;

namespace OutfitOfferService.src.OutfitOfferService.Host.Services.OutfitGeneration;

/// <summary>
/// Прогріває шлях генерації у фоні після старту: відкриває gRPC-канали до ShelfsService і OutfitNetworkService,
/// проганяє ConstraintFilter і beam search на синтетичному гардеробі, а наприкінці робить запит до власного
/// ендпоінта, щоб HTTP/2-з'єднання і JIT не припадали на перший запит користувача. Не блокує старт:
/// у docker compose залежності можуть піднятись пізніше, тому поки вони недоступні, прогрів повторюється.
/// Нічого не записує.
/// </summary>
public sealed class OutfitGenerationWarmupService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutfitGenerationOptions> options,
    ILogger<OutfitGenerationWarmupService> logger) : BackgroundService
{
    private static readonly OutfitSlot[] WarmupSlots =
        [OutfitSlot.Top, OutfitSlot.Bottom, OutfitSlot.Shoes, OutfitSlot.Outerwear, OutfitSlot.FullBody];

    // Розмір вектора важливий лише для серіалізації gRPC; чужу модель скорер вважає «немає фото»
    private const int WarmupEmbeddingDim = 512;
    private const string WarmupEmbeddingModel = "warmup";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.WarmupOnStartup)
        {
            return;
        }

        // Синхронна частина ExecuteAsync виконується під час старту хоста — одразу віддаємо керування
        await Task.Yield();

        var stopwatch = Stopwatch.StartNew();
        for (var attempt = 1; attempt <= settings.WarmupMaxAttempts; attempt++)
        {
            try
            {
                await RunOnceAsync(settings, stoppingToken);
                logger.LogInformation("Outfit generation warmed up in {ElapsedMs} ms (attempt {Attempt})",
                    stopwatch.ElapsedMilliseconds, attempt);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
            {
                logger.LogDebug("Warmup attempt {Attempt}: downstream service is not ready ({Status})", attempt, ex.StatusCode);
            }
            catch (Exception ex)
            {
                // Прогрів не критичний: без нього перший запит лише повільніший
                logger.LogWarning(ex, "Outfit generation warmup failed; the first request will be slower");
                return;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(settings.WarmupRetryDelaySeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        logger.LogWarning("Outfit generation warmup gave up after {Attempts} attempts; the first request will be slower",
            settings.WarmupMaxAttempts);
    }

    private async Task RunOnceAsync(OutfitGenerationOptions settings, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        // Випадковий користувач: гардероб порожній, але канал до ShelfsService відкрито й запит пройшов увесь шлях
        await services.GetRequiredService<IWardrobeProvider>().GetWardrobeAsync(Guid.NewGuid(), onlyFavorite: false, ct);

        var pool = services.GetRequiredService<IConstraintFilter>()
            .Apply(BuildSyntheticWardrobe(settings), new OfferContext { SeasonPhase = 1 });
        await services.GetRequiredService<IOutfitGenerator>().GenerateAsync(pool, settings.DefaultTopN, ct);

        // Останнім: gRPC-залежності вже відповіли, тож 503 тут не буде
        await WarmHttpEndpointAsync(services, ct);
    }

    /// <summary>Запит до власного ендпоінта від випадкового користувача: FastEndpoints, валідація, JSON.</summary>
    private async Task WarmHttpEndpointAsync(IServiceProvider services, CancellationToken ct)
    {
        // IServer і IHostApplicationLifetime є лише у справжньому хості, у юніт-тестах цей крок пропускається
        if (services.GetService<IHostApplicationLifetime>() is not { } lifetime || services.GetService<IServer>() is not { } server)
        {
            return;
        }

        // Адреси Kestrel з'являються лише після старту сервера
        var started = new TaskCompletionSource();
        using (lifetime.ApplicationStarted.Register(() => started.TrySetResult()))
        {
            await started.Task.WaitAsync(ct);
        }

        var address = server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();
        if (address is null)
        {
            return;
        }

        using var client = new HttpClient { BaseAddress = new Uri(address.Replace("[::]", "localhost").Replace("0.0.0.0", "localhost")) };
        using var response = await client.PostAsJsonAsync(ApiRoutes.GenerateOutfits, new { userId = Guid.NewGuid() }, ct);
        logger.LogDebug("Warmup request to {Route} returned {Status}", ApiRoutes.GenerateOutfits, (int)response.StatusCode);
    }

    /// <summary>По дві всесезонні речі на слот, щоб beam search пройшов кроки з 2, 3 і 4 речами в обох шаблонах.</summary>
    public static List<WardrobeItem> BuildSyntheticWardrobe(OutfitGenerationOptions settings) =>
        WarmupSlots
            .Where(slot => settings.Slots.TryGetValue(slot.ToString(), out var types) && types.Length > 0)
            .SelectMany(slot => Enumerable.Range(0, 2).Select(i => new WardrobeItem
            {
                Id = Guid.NewGuid(),
                Name = $"warmup {slot} {i}",
                AttributeType = settings.Slots[slot.ToString()][0],
                VisualEmbedding = new float[WarmupEmbeddingDim],
                EmbeddingModel = WarmupEmbeddingModel,
            }))
            .ToList();
}
