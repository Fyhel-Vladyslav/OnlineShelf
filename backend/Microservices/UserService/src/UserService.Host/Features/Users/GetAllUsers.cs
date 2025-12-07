using Microsoft.EntityFrameworkCore;
using FastEndpoints;
using UserService.src.UserService.Repository.EfCore.Entities;
using UserService.src.UserService.Common.Interfaces;
using UserService.src.UserService.Common.DTOs;
using UserService.Extentions;
using UserService.src.UserService.Common;

namespace UserService.src.UserService.Host.Features.Users;

sealed record GetAllUsersResponse(List<UserDto> users);

class GetAllUsers : EndpointWithoutRequest<GetAllUsersResponse>
{
    private readonly IUserRepository _repos;

    public GetAllUsers(IUserRepository repos)
    {
        _repos = repos;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Users);
        AllowAnonymous();
        //Policies("AdminPolicy");
    }

    public override async Task<GetAllUsersResponse> ExecuteAsync(CancellationToken ct)
    {
        var users = await _repos.GetAllUsersAsync();

        return new GetAllUsersResponse(
            users.Select(u => u.ToDto())
            .ToList()
            );
    }
}
