using ErrorOr;
using FastEndpoints;
using FluentValidation;
using MediatR;
using ShelfsService.Extentions;
using ShelfsService.src.ShelfsService.Common;
using ShelfsService.src.ShelfsService.Common.DTOs.Items;
using ShelfsService.src.ShelfsService.Common.DTOs.Shelfs;
using ShelfsService.src.ShelfsService.Common.Interfaces;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;

namespace ShelfsService.src.ShelfsService.Host.Features.Items;
public sealed record CreateItemCommand(CreateItemDto newItem) : IRequest<ErrorOr<ShelfDto>>;
internal sealed class CreateItemCommandValidator : AbstractValidator<CreateItemCommand>
{

    public CreateItemCommandValidator()
    {

        RuleFor(v => v.newItem.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.")
            ;
    }

}
public class CreateItemCommandHandler(
    IItemRepository repos
    ) : Endpoint<CreateItemCommand, ItemDto>
{
    public override void Configure()
    {
        Post(ApiRoutes.AddItem);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateItemCommand request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var item = FromDTO(request.newItem, userId);
        if (!await repos.CheckItemNameUniqueAsync(request.newItem.Name, userId, ct))
        {
            Logger.LogError("Item with name [{Name}] already exists", request.newItem.Name);
            await Send.ResultAsync(TypedResults.Conflict("Item with this name already exists"));
            return;
        }


        var newItem = await repos.CreateItemAsync(item, ct);

        if (newItem == null)
        {
            Logger.LogError("Failed to create item [{Name}]", request.newItem.Name);
            await Send.ResultAsync(TypedResults.Problem("Failed to create item"));
            return;
        }

        await Send.OkAsync(newItem.ToDto(), cancellation: ct);
    }

    private Item FromDTO(CreateItemDto dto, Guid userId)
    {

        var reqTime = DateTime.UtcNow;
        var newItem = new Item
        {
            Id = Guid.NewGuid(),

            ShelfId = dto.ShelfId,
            UserId = userId,
            Name = dto.Name,
            DateCreated = reqTime,
            UpdatedAt = reqTime,
            AttributeColorMain = dto.AttributeColorMain,
            AttributeColorSecond = dto.AttributeColorSecond,
            AttributeType = dto.AttributeType,
            AttributeSeason = dto.AttributeSeason,
            AttributePattern = dto.AttributePattern,
            AttributeMatterial = dto.AttributeMatterial,
            isFavorite = dto.isFavorite
        };

        return newItem;
    }

}