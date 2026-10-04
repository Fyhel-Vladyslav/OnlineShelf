using OutfitNetworkService.Protos;
using OutfitOfferService.src.OutfitOfferService.Common.Interfaces;
using OutfitOfferService.src.OutfitOfferService.Common.Models;
using ModelPairwiseScore = OutfitOfferService.src.OutfitOfferService.Common.Models.PairwiseScore;

namespace OutfitOfferService.src.OutfitOfferService.Host.Services.Grpc;

/// <summary>IOutfitScorer поверх моделі OutfitNetworkService (gRPC ScoreOutfits, пакетами).</summary>
public sealed class NetworkOutfitScorer(OutfitNetworkService.Protos.OutfitNetworkService.OutfitNetworkServiceClient client) : IOutfitScorer
{
    // Обмеження розміру одного gRPC-повідомлення: 512-вимірні ембединги × кілька речей × сотні кандидатів
    private const int BatchSize = 200;

    public async Task<IReadOnlyList<OutfitScore>> ScoreAsync(
        IReadOnlyList<IReadOnlyList<WardrobeItem>> outfits,
        CancellationToken cancellationToken = default)
    {
        var results = new List<OutfitScore>(outfits.Count);

        foreach (var batch in outfits.Chunk(BatchSize))
        {
            var request = new ScoreOutfitsGrpcRequest();
            request.Outfits.AddRange(batch.Select(ToRequest));

            var response = await client.ScoreOutfitsAsync(request, cancellationToken: cancellationToken);
            results.AddRange(response.Results.Select(ToScore));
        }

        return results;
    }

    private static ScoreOutfitGrpcRequest ToRequest(IReadOnlyList<WardrobeItem> outfit)
    {
        var request = new ScoreOutfitGrpcRequest();
        request.Items.AddRange(outfit.Select(ToNode));
        return request;
    }

    private static ItemNode ToNode(WardrobeItem item)
    {
        var node = new ItemNode
        {
            ItemId = item.Id.ToString(),
            AttributeType = item.AttributeType,
            IsVirtual = item.IsVirtual,
            AttributeColorMain = item.ColorMain,
            AttributeColorSecond = item.ColorSecond,
            AttributeSeason = item.AttributeSeason,
            AttributePattern = item.AttributePattern,
            AttributeMatterial = item.AttributeMaterial,
        };
        node.FeatureVector.AddRange(item.VisualEmbedding);
        return node;
    }

    private static OutfitScore ToScore(ScoreOutfitGrpcResponse response) => new(
        response.FinalScore,
        response.GraphLevelScore,
        response.PenaltyMultiplier,
        response.AppliedPenalties.ToDictionary(kv => kv.Key, kv => kv.Value),
        response.PairwiseScores.Select(p => new ModelPairwiseScore(p.ItemIdA, p.ItemIdB, p.Score)).ToList());
}
