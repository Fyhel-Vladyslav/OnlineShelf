using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Threading;
using UserService.src.UserService.Common;
using UserService.src.UserService.Common.Interfaces;
using UserService.src.UserService.Repository.EfCore.Entities;

namespace UserService.src.UserService.Repository.EfCore;
public class UserRepository : IUserRepository
{
    private readonly DataContext _dbContext;
    private readonly ILogger<UserRepository> _logger;
    private readonly IPasswordHasher<User> _passwordHasher;


    public UserRepository(
        DataContext dbContext, 
        ILogger<UserRepository> logger,
        IPasswordHasher<User> passwordHasher
        )
    {
        _dbContext = dbContext;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    public IQueryable<User> Users => _dbContext.Users;

    public Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _dbContext.Users.Include(u => u.Roles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken: cancellationToken);

    public async Task<bool> CheckUserLoginAndEmailUniqueAsync(string login, string email, CancellationToken cancellationToken = default)
    {
        return !await _dbContext.Users.AnyAsync(p => p.Login == login || p.Email == email, cancellationToken);
    }

    public Task<User?> GetUserByLoginAsync(string login, CancellationToken cancellationToken = default) =>
        _dbContext.Users.Include(u => u.Roles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Login.ToLower() == login.ToLower(), cancellationToken: cancellationToken);
    
    public Task<User?> GetUserByRefreshToken(string refreshToken, CancellationToken cancellationToken = default) =>
        _dbContext.Users.Include(u => u.Roles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.RefreshToken.ToLower() == refreshToken.ToLower(), cancellationToken: cancellationToken);
 
    
    public Task<List<User>> GetAllUsersAsync() =>
        _dbContext.Users.Include(u => u.Roles).ThenInclude(ur => ur.Role).ToListAsync();

    public Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _dbContext.Users.Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken: cancellationToken);


    public Task<User?> GetUserByActiveDirectoryLoginAsync(string login, CancellationToken cancellationToken = default) =>
        _dbContext.Users.Include(u => u.Roles)
            .FirstOrDefaultAsync(p => p.Login!.ToLower() == login.ToLower(), cancellationToken: cancellationToken);


    public Task<User?> GetUserByEmailVerifyTokenAsync(string token, CancellationToken cancellationToken = default) =>
        _dbContext.Users.Include(u => u.Roles)
        .FirstOrDefaultAsync(u => u.Email == token, cancellationToken: cancellationToken);


    //public Task<User?> GetUserByResetPasswordTokenAsync(string token, CancellationToken cancellationToken = default) =>
    //    _dbContext.Users.FirstOrDefaultAsync(u => u.ResetPasswordToken == token, cancellationToken: cancellationToken);

    public async Task<User> CreateUserAsync(User user, CancellationToken ct = default)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(ct);
        return user;
    }


    public async Task UpdateUserAsync(User user)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

    }


    public async Task DeleteUserAsync(User user)
    {
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();
    }


    public async Task DeleteUserByIdAsync(Guid userId)
    {
        _dbContext.Users.Remove(new()
        {
            Id = userId
        });

        await _dbContext.SaveChangesAsync();
    }


    //public async Task<IReadOnlyList<string>> GetPasswordCommonAsync(CancellationToken cancellationToken = default) =>
    //    await _dbContext.CommonPasswords.AsNoTracking()
    //        .Select(x => x.Password)
    //        .ToListAsync(cancellationToken: cancellationToken);

    public Task<User?> GetUserByResetPasswordTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task SaveRefreshTokenAsync(Guid userId, string refreshToken, CancellationToken ct)
    {
        var user = await GetUserByIdAsync(userId, ct);
        if (user == null)
            //TODO log error
            return;
        
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry =  DateTime.UtcNow.AddDays(30);
        await _dbContext.SaveChangesAsync();
    }
    
    public async Task<List<Role>> GetUserRoles()
    {
        return await _dbContext.Roles.ToListAsync();
    }

    public async Task<User> AddRoleToUser(Guid userId, Role role, CancellationToken cancellationToken = default)
    {
        var user = await GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            _logger.LogError("User with Id [{UserId}] not found", userId);
            return null;
        }

        if (user.Roles.Any(r => r.Role == role))
        {
            _logger.LogError("User with Id [{UserId}] already has role [{Role}]", userId, role);
            return user;
        }

        user.Roles.Add(new UserRoleLink
        {
            Role = role,
            UserId = user.Id
        });
        await UpdateUserAsync(user);

        return user;
    }
    public async Task<User> RemoveRoleFromUser(Guid userId, Role role, CancellationToken ct)
    {
        var user = await GetUserByIdAsync(userId, ct);
        if (user == null)
        {
            _logger.LogError("User with Id [{UserId}] not found", userId);
            return null;
        }

        var usrRoleLink = user.Roles.Where(r => r.Role == role).FirstOrDefault();

        if (usrRoleLink==null)
        {
            _logger.LogError("User with Id [{UserId}] doesn't have role [{Role}]", userId, role);
            return user;
        }

        user.Roles.Remove(usrRoleLink);
        await UpdateUserAsync(user);

        return user;
    }

    public async Task<bool> VerifyUserPasswordAsync(User user, string password, CancellationToken cancellationToken = default)
    {
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Success;

    }
}
