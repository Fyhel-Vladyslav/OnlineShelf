using System.Data;
using Microsoft.EntityFrameworkCore;
using OutfitOfferService.src.OutfitOfferService.Common.Interfaces;
using OutfitOfferService.src.OutfitOfferService.Common.Models.Reference;
using OutfitOfferService.src.OutfitOfferService.Repository.EfCore.Entities;

namespace OutfitOfferService.src.OutfitOfferService.Repository.EfCore
{
    /// <summary>Черга еталонних фото в Postgres: забір через FOR UPDATE SKIP LOCKED, результат в одній транзакції.</summary>
    public class ReferencePhotoRepository(DataContext db, TimeProvider time) : IReferencePhotoRepository
    {
        // Забір написано SQL-ом: UPDATE ... RETURNING не можна огортати підзапитом, який EF додає до FromSql.
        // Другий вираз у WHERE повертає в чергу фото, чий замок прострочено, але лише поки лишились спроби.
        private const string ClaimSql = $"""
            UPDATE {DataContext.Schema}."ReferencePhotos" AS p
            SET "Status" = 'Processing', "LockedUntil" = @lockedUntil, "Attempts" = p."Attempts" + 1
            WHERE p."Id" = (
                SELECT c."Id" FROM {DataContext.Schema}."ReferencePhotos" AS c
                WHERE (c."Status" = 'Queued' AND c."NextAttemptAt" <= @now)
                   OR (c."Status" = 'Processing' AND c."LockedUntil" < @now AND c."Attempts" < @maxAttempts)
                ORDER BY c."NextAttemptAt", c."CreatedAt"
                LIMIT 1
                FOR UPDATE SKIP LOCKED)
            RETURNING p."Id", p."BigImage", p."Attempts"
            """;

        public async Task<ClaimedReferencePhoto?> ClaimNextAsync(TimeSpan lockDuration, int maxAttempts, CancellationToken ct)
        {
            var now = time.GetUtcNow().UtcDateTime;
            var connection = db.Database.GetDbConnection();
            var wasClosed = connection.State != ConnectionState.Open;
            if (wasClosed)
            {
                await connection.OpenAsync(ct);
            }

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = ClaimSql;
                AddParameter(command, "now", now);
                AddParameter(command, "lockedUntil", now + lockDuration);
                AddParameter(command, "maxAttempts", maxAttempts);

                await using var reader = await command.ExecuteReaderAsync(ct);
                return await reader.ReadAsync(ct)
                    ? new ClaimedReferencePhoto(reader.GetGuid(0), reader.GetString(1), reader.GetInt32(2))
                    : null;
            }
            finally
            {
                if (wasClosed)
                {
                    await connection.CloseAsync();
                }
            }
        }

        public Task<int> FailExhaustedAsync(int maxAttempts, CancellationToken ct)
        {
            var now = time.GetUtcNow().UtcDateTime;
            return db.ReferencePhotos
                .Where(p => p.Status == ReferencePhotoStatus.Processing && p.LockedUntil < now && p.Attempts >= maxAttempts)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Status, ReferencePhotoStatus.Failed)
                    .SetProperty(p => p.LockedUntil, (DateTime?)null)
                    .SetProperty(p => p.LastError, "Processing was interrupted too many times"), ct);
        }

        public async Task CompleteAsync(Guid photoId, OutfitAnalysis analysis, ReferenceOutfitStatus outfitStatus, CancellationToken ct)
        {
            var now = time.GetUtcNow().UtcDateTime;
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            var photo = await db.ReferencePhotos.FirstAsync(p => p.Id == photoId, ct);

            // Повторна обробка (нова модель ембедингів, повтор після помилки) замінює попередній образ
            await db.ReferenceOutfits.Where(o => o.PhotoId == photoId).ExecuteDeleteAsync(ct);

            photo.Width = analysis.ImageWidth > 0 ? analysis.ImageWidth : null;
            photo.Height = analysis.ImageHeight > 0 ? analysis.ImageHeight : null;
            photo.LockedUntil = null;
            photo.LastError = null;
            photo.ProcessedAt = now;

            if (analysis.Items.Count == 0)
            {
                photo.Status = ReferencePhotoStatus.NoItems;
            }
            else
            {
                photo.Status = ReferencePhotoStatus.Done;
                db.ReferenceOutfits.Add(new ReferenceOutfit
                {
                    Id = Guid.NewGuid(),
                    PhotoId = photoId,
                    Status = outfitStatus,
                    EmbeddingModel = analysis.EmbeddingModel,
                    CreatedAt = now,
                    Items = analysis.Items.Select(i => new ReferenceItem
                    {
                        Id = Guid.NewGuid(),
                        Slot = i.Slot,
                        AttributeType = NullIfUnset(i.AttributeType),
                        AttributeSeason = NullIfUnset(i.AttributeSeason),
                        AttributePattern = NullIfUnset(i.AttributePattern),
                        AttributeMaterial = NullIfUnset(i.AttributeMaterial),
                        ColorMain = i.ColorMain,
                        ColorSecond = i.ColorSecond,
                        Confidence = i.Confidence,
                        BoxX1 = i.Box.X1,
                        BoxY1 = i.Box.Y1,
                        BoxX2 = i.Box.X2,
                        BoxY2 = i.Box.Y2,
                        Embedding = i.Embedding,
                    }).ToList(),
                });
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }

        public Task RetryLaterAsync(Guid photoId, string error, DateTime nextAttemptAt, CancellationToken ct) =>
            db.ReferencePhotos.Where(p => p.Id == photoId).ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, ReferencePhotoStatus.Queued)
                .SetProperty(p => p.NextAttemptAt, nextAttemptAt)
                .SetProperty(p => p.LockedUntil, (DateTime?)null)
                .SetProperty(p => p.LastError, Truncate(error)), ct);

        public Task FailAsync(Guid photoId, string error, CancellationToken ct) =>
            db.ReferencePhotos.Where(p => p.Id == photoId).ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, ReferencePhotoStatus.Failed)
                .SetProperty(p => p.LockedUntil, (DateTime?)null)
                .SetProperty(p => p.LastError, Truncate(error)), ct);

        private static int? NullIfUnset(int attribute) => attribute > 0 ? attribute : null;

        private static string Truncate(string error) => error.Length <= 2000 ? error : error[..2000];

        private static void AddParameter(IDbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }
    }
}
