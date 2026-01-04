using MediatR;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;
using System.Data;
using System.Xml.Linq;

namespace ShelfsService.src.ShelfsService.Common.Interfaces;
public interface IShelfsRepository
{
    //IQueryable<Shelf> Shelfs { get; }
    //Task<Shelf?> GetShelfByIdAsync(Guid ShelfId, CancellationToken cancellationToken = default);
    //Task<Shelf?> GetShelfByLoginAsync(string login, CancellationToken cancellationToken = default);
    //Task<Shelf?> GetShelfByEmailAsync(string email, CancellationToken cancellationToken = default);
    //Task<Shelf?> GetShelfByActiveDirectoryLoginAsync(string login, CancellationToken cancellationToken = default);
    //Task<Shelf?> GetShelfByEmailVerifyTokenAsync(string token, CancellationToken cancellationToken = default);
    //Task<Shelf?> GetShelfByResetPasswordTokenAsync(string token, CancellationToken cancellationToken = default);
    //Task<Shelf> CreateShelfAsync(Shelf Shelf, CancellationToken cancellationToken);
    //Task<bool> CheckShelfLoginAndEmailUniqueAsync(string login, string email, CancellationToken cancellationToken);
    //Task UpdateShelfAsync(Shelf Shelf);
    //Task DeleteShelfAsync(Shelf Shelf);
    //Task DeleteShelfByIdAsync(Guid ShelfId);
    Task<List<Shelf>> GetAllShelfsAsync();
    Task<Item?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<Shelf?> GetShelfByIdAsync(Guid shelfId, CancellationToken cancellationToken = default);
    Task<Shelf> CreateShelfAsync(Shelf shelf, CancellationToken ct);
    Task<bool> CheckShelfNameUniqueAsync(string Name, Guid UserId, CancellationToken ct);
    Task<Guid> DeleteShelfByIdAsync(Guid ShelfId, CancellationToken ct);
    Task<Shelf> UpdateShelfAsync(Shelf shelf, CancellationToken ct);
}

