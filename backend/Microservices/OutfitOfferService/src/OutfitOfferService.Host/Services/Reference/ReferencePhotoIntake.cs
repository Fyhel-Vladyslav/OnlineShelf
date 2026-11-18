using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OutfitOfferService.src.OutfitOfferService.Common.Interfaces;
using OutfitOfferService.src.OutfitOfferService.Common.Models.Reference;
using OutfitOfferService.src.OutfitOfferService.Repository.EfCore;
using OutfitOfferService.src.OutfitOfferService.Repository.EfCore.Entities;

namespace OutfitOfferService.src.OutfitOfferService.Host.Services.Reference;

public enum IntakeOutcome
{
    /// <summary>Фото збережено й поставлено в чергу.</summary>
    Queued,
    /// <summary>Такий файл власник уже завантажував: нової черги немає, повернуто наявне фото.</summary>
    Duplicate,
    BatchNotFound,
    UnsupportedFormat,
    TooLarge,
    /// <summary>ImageService не зміг прочитати файл як картинку.</summary>
    NotAnImage
}

public sealed record IntakeResult(IntakeOutcome Outcome, Guid? PhotoId = null, ReferencePhotoStatus? Status = null);

/// <summary>Приймає одне фото дизайнера: перевіряє формат, відсікає дублікат за SHA-256, кладе файл в ImageService і в чергу.</summary>
public sealed class ReferencePhotoIntake(
    DataContext db,
    IReferenceImageStore images,
    TimeProvider time,
    ILogger<ReferencePhotoIntake> logger)
{
    /// <summary>Тіло запиту Kestrel обмежене 30 000 000 байтами за замовчуванням: файл із запасом під multipart.</summary>
    public const int MaxFileBytes = 25 * 1024 * 1024;

    private const string PostgresUniqueViolation = "23505";

    public async Task<IntakeResult> AddAsync(Guid ownerUserId, Guid batchId, string fileName, byte[] bytes, CancellationToken ct)
    {
        if (bytes.Length > MaxFileBytes)
        {
            return new IntakeResult(IntakeOutcome.TooLarge);
        }

        if (!UploadedImageFormat.IsSupported(bytes))
        {
            return new IntakeResult(IntakeOutcome.UnsupportedFormat);
        }

        if (!await db.ReferenceBatches.AnyAsync(b => b.Id == batchId && b.OwnerUserId == ownerUserId, ct))
        {
            return new IntakeResult(IntakeOutcome.BatchNotFound);
        }

        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var existing = await FindAsync(ownerUserId, hash, ct);
        if (existing is not null)
        {
            return existing;
        }

        var uploaded = await images.UploadAsync(bytes, fileName, ct);
        if (uploaded is null)
        {
            return new IntakeResult(IntakeOutcome.NotAnImage);
        }

        var now = time.GetUtcNow().UtcDateTime;
        var photo = new ReferencePhoto
        {
            Id = Guid.NewGuid(),
            BatchId = batchId,
            OwnerUserId = ownerUserId,
            ContentHash = hash,
            FileName = fileName.Length <= 260 ? fileName : fileName[..260],
            BigImage = uploaded.Value.BigImage,
            SmallImage = uploaded.Value.SmallImage,
            Status = ReferencePhotoStatus.Queued,
            NextAttemptAt = now,
            CreatedAt = now,
        };

        try
        {
            db.ReferencePhotos.Add(photo);
            await db.SaveChangesAsync(ct);
            return new IntakeResult(IntakeOutcome.Queued, photo.Id, photo.Status);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresUniqueViolation })
        {
            // Той самий файл прийшов паралельним запитом: лишаємо перше фото, а наш щойно завантажений файл прибираємо
            db.Entry(photo).State = EntityState.Detached;
            await DeleteQuietlyAsync(uploaded.Value.BigImage, uploaded.Value.SmallImage);
            return await FindAsync(ownerUserId, hash, ct) ?? throw ex;
        }
    }

    private async Task<IntakeResult?> FindAsync(Guid ownerUserId, string hash, CancellationToken ct)
    {
        var photo = await db.ReferencePhotos.AsNoTracking()
            .Where(p => p.OwnerUserId == ownerUserId && p.ContentHash == hash)
            .Select(p => new { p.Id, p.Status })
            .FirstOrDefaultAsync(ct);
        return photo is null ? null : new IntakeResult(IntakeOutcome.Duplicate, photo.Id, photo.Status);
    }

    private async Task DeleteQuietlyAsync(string bigImage, string smallImage)
    {
        try
        {
            await images.DeleteAsync(bigImage, smallImage, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not delete duplicate upload {BigImage} from ImageService", bigImage);
        }
    }
}

/// <summary>Формати, які приймає AnalyzeOutfit. HEIC з iPhone і решта відхиляються одразу, а не падають у Parse.</summary>
public static class UploadedImageFormat
{
    // Сигнатури байтами, а не рядками u8: \xFF у рядку кодується як UTF-8 у два байти
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static bool IsSupported(ReadOnlySpan<byte> bytes) =>
        bytes.StartsWith(Jpeg)
        || bytes.StartsWith(Png)
        || (bytes.Length >= 12 && bytes.StartsWith("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8));
}
