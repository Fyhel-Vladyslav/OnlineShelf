using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using FastEndpoints;
using UserService.src.Models;
using UserService.src.Data;

namespace UserService.src.Features.Users;

sealed record GetAllUsersResponse(List<User> users);

class GetAllUsers : EndpointWithoutRequest<GetAllUsersResponse>
{
    private readonly DataContext _dbContext;

    public GetAllUsers(DataContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/api/users");
        Policies("AdminPolicy");
    }

    public override async Task<GetAllUsersResponse> ExecuteAsync(CancellationToken ct)
    {
        var users = await _dbContext.Users.ToListAsync();
        return new GetAllUsersResponse(users);
    }
}
