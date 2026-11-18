using OutfitOfferService.src.OutfitOfferService.Common.Interfaces;
using OutfitOfferService.src.OutfitOfferService.Common.Models;
using ShelfsService.Protos;
using ProtoWardrobeItem = ShelfsService.Protos.WardrobeItem;
using WardrobeItem = OutfitOfferService.src.OutfitOfferService.Common.Models.WardrobeItem;

namespace OutfitOfferService.src.OutfitOfferService.Host.Services.Grpc;

/// <summary>Гардероб користувача з ShelfsService (gRPC Wardrobe.GetWardrobe).</summary>
public sealed class ShelfsWardrobeProvider(Wardrobe.WardrobeClient client) : IWardrobeProvider
{
    public async Task<IReadOnlyList<WardrobeItem>> GetWardrobeAsync(Guid userId, bool onlyFavorite, CancellationToken cancellationToken = default)
    {
        var response = await client.GetWardrobeAsync(
            new GetWardrobeRequest { UserId = userId.ToString(), OnlyFavorite = onlyFavorite },
            cancellationToken: cancellationToken);

        return response.Items.Select(ToModel).ToList();
    }

    private static WardrobeItem ToModel(ProtoWardrobeItem item) => new()
    {
        Id = Guid.Parse(item.ItemId),
        Name = item.Name,
        BigImage = string.IsNullOrEmpty(item.BigImage) ? null : item.BigImage,
        IsFavorite = item.IsFavorite,
        ColorMain = item.AttributeColorMain,
        ColorSecond = item.AttributeColorSecond,
        AttributeType = item.AttributeType,
        AttributeSeason = item.AttributeSeason,
        AttributePattern = item.AttributePattern,
        AttributeMaterial = item.AttributeMaterial,
        VisualEmbedding = item.VisualEmbedding.ToArray(),
        EmbeddingModel = string.IsNullOrEmpty(item.EmbeddingModel) ? null : item.EmbeddingModel,
    };
}
