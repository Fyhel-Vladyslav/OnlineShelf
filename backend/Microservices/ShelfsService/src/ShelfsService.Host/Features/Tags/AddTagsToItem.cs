
using ErrorOr;
using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ShelfsService.Extentions;
using ShelfsService.src.ShelfsService.Common;
using ShelfsService.src.ShelfsService.Common.DTOs.Items;
using ShelfsService.src.ShelfsService.Common.DTOs.Shelfs;
using ShelfsService.src.ShelfsService.Common.DTOs.Tags;
using ShelfsService.src.ShelfsService.Common.Interfaces;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;

namespace ShelfsService.src.ShelfsService.Host.Features.Tags;

public sealed record AddTagsToItemCommand(TagsListDto newTags) : IRequest<ErrorOr<ItemDto>>;
internal sealed class AddTagsToItemCommandValidator : AbstractValidator<AddTagsToItemCommand>
{

    public AddTagsToItemCommandValidator()
    {

        RuleFor(v => v.newTags.ItemId)
            .NotEmpty().WithMessage("Item Id is required.")
            ;

        RuleFor(v => v.newTags.Tags)
          .NotEmpty().WithMessage("Tags list cannot be empty.")
          .Must(tags => tags.Count <= 100).WithMessage("Maximum amount of tags per operation is 100");
    }
}
public class AddTagsToItemCommandHandler(
    IItemRepository repos
    ) : Endpoint<AddTagsToItemCommand, ItemDto>
{
    public override void Configure()
    {
        Post(ApiRoutes.AddTagsToItem);
        AllowAnonymous();
    }

    public override async Task HandleAsync(AddTagsToItemCommand request, CancellationToken ct)
    {
        var item = await repos.GetItemByIdAsync(request.newTags.ItemId, ct);
        if (item == null)
        {
            Logger.LogError("Item with Id [{ItemId}] not found", request.newTags.ItemId);
            await Send.ResultAsync(TypedResults.NotFound($"Item with Id [{request.newTags.ItemId}] not found"));
            return;
        }

        foreach(var tagName in request.newTags.Tags)
        {
            if (!item.Tags.Any(t => t.Name == tagName))
            {
                item.Tags.Add(new ItemTag
                {
                    ItemId = item.Id,
                    Name = tagName,
                    TagTypeId = 1 // Default TagTypeId, adjust as necessary
                });
            }
        }

        await Send.OkAsync(item.ToDto(), cancellation: ct);
    }

}