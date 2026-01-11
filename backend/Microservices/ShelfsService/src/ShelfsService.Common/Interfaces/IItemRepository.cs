using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;

namespace ShelfsService.src.ShelfsService.Common.Interfaces;
    public interface IItemRepository
    {
    Task<Item> CreateItemAsync(Item item, CancellationToken ct);
    Task<Item> UpdateItemAsync(Item item, CancellationToken ct);
    Task<Guid> DeleteItemByIdAsync(Guid itemId, CancellationToken ct);
    Task<bool> CheckItemNameUniqueAsync(string Name, Guid UserId, CancellationToken ct); 
    Task<Item?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<ItemTag> AddTagToItemAsync(string tagName, Guid itemId, CancellationToken cancellationToken = default);
    Task<Guid> DeleteTagFromItemAsync(string tagName, Guid itemId, CancellationToken ct = default);

}
