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

}
