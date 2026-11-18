using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using OutfitOfferService.src.OutfitOfferService.Common;
using OutfitOfferService.src.OutfitOfferService.Common.Models;
using OutfitOfferService.src.OutfitOfferService.Common.Models.Reference;
using OutfitOfferService.src.OutfitOfferService.Host.Services.Reference;
using OutfitOfferService.src.OutfitOfferService.Repository.EfCore;
using OutfitOfferService.src.OutfitOfferService.Repository.EfCore.Entities;

namespace OutfitOfferService.src.OutfitOfferService.Host.Features.Reference;

// Маршрути списку, огляду й повтору. Авторизація, як і в інших маршрутах еталонів: AllowAnonymous до появи JWT.

public sealed record PhotoStatusCounts(int Queued, int Processing, int Done, int NoItems, int Failed);

public sealed record OutfitStatusCounts(int Ready, int NeedsReview, int Approved, int Rejected);

public sealed record ReferenceBatchSummary(
    Guid Id, string Name, DateTime CreatedAt, PhotoStatusCounts Photos, OutfitStatusCounts Outfits);

public sealed record ReferenceItemView(
    Guid Id, OutfitSlot Slot, int? AttributeType, int? AttributeSeason, int? AttributePattern, int? AttributeMaterial,
    string ColorMain, string ColorSecond, float Confidence, int BoxX1, int BoxY1, int BoxX2, int BoxY2);

public sealed record ReferencePhotoView(
    Guid Id, string FileName, string BigImage, int? Width, int? Height, ReferencePhotoStatus Status, string? LastError,
    Guid? OutfitId, ReferenceOutfitStatus? OutfitStatus, IReadOnlyList<ReferenceItemView> Items);

public sealed record ReferencePhotoPage(int Total, int Page, int PageSize, IReadOnlyList<ReferencePhotoView> Items);

/// <summary>Партії з лічильниками фото й образів за статусами. Лічильники рахуються в БД, без вивантаження фото.</summary>
public sealed class ListReferenceBatchesEndpoint(DataContext db) : EndpointWithoutRequest<List<ReferenceBatchSummary>>
{
    public override void Configure()
    {
        Get(ApiRoutes.ReferenceBatches);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var batches = await db.ReferenceBatches
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new { b.Id, b.Name, b.CreatedAt })
            .ToListAsync(ct);

        var photoRows = await db.ReferencePhotos
            .GroupBy(p => new { p.BatchId, p.Status })
            .Select(g => new { g.Key.BatchId, g.Key.Status, Count = g.Count() })
            .ToListAsync(ct);

        var outfitRows = await db.ReferenceOutfits
            .GroupBy(o => new { o.Photo.BatchId, o.Status })
            .Select(g => new { g.Key.BatchId, g.Key.Status, Count = g.Count() })
            .ToListAsync(ct);

        var result = batches.Select(b =>
        {
            var photos = photoRows.Where(r => r.BatchId == b.Id).ToDictionary(r => r.Status, r => r.Count);
            var outfits = outfitRows.Where(r => r.BatchId == b.Id).ToDictionary(r => r.Status, r => r.Count);
            return new ReferenceBatchSummary(
                b.Id, b.Name, b.CreatedAt,
                new PhotoStatusCounts(
                    Count(photos, ReferencePhotoStatus.Queued), Count(photos, ReferencePhotoStatus.Processing),
                    Count(photos, ReferencePhotoStatus.Done), Count(photos, ReferencePhotoStatus.NoItems),
                    Count(photos, ReferencePhotoStatus.Failed)),
                new OutfitStatusCounts(
                    Count(outfits, ReferenceOutfitStatus.Ready), Count(outfits, ReferenceOutfitStatus.NeedsReview),
                    Count(outfits, ReferenceOutfitStatus.Approved), Count(outfits, ReferenceOutfitStatus.Rejected)));
        }).ToList();

        await Send.OkAsync(result, ct);
    }

    private static int Count<TStatus>(Dictionary<TStatus, int> counts, TStatus status) where TStatus : notnull =>
        counts.GetValueOrDefault(status);
}

public sealed class ListReferencePhotosRequest
{
    public Guid BatchId { get; set; }

    /// <summary>Фільтр за статусом образу, наприклад NeedsReview для черги огляду.</summary>
    public ReferenceOutfitStatus? OutfitStatus { get; set; }

