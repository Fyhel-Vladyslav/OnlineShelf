namespace UserService.Controllers;

using Microsoft.AspNetCore.Mvc;
using UserService.Models;
using UserService.Repositories;

[Route("users/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    // GET: users/Users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        var users = await _userRepository.GetAllUsersAsync();
        return Ok(users);
    }

    // GET: users/Users/5
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    // POST: users/Users
    // NOTE: In a real app, you'd use a DTO (Data Transfer Object) here
    [HttpPost]
    public async Task<ActionResult<User>> PostUser(User user)
    {
        // In a real app, hash the password before saving!
        // user.PasswordHash = HashPassword(user.PasswordHash); 

        var createdUser = await _userRepository.AddUserAsync(user);

        // Returns a 201 Created status with the URI of the new resource
        return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
    }

    // PUT: users/Users/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutUser(int id, User user)
    {
        if (id != user.Id)
        {
            return BadRequest("ID mismatch.");
        }

        // This is a simplified update. A real service would fetch the existing user first.
        var success = await _userRepository.UpdateUserAsync(user);

        if (!success)
        {
            return NotFound();
        }

        return NoContent(); // 204 No Content
    }

    // DELETE: users/Users/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var success = await _userRepository.DeleteUserAsync(id);

        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}