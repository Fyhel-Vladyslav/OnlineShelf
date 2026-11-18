using ErrorOr;
using FastEndpoints;
using FluentValidation;
using ImageService.Protos;
using MediatR;
using ShelfsService.Extentions;
using ShelfsService.src.ShelfsService.Common;
using ShelfsService.src.ShelfsService.Common.DTOs.Items;
using ShelfsService.src.ShelfsService.Common.DTOs.Shelfs;
using ShelfsService.src.ShelfsService.Common.Interfaces;
using ShelfsService.src.ShelfsService.Host.Helpers;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;
using static FastEndpoints.Ep;

namespace ShelfsService.src.ShelfsService.Host.Features.Items;
public sealed record CreateItemCommand(
    string Name,
    Guid ShelfId,
    IFormFile BigImage,
    string? AttributeColorMain,
    string? AttributeColorSecond,
    int AttributeType = 0,
    int AttributeSeason = 0,
    int AttributePattern = 0,
    int AttributeMatterial = 0,
    string? IsFavoriteString = null
) : IRequest<ErrorOr<ShelfDto>>;
internal sealed class CreateItemCommandValidator : AbstractValidator<CreateItemCommand>
{

    public CreateItemCommandValidator()
    {

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.")
            ;
    }

}
public class CreateItemCommandHandler(
    IItemRepository repos,
    ImageProcessor.ImageProcessorClient imageClient,
    IImageAnalyzer imageAnalyzer
    ) : Endpoint<CreateItemCommand, ItemDto>
{
    public override void Configure()
    {
        Post(ApiRoutes.AddItem);
        AllowFileUploads();
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

        var item = FromDTO(request, userId);
        if (!await repos.CheckItemNameUniqueAsync(request.Name, userId, ct))
        {
            Logger.LogError("Item with name [{Name}] already exists", request.Name);
            await Send.ResultAsync(TypedResults.Conflict("Item with this name already exists"));
            return;
        }

        string bigImageName = null;
        string smallImageName = null;

        // 1. Якщо файл прийшов
        if (request.BigImage is { Length: > 0 })
        {
            using var stream = request.BigImage.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);

            // 2. Викликаємо ImageService через gRPC
            var grpcRequest = new UploadImageRequest
            {
                Data = Google.Protobuf.ByteString.CopyFrom(ms.ToArray()),
                FileName = request.BigImage.FileName
            };

            var response = await imageClient.UploadImageAsync(grpcRequest, cancellationToken: ct);

            if (response.Success)
            {
                bigImageName = response.BigImageName;
                smallImageName = response.SmallImageName;

                await imageAnalyzer.ApplyVisualEmbeddingAsync(item, ms.ToArray(), Logger, ct);
            }
        }

        item.BigImage = bigImageName;
        item.SmallImage = smallImageName;


        var newItem = await repos.CreateItemAsync(item, ct);

        if (newItem == null)
        {
            Logger.LogError("Failed to create item [{Name}]", request.Name);
            await Send.ResultAsync(TypedResults.Problem("Failed to create item"));
            return;
        }

        await Send.OkAsync(newItem.ToDto(), cancellation: ct);
    }

    private Item FromDTO(CreateItemCommand dto, Guid userId)
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
            isFavorite = dto.IsFavoriteString?.ToLower() == "true"
};

        return newItem;
    }

}