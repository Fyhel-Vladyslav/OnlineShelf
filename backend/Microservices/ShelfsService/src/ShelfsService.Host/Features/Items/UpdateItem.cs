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
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;

namespace ShelfsService.src.ShelfsService.Host.Features.Items;

public sealed record UpdateItemCommand(
    Guid Id,
    string Name,
    Guid ShelfId,
    string? ImageName,
    IFormFile? ImageFile,
    string? AttributeColorMain,
    string? AttributeColorSecond,
    int AttributeType = 0,
    int AttributeSeason = 0,
    int AttributePattern = 0,
    int AttributeMatterial = 0,
    string? IsFavoriteString = null
) : IRequest<ErrorOr<ItemDto>>;
internal sealed class UpdateItemCommandValidator : AbstractValidator<UpdateItemCommand>
{
    public UpdateItemCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.")
            ;
    }

}
public class UpdateItemCommandHandler(
    IItemRepository repos,
    ImageProcessor.ImageProcessorClient imageClient
    ) : Endpoint<UpdateItemCommand, ItemDto>
{

    public override void Configure()
    {
        Put(ApiRoutes.UpdateItem);
        AllowFileUploads();
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateItemCommand request, CancellationToken ct)
    {
        if (request == null)
        {
            await Send.ErrorsAsync(400, cancellation: ct);
            return;
        }

        var item = await repos.GetItemByIdAsync(request.Id);

        if (item == null)
        {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        SetItemFromDTO(item, request);


        // 1. Якщо файлнейма немає - починаємо логіку оновлення
        if (request.ImageName == null)
        {
            // 2. Якщо файл раніше був але в реквесті файлнейма нема - значить треба видалити файл
            if (item.BigImage != null)
            {
                //TODO: видалити файл на imageservice 

                item.BigImage = null;
                item.SmallImage = null;
            }


            // 3. Якщо файл прийшов - передаємо його на image service
                if (request.ImageFile is { Length: > 0 })
                {
                    string bigImageName = null;
                    string smallImageName = null;
                    using var stream = request.ImageFile.OpenReadStream();
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms, ct);

                    // 3. Викликаємо ImageService через gRPC
                    var grpcRequest = new UploadImageRequest
                    {
                        Data = Google.Protobuf.ByteString.CopyFrom(ms.ToArray()),
                        FileName = request.ImageFile.FileName
                    };

                    var response = await imageClient.UploadImageAsync(grpcRequest, cancellationToken: ct);

                    if (response.Success)
                    {
                        bigImageName = response.BigImageName;
                        smallImageName = response.SmallImageName;
                    }
                    item.BigImage = bigImageName;
                    item.SmallImage = smallImageName;
                }
        }

        var resShelf = await repos.UpdateItemAsync(item, ct);
        if (resShelf == null)
        {
            await Send.ErrorsAsync(500, cancellation: ct);
            return;
        }

        await Send.OkAsync(item.ToDto(), cancellation: ct);

    }

    private void SetItemFromDTO(Item item, UpdateItemCommand dto)
    {
        var reqTime = DateTime.UtcNow;
        item.Name = dto.Name;
        item.ShelfId = dto.ShelfId;
        item.AttributeColorMain = dto.AttributeColorMain;
        item.AttributeColorSecond = dto.AttributeColorSecond;
        item.UpdatedAt = reqTime;
        item.AttributeType = dto.AttributeType;
        item.AttributeSeason = dto.AttributeSeason;
        item.AttributePattern = dto.AttributePattern;
        item.AttributeMatterial = dto.AttributeMatterial;
        item.isFavorite = dto.IsFavoriteString?.ToLower() == "true";

    }
}
