using ErrorOr;
using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using ShelfsService.Extentions;
using ShelfsService.src.ShelfsService.Common;
using ShelfsService.src.ShelfsService.Common.DTOs.Shelfs;
using ShelfsService.src.ShelfsService.Common.Interfaces;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;

namespace ShelfsService.src.ShelfsService.Host.Features.Items;

public sealed record MoveItemRequest(Guid ItemId, Guid NewShelfId);


public class MoveItemEndpoint(
    IShelfsRepository shelfRepos,
    IItemRepository itemsRepos
    ) : Endpoint<MoveItemRequest> // Removed MediatR IRequest interface from here
{
    public override void Configure()
    {
        Post(ApiRoutes.MoveItem);
        // Note: You have AllowAnonymous() but then check for User.GetUserId(). 
        // If it's required, use Roles or Policies instead of manual checks.
        AllowAnonymous();
    }

    public override async Task HandleAsync(MoveItemRequest req, CancellationToken ct)
    {
        // 1. Authentication Check
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // 2. Data Retrieval (Use 'await' instead of '.Result' to avoid deadlocks)
        var newShelf = await shelfRepos.GetShelfByIdAsync(req.NewShelfId, ct);
        var item = await itemsRepos.GetItemByIdAsync(req.ItemId, ct);
        
        if (item.ShelfId == req.NewShelfId)
        {
            await Send.OkAsync(ct);
            return;
        }
        if (newShelf == null || item == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // 3. Authorization / Ownership Check
        if (userId != newShelf.UserId || userId != item.UserId)
        {
            await Send.ForbiddenAsync(ct); // Use Forbidden (403) for ownership issues
            return;
        }


        item.ShelfId = req.NewShelfId;

        var newItem = await itemsRepos.MoveItemAsync(item, newShelf, ct);

        await Send.OkAsync(newItem.ToDto(), ct);
    }
}