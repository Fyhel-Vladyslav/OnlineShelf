using FastEndpoints;
using OutfitOfferService.src.OutfitOfferService.Common;
using OutfitOfferService.src.OutfitOfferService.Repository.EfCore;
using OutfitOfferService.src.OutfitOfferService.Repository.EfCore.Entities;

namespace OutfitOfferService.src.OutfitOfferService.Host.Features.Reference;

public sealed record CreateReferenceBatchRequest
{
    /// <summary>Тимчасово з тіла запиту: коли додамо JWT, userId братиметься з токена.</summary>
    public Guid OwnerUserId { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed record ReferenceBatchResponse(Guid Id, string Name, DateTime CreatedAt);

/// <summary>Нова партія еталонних фото: дизайнер створює її перед завантаженням кожної сесії.</summary>
public sealed class CreateReferenceBatchEndpoint(DataContext db, TimeProvider time)
    : Endpoint<CreateReferenceBatchRequest, ReferenceBatchResponse>
{
    public override void Configure()
    {
        Post(ApiRoutes.ReferenceBatches);
        AllowAnonymous(); // авторизацію додамо разом для всіх сервісів; перед публічним запуском обов'язково закрити
    }

    public override async Task HandleAsync(CreateReferenceBatchRequest req, CancellationToken ct)
    {
        if (req.OwnerUserId == Guid.Empty)
        {
            AddError(r => r.OwnerUserId, "OwnerUserId is required.");
        }
        ThrowIfAnyErrors();

        var now = time.GetUtcNow().UtcDateTime;
        var name = string.IsNullOrWhiteSpace(req.Name) ? $"Batch {now:yyyy-MM-dd HH:mm}" : req.Name.Trim();
        var batch = new ReferenceBatch
        {
            Id = Guid.NewGuid(),
            OwnerUserId = req.OwnerUserId,
            Name = name.Length <= 200 ? name : name[..200],
            CreatedAt = now,
        };
        db.ReferenceBatches.Add(batch);
        await db.SaveChangesAsync(ct);

        await Send.ResponseAsync(new ReferenceBatchResponse(batch.Id, batch.Name, batch.CreatedAt), StatusCodes.Status201Created, ct);
    }
}
