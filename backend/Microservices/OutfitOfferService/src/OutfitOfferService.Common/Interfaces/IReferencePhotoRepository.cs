using OutfitOfferService.src.OutfitOfferService.Common.Models.Reference;

namespace OutfitOfferService.src.OutfitOfferService.Common.Interfaces;

/// <summary>Черга еталонних фото в БД: забір, результат, повтор.</summary>
public interface IReferencePhotoRepository
{
    /// <summary>
    /// Бере наступне фото (Queued, час настав, або Processing із простроченим замком) і ставить Processing із замком.
    /// Кілька воркерів не беруть те саме фото (FOR UPDATE SKIP LOCKED). Null, якщо черга порожня.
    /// </summary>
    Task<ClaimedReferencePhoto?> ClaimNextAsync(TimeSpan lockDuration, int maxAttempts, CancellationToken ct);

    /// <summary>Переводить у Failed фото, чий замок прострочено після вичерпання спроб (контейнер падав посеред обробки).</summary>
    Task<int> FailExhaustedAsync(int maxAttempts, CancellationToken ct);

    /// <summary>Зберігає образ, речі і статус фото в одній транзакції. Порожній список речей дає NoItems.</summary>
    Task CompleteAsync(Guid photoId, OutfitAnalysis analysis, ReferenceOutfitStatus outfitStatus, CancellationToken ct);

    /// <summary>Повертає фото в чергу з наступною спробою не раніше <paramref name="nextAttemptAt"/>.</summary>
    Task RetryLaterAsync(Guid photoId, string error, DateTime nextAttemptAt, CancellationToken ct);

    Task FailAsync(Guid photoId, string error, CancellationToken ct);
}
