using UserService.src.UserService.Repository.EfCore.Entities;

namespace UserService.src.UserService.Common.Interfaces;
public interface IUserRepository
{
    IQueryable<User> Users { get; }
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<User?> GetUserByLoginAsync(string login, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetUserByActiveDirectoryLoginAsync(string login, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmailVerifyTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<User?> GetUserByResetPasswordTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<User> CreateUserAsync(User user, CancellationToken cancellationToken);
    Task<bool> CheckUserLoginAndEmailUniqueAsync(string login, string email, CancellationToken cancellationToken);
    Task UpdateUserAsync(User user);
    Task DeleteUserAsync(User user);
    Task DeleteUserByIdAsync(Guid userId);
    Task<List<User>> GetAllUsersAsync();
    Task<User> AddRoleToUser(Guid userId, Role newRole, CancellationToken ct);

    Task<List<Role>> GetUserRoles();
    Task<bool> VerifyUserPasswordAsync(User user, string password, CancellationToken cancellationToken = default);
}

