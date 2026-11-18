using OutfitOfferService.src.OutfitOfferService.Common.Models;

namespace OutfitOfferService.src.OutfitOfferService.Common.Interfaces;

/// <summary>Джерело гардероба користувача (ShelfsService по gRPC).</summary>
public interface IWardrobeProvider
{
    Task<IReadOnlyList<WardrobeItem>> GetWardrobeAsync(Guid userId, bool onlyFavorite, CancellationToken cancellationToken = default);
}
