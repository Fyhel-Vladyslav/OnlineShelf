using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using UserService.Data;
using UserService.Models;
using FastEndpoints;

namespace UserService.Features.Users;

sealed record GetAllUsersResponse(List<User> users); 

class GetAllUsersEndpoint : EndpointWithoutRequest<GetAllUsersResponse> 
{
    private readonly DataContext _dbContext;

    public GetAllUsersEndpoint(DataContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/api/users");
        AllowAnonymous();
    }

    public override async Task<GetAllUsersResponse> ExecuteAsync(CancellationToken ct)
    {
        var users = await _dbContext.Users.ToListAsync();
        return new GetAllUsersResponse(users);
    }
}
