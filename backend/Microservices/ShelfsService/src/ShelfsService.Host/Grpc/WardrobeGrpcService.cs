using Grpc.Core;
using ShelfsService.Protos;
using ShelfsService.src.ShelfsService.Common.Interfaces;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;

namespace ShelfsService.src.ShelfsService.Host.Grpc;

public sealed class WardrobeGrpcService(IItemRepository repos) : Wardrobe.WardrobeBase
{
    public override async Task<GetWardrobeResponse> GetWardrobe(GetWardrobeRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid user_id '{request.UserId}'"));
        }

        var items = await repos.GetUserItemsAsync(userId, request.OnlyFavorite, context.CancellationToken);

        var response = new GetWardrobeResponse();
        response.Items.AddRange(items.Select(ToProto));
        return response;
    }

    private static WardrobeItem ToProto(Item item)
    {
        var proto = new WardrobeItem
        {
            ItemId = item.Id.ToString(),
            Name = item.Name ?? string.Empty,
            ShelfId = item.ShelfId.ToString(),
            BigImage = item.BigImage ?? string.Empty,
            IsFavorite = item.isFavorite,
            AttributeColorMain = item.AttributeColorMain ?? string.Empty,
            AttributeColorSecond = item.AttributeColorSecond ?? string.Empty,
            AttributeType = item.AttributeType,
            AttributeSeason = item.AttributeSeason,
            AttributePattern = item.AttributePattern,
            AttributeMaterial = item.AttributeMatterial,
            EmbeddingModel = item.EmbeddingModel ?? string.Empty,
        };

        if (item.VisualEmbedding is { Length: > 0 })
        {
            proto.VisualEmbedding.AddRange(item.VisualEmbedding);
        }

        return proto;
    }
}
