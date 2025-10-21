//namespace UserService.Repositories;

//using Microsoft.EntityFrameworkCore;
//using UserService.Data;
//using UserService.Models;

//public class UserRepository : IUserRepository
//{
//    private readonly Data.DataContext _context;

//    public UserRepository(Data.DataContext context)
//    {
//        _context = context;
//    }

//    public async Task<IEnumerable<User>> GetAllUsersAsync()
//    {
//        return await _context.Users.ToListAsync();
//    }

//    public async Task<User?> GetUserByIdAsync(int id)
//    {
//        return await _context.Users.FindAsync(id);
//    }

//    public async Task<User> AddUserAsync(User user)
//    {
//        _context.Users.Add(user);
//        await _context.SaveChangesAsync();
//        return user;
//    }

//    public async Task<bool> UpdateUserAsync(User user)
//    {
//        // For simplicity, we're assuming the user object is fully tracked or attached
//        _context.Entry(user).State = EntityState.Modified;
//        try
//        {
//            await _context.SaveChangesAsync();
//            return true;
//        }
//        catch (DbUpdateConcurrencyException)
//        {
//            if (!_context.Users.Any(e => e.Id == user.Id))
//            {
//                return false; // User not found
//            }
//            throw;
//        }
//    }

//    public async Task<bool> DeleteUserAsync(int id)
//    {
//        var user = await _context.Users.FindAsync(id);
//        if (user == null)
//        {
//            return false;
//        }

//        _context.Users.Remove(user);
//        await _context.SaveChangesAsync();
//        return true;
//    }
//}