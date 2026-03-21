using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShelfsService.src.ShelfsService.Common.Interfaces;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;
using System;
using System.Threading;
using static ShelfsService.src.ShelfsService.Repository.EfCore.ShelfsRepository;

namespace ShelfsService.src.ShelfsService.Repository.EfCore;
public class ShelfsRepository : IShelfsRepository
{
    private readonly ShelfsDataContext _dbContext;
    private readonly ILogger<ShelfsRepository> _logger;


    public ShelfsRepository(
        ShelfsDataContext dbContext,
        ILogger<ShelfsRepository> logger
        )
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    public Task<List<Shelf>> GetAllShelfsAsync() =>
        _dbContext.Shelfs
            .Include(s => s.Items)
                .ToListAsync();


    public Task<Shelf?> GetShelfByIdAsync(Guid shelfId, CancellationToken cancellationToken = default) =>
    _dbContext.Shelfs
        .Include(i => i.Items).FirstOrDefaultAsync(s => s.Id == shelfId, cancellationToken);

    public async Task<Shelf> CreateShelfAsync(Shelf shelf, CancellationToken ct)
    {
        _dbContext.Shelfs.AddAsync(shelf);
        await _dbContext.SaveChangesAsync(ct);
        return shelf;
    }

    public async Task<bool> CheckShelfNameUniqueAsync(string Name, Guid UserId, CancellationToken ct)
    {
        var isExist = await _dbContext.Shelfs.AnyAsync(s => s.Name == Name && s.UserId == UserId, ct);
        return !isExist;
    }

    public async Task<Guid> DeleteShelfByIdAsync(Guid ShelfId, CancellationToken ct)
    {
        var shelf = await _dbContext.Shelfs.FirstOrDefaultAsync(s => s.Id == ShelfId, ct);
        if (shelf == null)
            return Guid.Empty;

        var shelfItems = await _dbContext.Items.Where(i => i.ShelfId == ShelfId).ToListAsync(ct);
        if(shelfItems != null)
          _dbContext.Items.RemoveRange(shelfItems);
        

        _dbContext.Shelfs.Remove(shelf);
        await _dbContext.SaveChangesAsync(ct);

        return ShelfId;

    }

    public async Task<Shelf> UpdateShelfAsync(Shelf shelf, CancellationToken ct)
    {
        if (shelf == null)
            return null;

        shelf.UpdatedAt = DateTime.UtcNow;
        _dbContext.Shelfs.Update(shelf);
        await _dbContext.SaveChangesAsync(ct);

        return shelf;

    }

}
