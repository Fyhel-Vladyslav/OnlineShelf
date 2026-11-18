using Grpc.Core;
using Microsoft.Extensions.Options;
using OutfitOfferService.src.OutfitOfferService.Common.Interfaces;
using OutfitOfferService.src.OutfitOfferService.Common.Models.Reference;

namespace OutfitOfferService.src.OutfitOfferService.Host.Services.Reference;

/// <summary>
/// Одна ітерація воркера: бере фото з черги, читає оригінал з ImageService, розбирає його в Parse
/// і зберігає образ, речі та статус фото. Тимчасові збої повертають фото в чергу з паузою, решта завершується Failed.
/// </summary>
public sealed class ReferencePhotoProcessor(
    IReferencePhotoRepository repository,
    IReferenceImageStore images,
    IOutfitPhotoAnalyzer analyzer,
    IOptions<ReferenceProcessingOptions> options,
    TimeProvider time,
    ILogger<ReferencePhotoProcessor> logger)
{
    private readonly ReferenceProcessingOptions _settings = options.Value;

    /// <returns>true, якщо фото було в черзі й оброблене (чи повернене на повтор); false, якщо черга порожня.</returns>
    public async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        var exhausted = await repository.FailExhaustedAsync(_settings.MaxAttempts, ct);
        if (exhausted > 0)
        {
            logger.LogWarning("{Count} reference photo(s) failed: processing was interrupted {Attempts} times",
                exhausted, _settings.MaxAttempts);
        }

        var claimed = await repository.ClaimNextAsync(TimeSpan.FromMinutes(_settings.LockMinutes), _settings.MaxAttempts, ct);
        if (claimed is null)
        {
            return false;
        }

        await ProcessAsync(claimed, ct);
        return true;
    }

    private async Task ProcessAsync(ClaimedReferencePhoto photo, CancellationToken ct)
    {
        try
        {
            var bytes = await images.DownloadOriginalAsync(photo.BigImage, ct);
            var analysis = await analyzer.AnalyzeAsync(bytes, ct);

            if (!analysis.Success)
            {
                // Биті байти чи формат повтором не виправляються
                await repository.FailAsync(photo.Id, analysis.ErrorMessage ?? "AnalyzeOutfit failed", ct);
                logger.LogWarning("Reference photo {PhotoId} failed: {Error}", photo.Id, analysis.ErrorMessage);
                return;
            }

            var status = ReferenceOutfitClassifier.InitialStatus(analysis.Items.Select(i => i.Slot));
            await repository.CompleteAsync(photo.Id, analysis, status, ct);
            logger.LogInformation("Reference photo {PhotoId} processed: {Items} item(s), outfit {Status}",
                photo.Id, analysis.Items.Count, analysis.Items.Count == 0 ? "none" : status);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Зупинка сервісу: фото лишається Processing, замок спливе, і його візьмуть знову
            throw;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            await repository.FailAsync(photo.Id, $"Photo {photo.BigImage} not found in ImageService", ct);
            logger.LogWarning("Reference photo {PhotoId} failed: file is missing in ImageService", photo.Id);
        }
        catch (Exception ex)
        {
            // Unavailable і DeadlineExceeded очікувані, решта теж могла бути випадковою: повторюємо, поки є спроби
            await RetryOrFailAsync(photo, ex, ct);
        }
    }

    private async Task RetryOrFailAsync(ClaimedReferencePhoto photo, Exception error, CancellationToken ct)
    {
        var message = error is RpcException rpc ? $"{rpc.StatusCode}: {rpc.Status.Detail}" : error.Message;
        if (photo.Attempts >= _settings.MaxAttempts)
        {
            await repository.FailAsync(photo.Id, message, ct);
            logger.LogError(error, "Reference photo {PhotoId} failed after {Attempts} attempts", photo.Id, photo.Attempts);
            return;
        }

        var delay = TimeSpan.FromSeconds(_settings.RetryDelaysSeconds[photo.Attempts - 1]);
        await repository.RetryLaterAsync(photo.Id, message, time.GetUtcNow().UtcDateTime + delay, ct);
        logger.LogWarning(error, "Reference photo {PhotoId} will be retried in {Delay} (attempt {Attempt} of {Max})",
            photo.Id, delay, photo.Attempts, _settings.MaxAttempts);
    }
}
