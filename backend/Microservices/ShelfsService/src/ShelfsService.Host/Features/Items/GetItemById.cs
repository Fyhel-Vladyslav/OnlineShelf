using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using ShelfsService.Extentions;
using ShelfsService.src.ShelfsService.Common;
using ShelfsService.src.ShelfsService.Common.DTOs.Items;
using ShelfsService.src.ShelfsService.Common.Interfaces;

namespace ShelfsService.src.ShelfsService.Host.Features.Items;
internal sealed record GetItemByIdRequest
{
    public Guid ItemId { get; init; }
}

internal sealed class GetItemById : Endpoint<GetItemByIdRequest, Results<Ok<ItemDto>, NotFound>>
{
    private readonly IItemRepository _repos;
    public GetItemById(IItemRepository repos)
    {
        _repos = repos;
    }

    public override void Configure()
    {
        Get(ApiRoutes.GetItemById);
        //Policies("AdminPolicy");
        AllowAnonymous();
    }

    public override async Task<Results<Ok<ItemDto>, NotFound>> ExecuteAsync(GetItemByIdRequest req, CancellationToken ct)
    {
        Logger.LogDebug("Getting item {ItemId}", req.ItemId);

        var item = await _repos.GetItemByIdAsync(req.ItemId, ct);

        if (item is null)
        {
            Logger.LogWarning("Item {ItemId} was not found", req.ItemId);
            return TypedResults.NotFound();
        }

        Logger.LogInformation("Item {ItemId} was returned", req.ItemId);

        return TypedResults.Ok(item.ToDto());
    }
}
