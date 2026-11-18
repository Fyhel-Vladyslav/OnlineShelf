using FastEndpoints;
using OutfitOfferService.src.OutfitOfferService.Common;
using OutfitOfferService.src.OutfitOfferService.Host.Services.Reference;

namespace OutfitOfferService.src.OutfitOfferService.Host.Features.Reference;

public sealed class UploadReferencePhotoRequest
{
    public Guid BatchId { get; set; }

    /// <summary>Тимчасово з форми: коли додамо JWT, userId братиметься з токена.</summary>
    public Guid OwnerUserId { get; set; }

    public IFormFile File { get; set; } = null!;
}

public sealed record UploadReferencePhotoResponse(Guid PhotoId, string Status, bool Duplicate);

/// <summary>
/// Одне фото за запит (multipart): Kestrel обмежує тіло запиту, тому сотня фото одним запитом не вміститься, а збій
/// одного файла не має губити решту. Фронтенд шле до 4 запитів паралельно.
/// </summary>
public sealed class UploadReferencePhotoEndpoint(ReferencePhotoIntake intake)
    : Endpoint<UploadReferencePhotoRequest, UploadReferencePhotoResponse>
{
    public override void Configure()
    {
        Post(ApiRoutes.ReferenceBatchPhotos);
        AllowAnonymous(); // авторизацію додамо разом для всіх сервісів; перед публічним запуском обов'язково закрити
        AllowFileUploads();
    }

    public override async Task HandleAsync(UploadReferencePhotoRequest req, CancellationToken ct)
    {
        if (req.OwnerUserId == Guid.Empty)
        {
            AddError(r => r.OwnerUserId, "OwnerUserId is required.");
        }
        if (req.File is null || req.File.Length == 0)
        {
            AddError(r => r.File, "File is required.");
        }
        ThrowIfAnyErrors();

        if (req.File!.Length > ReferencePhotoIntake.MaxFileBytes)
        {
            await Send.StringAsync("File is too large.", StatusCodes.Status413PayloadTooLarge, cancellation: ct);
            return;
        }

        await using var stream = req.File.OpenReadStream();
        using var buffer = new MemoryStream((int)req.File.Length);
        await stream.CopyToAsync(buffer, ct);

        var result = await intake.AddAsync(req.OwnerUserId, req.BatchId, req.File.FileName, buffer.ToArray(), ct);
        switch (result.Outcome)
        {
            case IntakeOutcome.Queued:
                await Send.ResponseAsync(new UploadReferencePhotoResponse(result.PhotoId!.Value, result.Status!.ToString()!, false),
                    StatusCodes.Status201Created, ct);
                break;
            case IntakeOutcome.Duplicate:
                await Send.ResponseAsync(new UploadReferencePhotoResponse(result.PhotoId!.Value, result.Status!.ToString()!, true),
                    StatusCodes.Status200OK, ct);
                break;
            case IntakeOutcome.BatchNotFound:
                await Send.NotFoundAsync(ct);
                break;
            case IntakeOutcome.TooLarge:
                await Send.StringAsync("File is too large.", StatusCodes.Status413PayloadTooLarge, cancellation: ct);
                break;
            default:
                AddError(r => r.File, result.Outcome == IntakeOutcome.UnsupportedFormat
                    ? "Unsupported format: only JPEG, PNG and WebP are accepted (HEIC must be converted)."
                    : "The file could not be read as an image.");
                ThrowIfAnyErrors();
                break;
        }
    }
}
