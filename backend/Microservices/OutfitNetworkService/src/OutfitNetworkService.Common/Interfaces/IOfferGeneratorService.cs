using OutfitNetworkService.src.OutfitNetworkService.Host.Services.OutfitCompatibilityScorer;
using OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities;

namespace OutfitNetworkService.src.OutfitNetworkService.Common.Interfaces;
public interface IOfferGeneratorService
{
    Task<OutfitCompatibilityResult> EvaluateAsync(
        IReadOnlyList<ItemNode> candidateItems,
        CancellationToken cancellationToken = default);
}