    /// <summary>Фільтр за статусом фото, наприклад Failed для повтору помилок.</summary>
    public ReferencePhotoStatus? Status { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 24;
}

/// <summary>Фото партії з образами й рамками для сітки огляду, сторінками.</summary>
public sealed class ListReferencePhotosEndpoint(DataContext db) : Endpoint<ListReferencePhotosRequest, ReferencePhotoPage>
{
    private const int MaxPageSize = 100;

    public override void Configure()
    {
        Get(ApiRoutes.ReferenceBatchPhotos);
        AllowAnonymous();
    }

    public override async Task HandleAsync(ListReferencePhotosRequest req, CancellationToken ct)
    {
        if (!await db.ReferenceBatches.AnyAsync(b => b.Id == req.BatchId, ct))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var page = Math.Max(1, req.Page);
        var pageSize = Math.Clamp(req.PageSize, 1, MaxPageSize);

        var query = db.ReferencePhotos.Where(p => p.BatchId == req.BatchId);
        if (req.OutfitStatus is { } status)
        {
            query = query.Where(p => p.Outfit != null && p.Outfit.Status == status);
        }
        if (req.Status is { } photoStatus)
        {
            query = query.Where(p => p.Status == photoStatus);
        }

        var total = await query.CountAsync(ct);
        var photos = await query
            .OrderBy(p => p.CreatedAt).ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.Outfit!).ThenInclude(o => o.Items)
            .ToListAsync(ct);

        var items = photos.Select(p => new ReferencePhotoView(
            p.Id, p.FileName, p.BigImage, p.Width, p.Height, p.Status, p.LastError,
            p.Outfit?.Id, p.Outfit?.Status,
            (p.Outfit?.Items ?? []).Select(ReferenceItemMapper.ToView).ToList())).ToList();

        await Send.OkAsync(new ReferencePhotoPage(total, page, pageSize, items), ct);
    }
}

/// <summary>Відображення речі для відповідей огляду.</summary>
internal static class ReferenceItemMapper
{
    internal static ReferenceItemView ToView(ReferenceItem i) => new(
        i.Id, i.Slot, i.AttributeType, i.AttributeSeason, i.AttributePattern, i.AttributeMaterial,
        i.ColorMain, i.ColorSecond, i.Confidence, i.BoxX1, i.BoxY1, i.BoxX2, i.BoxY2);
}

public sealed class ApproveReadyRequest
{
    public Guid BatchId { get; set; }
}

public sealed record ApproveReadyResponse(int Approved);

/// <summary>Приймає всі повні образи партії (Ready) одним натиском кнопки «Прийняти всі».</summary>
public sealed class ApproveReadyEndpoint(DataContext db, TimeProvider time) : Endpoint<ApproveReadyRequest, ApproveReadyResponse>
{
    public override void Configure()
    {
        Post(ApiRoutes.ReferenceBatchApproveReady);
        AllowAnonymous();
    }

    public override async Task HandleAsync(ApproveReadyRequest req, CancellationToken ct)
    {
        if (!await db.ReferenceBatches.AnyAsync(b => b.Id == req.BatchId, ct))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var now = time.GetUtcNow().UtcDateTime;
        var approved = await db.ReferenceOutfits
            .Where(o => o.Status == ReferenceOutfitStatus.Ready && o.Photo.BatchId == req.BatchId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, ReferenceOutfitStatus.Approved)
                .SetProperty(o => o.ReviewedAt, now), ct);

        await Send.OkAsync(new ApproveReadyResponse(approved), ct);
    }
}

public sealed class RetryReferencePhotoRequest
{
    public Guid PhotoId { get; set; }
}

/// <summary>Повторює фото зі статусом Failed: черга знову бере його без очікування.</summary>
public sealed class RetryReferencePhotoEndpoint(DataContext db, TimeProvider time) : Endpoint<RetryReferencePhotoRequest>
{
    public override void Configure()
    {
        Post(ApiRoutes.ReferencePhotoRetry);
        AllowAnonymous();
    }

