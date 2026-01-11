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
    _dbContext.Items
        .Include(i => i.Tags)
            .ThenInclude(t => t.Type)
                .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);
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

    public async Task<ItemTag> AddTagToItemAsync(string tagName, Guid itemId, CancellationToken ct = default)
    {
        var newTag = new ItemTag
        {
            Id = Guid.NewGuid(),
            Name = tagName,
            ItemId = itemId,
            TagTypeId = 1,
            Source = "User"
        };

        _dbContext.ItemTags.Add(newTag);
        await _dbContext.SaveChangesAsync(ct);

        return newTag;

    }

    public async Task<Guid> DeleteTagFromItemAsync(string tagName, Guid itemId, CancellationToken ct = default)
    {
        var tag = await _dbContext.ItemTags.FirstOrDefaultAsync(t => t.Name == tagName && t.ItemId == itemId, ct);

        if (tag == null)
        {
            _logger.LogError("Tag with Name [{TagName}] for Item Id [{ItemId}] not found", tagName, itemId);
            return Guid.Empty;
        }

        var tagId = tag.Id;

        _dbContext.ItemTags.Remove(tag);
        await _dbContext.SaveChangesAsync(ct);

        return tagId;
    }
}

