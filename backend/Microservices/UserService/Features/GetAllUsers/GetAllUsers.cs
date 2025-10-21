//using MediatR;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using System.Data.Common;
//using UserService.Data;
//using UserService.Models;
//using static UserService.Features.GetAllUsers.GetAllUsers;


//namespace UserService.Features.GetAllUsers
//{
//    public class GetAllUsers
//    {
//        [ApiController]
//        [Route("api/[controller]")]
//        public class GetAllUsersController : ControllerBase
//        {
//            private readonly DataContext _dbContext;
//            public GetAllUsersController(DataContext dbContext)
//            {
//                _dbContext = dbContext;
//            }

//            [HttpGet]
//            public async Task<IActionResult> GetAll()
//            {
//                var users = await _dbContext.Users.ToListAsync();
//                return Ok(users);
//            }

//        }
//    }
//}
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using UserService.Data;
using UserService.Models;

namespace UserService.Features.GetAllUsers
{
    // NO outer wrapper class.

    [ApiController]
    [Route("api/[controller]")] // This will resolve to "api/GetAllUsers"
    public class GetAllUsersController : ControllerBase // Renamed to ...Controller
    {
        private readonly DataContext _dbContext;

        // Constructor name MUST match the new class name
        public GetAllUsersController(DataContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _dbContext.Users.ToListAsync();
            return Ok(users);
        }
    }
}