    public override async Task HandleAsync(RetryReferencePhotoRequest req, CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var updated = await db.ReferencePhotos
            .Where(p => p.Id == req.PhotoId && p.Status == ReferencePhotoStatus.Failed)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, ReferencePhotoStatus.Queued)
                .SetProperty(p => p.Attempts, 0)
                .SetProperty(p => p.NextAttemptAt, now)
                .SetProperty(p => p.LockedUntil, (DateTime?)null)
                .SetProperty(p => p.LastError, (string?)null), ct);

        if (updated == 1)
        {
            await Send.OkAsync(ct);
            return;
        }

        if (await db.ReferencePhotos.AnyAsync(p => p.Id == req.PhotoId, ct))
        {
            await Send.StringAsync("Only failed photos can be retried.", StatusCodes.Status409Conflict, cancellation: ct);
        }
        else
        {
            await Send.NotFoundAsync(ct);
        }
    }
}

public sealed class UpdateReferenceOutfitRequest
{
    public Guid OutfitId { get; set; }
    public ReferenceOutfitStatus Status { get; set; }
}

/// <summary>Рішення дизайнера про образ: прийняти, відхилити або повернути на перегляд.</summary>
public sealed class UpdateReferenceOutfitEndpoint(DataContext db, TimeProvider time) : Endpoint<UpdateReferenceOutfitRequest>
{
    public override void Configure()
    {
        Patch(ApiRoutes.ReferenceOutfitById);
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateReferenceOutfitRequest req, CancellationToken ct)
    {
        var outfit = await db.ReferenceOutfits.FirstOrDefaultAsync(o => o.Id == req.OutfitId, ct);
        if (outfit is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        outfit.Status = req.Status;
        outfit.ReviewedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}

public sealed class UpdateReferenceItemRequest
{
    public Guid ItemId { get; set; }

    /// <summary>Нова роль речі в образі; null означає «не змінювати».</summary>
    public OutfitSlot? Slot { get; set; }

    /// <summary>Ключ довідника типу з ShelfsService; null означає «не змінювати».</summary>
    public int? AttributeType { get; set; }
}

/// <summary>Правка слота й типу речі. Після зміни слота статус образу перераховується, поки образ не рішено.</summary>
public sealed class UpdateReferenceItemEndpoint(DataContext db) : Endpoint<UpdateReferenceItemRequest>
{
    public override void Configure()
    {
        Patch(ApiRoutes.ReferenceItemById);
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateReferenceItemRequest req, CancellationToken ct)
    {
        if (req.Slot is OutfitSlot.Unknown)
        {
            AddError(r => r.Slot, "Slot must be a known value.");
            ThrowIfAnyErrors();
        }

        var item = await db.ReferenceItems
            .Include(i => i.Outfit).ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(i => i.Id == req.ItemId, ct);
        if (item is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (req.Slot is { } slot)
        {
            item.Slot = slot;
            RecomputeStatusIfPending(item.Outfit, item.Outfit.Items);
        }
        if (req.AttributeType is { } type)
        {
            item.AttributeType = type;
        }

        await db.SaveChangesAsync(ct);
        await Send.OkAsync(ReferenceItemMapper.ToView(item), ct);
    }

    public static void RecomputeStatusIfPending(ReferenceOutfit outfit, IEnumerable<ReferenceItem> items)
    {
        if (outfit.Status is ReferenceOutfitStatus.Approved or ReferenceOutfitStatus.Rejected)
        {
            return;
        }
        outfit.Status = ReferenceOutfitClassifier.InitialStatus(items.Select(i => i.Slot));
    }
}

public sealed class DeleteReferenceItemRequest
{
    public Guid ItemId { get; set; }
}

/// <summary>Прибирає зайву річ з образу (наприклад, помилкове виявлення на фоні).</summary>
public sealed class DeleteReferenceItemEndpoint(DataContext db) : Endpoint<DeleteReferenceItemRequest>
{
    public override void Configure()
    {
        Delete(ApiRoutes.ReferenceItemById);
        AllowAnonymous();
    }

    public override async Task HandleAsync(DeleteReferenceItemRequest req, CancellationToken ct)
    {
        var item = await db.ReferenceItems
            .Include(i => i.Outfit).ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(i => i.Id == req.ItemId, ct);
        if (item is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var outfit = item.Outfit;
        db.ReferenceItems.Remove(item);
        UpdateReferenceItemEndpoint.RecomputeStatusIfPending(outfit, outfit.Items.Where(i => i.Id != item.Id));
        await db.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
