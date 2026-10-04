using Microsoft.EntityFrameworkCore;
using ShelfsService.src.ShelfsService.Common.Interfaces;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;
using System.ComponentModel.DataAnnotations;

namespace ShelfsService.src.ShelfsService.Repository.EfCore;
    public class ItemRepository : IItemRepository
{
    private readonly ShelfsDataContext _dbContext;
    private readonly ILogger<ShelfsRepository> _logger;

    public ItemRepository(
    ShelfsDataContext dbContext,
    ILogger<ShelfsRepository> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    public Task<Item?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default) =>
    _dbContext.Items.FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);
    public async Task<bool> CheckItemNameUniqueAsync(string Name, Guid UserId, CancellationToken ct)
    { 
        var isExist = await _dbContext.Items.AnyAsync(s => s.Name == Name && s.UserId == UserId, ct);
        return !isExist;
    }

    public async Task<Item> CreateItemAsync(Item item, CancellationToken ct)
    {
        if (!_dbContext.Shelfs.Any(i => i.Id == item.ShelfId))
        {
            _logger.LogError("Shelf with Id [{ShelfId}] does not exist", item.ShelfId);
            return null;
        }
            await _dbContext.Items.AddAsync(item);
        await _dbContext.SaveChangesAsync(ct);
        return item;
    }

    public async Task<Guid> DeleteItemByIdAsync(Guid itemId, CancellationToken ct)
    {
        var item = await _dbContext.Items.FirstOrDefaultAsync(i => i.Id == itemId, ct);
        if (item == null)
            return Guid.Empty;

        _dbContext.Items.Remove(item);
        await _dbContext.SaveChangesAsync(ct);

        return itemId;
    }
    public async Task<Item> UpdateItemAsync(Item item, CancellationToken ct)
    {
        if (item == null)
            return null;

        item.UpdatedAt = DateTime.UtcNow;
        _dbContext.Items.Update(item);
        await _dbContext.SaveChangesAsync(ct);

        return item;

    }
    
    public Task<List<AttributesValue>> GetAttributesValuesAsync(CancellationToken cancellationToken = default) =>
    _dbContext.AttributesValues.ToListAsync();

    public Task<List<AttributeName>> GetAttributesAsync(CancellationToken cancellationToken = default) =>
    _dbContext.Attributes.ToListAsync();

    public Task<List<Item>> GetUserItemsAsync(Guid userId, bool onlyFavorite, CancellationToken ct) =>
        _dbContext.Items
            .AsNoTracking()
            .Where(i => i.UserId == userId && (!onlyFavorite || i.isFavorite))
            .ToListAsync(ct);

    public async Task<Item> MoveItemAsync(Item item, Shelf newShelf, CancellationToken ct)
    {
        if (item == null || newShelf == null)
            return null;
        item.ShelfId = newShelf.Id;
        item.UpdatedAt = DateTime.UtcNow;
        _dbContext.Items.Update(item);
        await _dbContext.SaveChangesAsync(ct);
        return item;
    }

}

