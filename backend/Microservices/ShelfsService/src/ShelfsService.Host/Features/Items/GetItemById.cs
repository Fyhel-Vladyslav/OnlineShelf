using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using ShelfsService.Extentions;
using ShelfsService.src.ShelfsService.Common;
using ShelfsService.src.ShelfsService.Common.DTOs;
using ShelfsService.src.ShelfsService.Common.Interfaces;

namespace ShelfsService.src.ShelfsService.Host.Features.Items;
internal sealed record GetItemByIdRequest
{
    public Guid ItemId { get; init; }
}

internal sealed class GetItemById : Endpoint<GetItemByIdRequest, Results<Ok<ItemDto>, NotFound>>
{
    private readonly IShelfsRepository _repos;
    public GetItemById(IShelfsRepository repos)
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
        var item = await _repos.GetItemByIdAsync(req.ItemId, ct);

        if (item is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(item.ToDto());
    }
}
