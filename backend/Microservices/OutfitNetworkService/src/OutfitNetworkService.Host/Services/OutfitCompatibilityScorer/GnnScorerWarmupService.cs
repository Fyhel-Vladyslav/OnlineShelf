using System.Diagnostics;
using OutfitNetworkService.src.OutfitNetworkService.Common.Interfaces;

namespace OutfitNetworkService.src.OutfitNetworkService.Host.Services.OutfitCompatibilityScorer;

/// <summary>
/// Завантажує модель сумісності й проганяє пробні графи під час старту сервісу, а не на першому запиті.
/// IHostedService.StartAsync виконується до того, як Kestrel почне приймати запити, тож перший
/// ScoreOutfits від OutfitOfferService уже йде на прогріту модель.
/// </summary>
public sealed class GnnScorerWarmupService(
    IGraphCompatibilityScorer scorer,
    IServiceScopeFactory scopeFactory,
    ILogger<GnnScorerWarmupService> logger) : IHostedService
{
    /// <summary>Шаблони OfferService дають до 4 речей, плюс закріплені аксесуари.</summary>
    public const int MaxWarmupNodes = 6;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Помилка прогріву не має валити сервіс — перші запити просто будуть повільнішими
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var onnx = scorer as OnnxGraphCompatibilityScorer;

            // ONNX Runtime: по інференсу на кожну кількість речей (нова форма входу = окрема підготовка)
            onnx?.Warmup(MaxWarmupNodes);

            // Увесь шлях запиту (оркестратор, штрафи, скорер) — щоб JIT не припадав на перший ScoreOutfits
            using (var scope = scopeFactory.CreateScope())
            {
                var orchestrator = scope.ServiceProvider.GetRequiredService<IScoringOrchestrator>();
                var nodes = OnnxGraphCompatibilityScorer.WarmupNodes(3, onnx?.EmbeddingDim ?? 0, onnx?.EmbeddingModel ?? string.Empty);
                await orchestrator.EvaluateAsync(nodes, cancellationToken);
            }

            logger.LogInformation("Outfit scoring warmed up ({Scorer} {ModelVersion}, 2..{MaxNodes} items) in {ElapsedMs} ms",
                scorer.GetType().Name, onnx?.ModelVersion ?? "-", MaxWarmupNodes, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Outfit scoring warmup failed; the first requests will be slower");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
