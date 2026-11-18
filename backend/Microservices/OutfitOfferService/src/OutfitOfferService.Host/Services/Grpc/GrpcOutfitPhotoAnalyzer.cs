using Clothing;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using OutfitOfferService.src.OutfitOfferService.Common.Interfaces;
using OutfitOfferService.src.OutfitOfferService.Common.Models;
using OutfitOfferService.src.OutfitOfferService.Common.Models.Reference;
using OutfitOfferService.src.OutfitOfferService.Host.Services.Reference;

namespace OutfitOfferService.src.OutfitOfferService.Host.Services.Grpc;

/// <summary>Розбір фото людини на речі: ParseImageService.AnalyzeOutfit.</summary>
public sealed class GrpcOutfitPhotoAnalyzer(
    ClothingAnalyzer.ClothingAnalyzerClient client,
    IOptions<ReferenceProcessingOptions> options,
    TimeProvider time) : IOutfitPhotoAnalyzer
{
    public async Task<OutfitAnalysis> AnalyzeAsync(byte[] imageBytes, CancellationToken ct)
    {
        // Перший виклик після рестарту Parse довгий (завантаження ваг), тому дедлайн щедрий
        var deadline = time.GetUtcNow().UtcDateTime.AddSeconds(options.Value.AnalyzeDeadlineSeconds);
        var response = await client.AnalyzeOutfitAsync(
            new AnalyzeOutfitRequest { ImageData = ByteString.CopyFrom(imageBytes) },
            deadline: deadline, cancellationToken: ct);

        return new OutfitAnalysis(
            response.Success,
            string.IsNullOrEmpty(response.ErrorMessage) ? null : response.ErrorMessage,
            response.ImageWidth,
            response.ImageHeight,
            response.EmbeddingModel,
            response.Items.Select(ToItem).ToList());
    }

    private static AnalyzedItem ToItem(Clothing.OutfitItem item)
    {
        var attributes = item.Attributes ?? new ClothingAttributes();
        var box = item.Box ?? new BoundingBox();
        return new AnalyzedItem(
            Enum.TryParse<OutfitSlot>(item.Slot, ignoreCase: true, out var slot) ? slot : OutfitSlot.Unknown,
            attributes.AttributeType,
            attributes.AttributeSeason,
            attributes.AttributePattern,
            attributes.AttributeMaterial,
            attributes.AttributeColorMain,
            attributes.AttributeColorSecond,
            attributes.Confidence,
            new AnalyzedBox(box.X1, box.Y1, box.X2, box.Y2),
            item.Embedding.ToArray());
    }
}
