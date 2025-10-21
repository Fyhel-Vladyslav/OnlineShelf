using MediatR;
using Microsoft.AspNetCore.Mvc;
using UserService.Data;
using UserService.Models;
using static UserService.Features.CreateUser.Createuser;

namespace UserService.Features.CreateUser
{
    public static class Createuser
    {
        public record Command(string Name, string Email): IRequest<User>;

        public class Handler : IRequestHandler<Command, User>
        {
            private readonly DataContext _dbContext;

            public Handler(DataContext dbContext)
            {
                _dbContext = dbContext;
            }


            public async Task<User> Handle(Command request, CancellationToken cancellationToken)
            {
                var user = new User
                {
                    Username = request.Name,
                    Email = request.Email,
                    PasswordHash = "default_hashed_password"
                };

                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return user;
            }

        }
    }


    [ApiController]
    [Route("api/[controller]")]
    public class CreateUserController : ControllerBase
    {
        private readonly IMediator _mediator;
        public CreateUserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] Command command)
        {
            var user = await _mediator.Send(command);
            return CreatedAtAction(nameof(CreateUser), new { id = user.Id }, user);
        }
    }
}
