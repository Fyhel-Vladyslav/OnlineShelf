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

public sealed record UpdateItemCommand(UpdateItemDto newItem) : IRequest<ErrorOr<ItemDto>>;
internal sealed class UpdateItemCommandValidator : AbstractValidator<UpdateItemCommand>
{
    public UpdateItemCommandValidator()
    {
        RuleFor(v => v.newItem.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.")
            ;
    }

}
public class UpdateItemCommandHandler(
    IItemRepository repos
    ) : Endpoint<UpdateItemCommand, ItemDto>
{

    public override void Configure()
    {
        Put(ApiRoutes.UpdateItem);
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateItemCommand request, CancellationToken ct)
    {
        if (request.newItem == null)
        {
            await Send.ErrorsAsync(400, cancellation: ct);
            return;
        }
        var item = await repos.GetItemByIdAsync(request.newItem.Id);

        if (item == null)
        {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        SetItemFromDTO(item, request.newItem);
        var resShelf = await repos.UpdateItemAsync(item, ct);
        if (resShelf == null)
        {
            await Send.ErrorsAsync(500, cancellation: ct);
            return;
        }

        await Send.OkAsync(item.ToDto(), cancellation: ct);

    }

    private void SetItemFromDTO(Item item, UpdateItemDto dto)
    {
        item.Name = dto.Name;
        item.ShelfId = dto.ShelfId;
        item.BigImage = dto.BigImage;
        item.SmallImage = dto.SmallImage;
    }
}
