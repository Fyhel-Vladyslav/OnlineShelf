using Microsoft.EntityFrameworkCore;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;

namespace ShelfsService.src.ShelfsService.Common.Interfaces;
    public interface IItemRepository
    {
    public Task<List<AttributesValue>> GetAttributesValuesAsync(CancellationToken cancellationToken = default);
    public Task<List<AttributeName>> GetAttributesAsync(CancellationToken cancellationToken = default);

    Task<Item> CreateItemAsync(Item item, CancellationToken ct);
    Task<Item> UpdateItemAsync(Item item, CancellationToken ct);
    Task<Guid> DeleteItemByIdAsync(Guid itemId, CancellationToken ct);
    Task<bool> CheckItemNameUniqueAsync(string Name, Guid UserId, CancellationToken ct); 
    Task<Item?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<Item> MoveItemAsync(Item item, Shelf newShelf, CancellationToken ct);
    Task<List<Item>> GetUserItemsAsync(Guid userId, bool onlyFavorite, CancellationToken ct);

    /// <summary>Речі з фото, але без візуального ембединга (кандидати на backfill).</summary>
    Task<List<(Guid Id, string BigImage)>> GetItemsMissingEmbeddingAsync(Guid? userId, CancellationToken ct);

    /// <summary>Точковий запис ембединга без зміни UpdatedAt (це технічні дані, а не редагування речі).</summary>
    Task SaveVisualEmbeddingAsync(Guid itemId, float[] embedding, string? embeddingModel, CancellationToken ct);

}
