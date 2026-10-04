using OutfitOfferService.src.OutfitOfferService.Common.Models;

namespace OutfitOfferService.src.OutfitOfferService.Common.Interfaces;

/// <summary>
/// Оцінка сумісності речей в образі. OutfitOfferService не знає, як саме рахується скор:
/// реалізація за замовчуванням звертається до моделі в OutfitNetworkService (GNN / ONNX) по gRPC.
/// </summary>
public interface IOutfitScorer
{
    /// <summary>
    /// Пакетна оцінка кандидатів (кожен — щонайменше 2 речі). Результати у тому ж порядку, що й вхідні образи.
    /// </summary>
    Task<IReadOnlyList<OutfitScore>> ScoreAsync(
        IReadOnlyList<IReadOnlyList<WardrobeItem>> outfits,
        CancellationToken cancellationToken = default);
}
