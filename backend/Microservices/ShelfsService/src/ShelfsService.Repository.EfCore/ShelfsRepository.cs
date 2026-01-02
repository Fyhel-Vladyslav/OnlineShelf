using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShelfsService.src.ShelfsService.Common.Interfaces;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;
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
    public Task<Item?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        _dbContext.Items
            .Include(i => i.Tags)
                .ThenInclude(t => t.Type)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);
}

