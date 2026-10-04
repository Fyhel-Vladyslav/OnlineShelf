using Grpc.Core;
using ImageService.Protos;
using ShelfsService.src.ShelfsService.Common.Interfaces;
using ShelfsService.src.ShelfsService.Host.Helpers;

namespace ShelfsService.src.ShelfsService.Host.Features.Embeddings;

public sealed record EmbeddingBackfillResult(int Candidates, int Updated, int Failed, bool Aborted, IReadOnlyList<string> Errors);

public interface IEmbeddingBackfillService
{
    /// <summary>Дораховує візуальні ембединги для речей з фото, у яких їх ще немає. userId = null — для всіх користувачів.</summary>
    Task<EmbeddingBackfillResult> RunAsync(Guid? userId, CancellationToken ct);
}

/// <summary>
/// Backfill ембедингів для речей, створених до появи EmbedClothing (або коли ParseImageService був недоступний).
/// Фото береться з ImageService, вектор рахує ParseImageService, запис — без зміни UpdatedAt.
/// </summary>
public sealed class EmbeddingBackfillService(
    IItemRepository repos,
    ImageProcessor.ImageProcessorClient imageClient,
    IImageAnalyzer imageAnalyzer,
    ILogger<EmbeddingBackfillService> logger) : IEmbeddingBackfillService
{
    // Кілька невдач поспіль майже напевно означають, що ParseImageService недоступний — немає сенсу мучити решту речей
    private const int MaxConsecutiveFailures = 3;

    public async Task<EmbeddingBackfillResult> RunAsync(Guid? userId, CancellationToken ct)
    {
        var candidates = await repos.GetItemsMissingEmbeddingAsync(userId, ct);
        var errors = new List<string>();
        var updated = 0;
        var consecutiveFailures = 0;

        logger.LogInformation("Embedding backfill started: {Count} items without visual embedding", candidates.Count);

        foreach (var (itemId, bigImage) in candidates)
        {
            var (error, isParseFailure) = await TryEmbedAsync(itemId, bigImage, ct);
            if (error is null)
            {
                updated++;
                consecutiveFailures = 0;
                continue;
            }

            errors.Add($"{itemId}: {error}");
            logger.LogWarning("Embedding backfill failed for item {ItemId}: {Error}", itemId, error);

            // Відсутній файл у ImageService — проблема конкретної речі; рахуємо лише збої ParseImageService
            consecutiveFailures = isParseFailure ? consecutiveFailures + 1 : 0;
            if (consecutiveFailures >= MaxConsecutiveFailures)
            {
                logger.LogError("Embedding backfill aborted after {Failures} consecutive failures", consecutiveFailures);
                return new EmbeddingBackfillResult(candidates.Count, updated, errors.Count, Aborted: true, errors);
            }
        }

        logger.LogInformation("Embedding backfill finished: {Updated}/{Count} updated, {Failed} failed",
            updated, candidates.Count, errors.Count);

        return new EmbeddingBackfillResult(candidates.Count, updated, errors.Count, Aborted: false, errors);
    }

    private async Task<(string? Error, bool IsParseFailure)> TryEmbedAsync(Guid itemId, string bigImage, CancellationToken ct)
    {
        byte[] imageBytes;
        try
        {
            imageBytes = await imageClient.DownloadPhotoAsync(bigImage, ct);
        }
        catch (RpcException ex)
        {
            return ($"ImageService: {ex.StatusCode} {ex.Status.Detail}", false);
        }

        var response = await imageAnalyzer.EmbedAsync(imageBytes, ct);
        if (!response.Success || response.Embedding.Count == 0)
        {
            return ($"ParseImageService: {response.ErrorMessage}", true);
        }

        await repos.SaveVisualEmbeddingAsync(itemId, response.Embedding.ToArray(), response.EmbeddingModel, ct);
        return (null, false);
    }
}
