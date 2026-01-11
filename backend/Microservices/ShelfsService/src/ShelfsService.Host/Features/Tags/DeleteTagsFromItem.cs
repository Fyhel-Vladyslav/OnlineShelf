using ErrorOr;
using FastEndpoints;
using FluentValidation;
using MediatR;
using ShelfsService.Extentions;
using ShelfsService.src.ShelfsService.Common;
using ShelfsService.src.ShelfsService.Common.DTOs.Items;
using ShelfsService.src.ShelfsService.Common.DTOs.Tags;
using ShelfsService.src.ShelfsService.Common.Interfaces;

namespace ShelfsService.src.ShelfsService.Host.Features.Tags;

public sealed record DeleteTagsFromItemCommand(TagsListDto newTags) : IRequest<ErrorOr<ItemDto>>;
internal sealed class DeleteTagsFromItemCommandValidator : AbstractValidator<AddTagsToItemCommand>
{

    public DeleteTagsFromItemCommandValidator()
    {

        RuleFor(v => v.newTags.ItemId)
            .NotEmpty().WithMessage("Item Id is required.")
            ;

        RuleFor(v => v.newTags.Tags)
          .NotEmpty().WithMessage("Tags list cannot be empty.")
          .Must(tags => tags.Count <= 100).WithMessage("Maximum amount of tags per operation is 100");
    }
}
public class DeleteTagsFromItemCommandHandler(
    IItemRepository repos
    ) : Endpoint<DeleteTagsFromItemCommand, ItemDto>
{
    public override void Configure()
    {
        Post(ApiRoutes.DeleteTagsFromItem);
        AllowAnonymous();
    }

    public override async Task HandleAsync(DeleteTagsFromItemCommand request, CancellationToken ct)
    {
        var item = await repos.GetItemByIdAsync(request.newTags.ItemId, ct);
        if (item == null)
        {
            Logger.LogError("Item with Id [{ItemId}] not found", request.newTags.ItemId);
            await Send.ResultAsync(TypedResults.NotFound($"Item with Id [{request.newTags.ItemId}] not found"));
            return;
        }

        var uniqueTags = request.newTags.Tags.Distinct().ToList();

        foreach (var tagName in uniqueTags)
        {
            if (item.Tags.Any(t => t.Name == tagName))
            {
                var tagId = await repos.DeleteTagFromItemAsync(tagName, item.Id, ct);
                if(tagId == Guid.Empty)                
                    Logger.LogWarning("Tag [{TagName}] not found on Item with Id [{ItemId}]", tagName, request.newTags.ItemId);
                
                else
                    Logger.LogInformation("Tag [{TagName}] removed from Item with Id [{ItemId}]", tagName, request.newTags.ItemId);
            }
        }


        item = await repos.GetItemByIdAsync(request.newTags.ItemId, ct);

        await Send.OkAsync(item.ToDto(), cancellation: ct);
    }

}