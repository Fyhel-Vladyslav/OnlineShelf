using Microsoft.AspNetCore.Http.HttpResults;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Serilog;
using UserService.src.UserService.Common;
using UserService.src.UserService.Repository.EfCore;
using UserService.src.UserService.Common.Interfaces;

namespace UserService.src.UserService.Host.Features.Users;
internal sealed record DeleteUserRequest
{
    public Guid UserId { get; init; }
};

internal sealed class DeleteUser : Endpoint<DeleteUserRequest, Results<Ok<Guid>, NotFound>>
{
    private readonly IUserRepository _repos;
    private readonly IHostEnvironment _env;

    public DeleteUser(IUserRepository repos, IHostEnvironment env)
    {
        _repos = repos;
        _env = env;
    }

    public override void Configure()
    {
        Delete(ApiRoutes.DeleteUser);
        //if (_env.IsDevelopment())
            AllowAnonymous();
        //else
            //Policies("AdminPolicy"); // prod/release
    }

    public override async Task<Results<Ok<Guid>, NotFound>> ExecuteAsync(DeleteUserRequest req, CancellationToken ct)
    {
        Log.Debug("-->request to delete user with Id {@UserId}", req.UserId);
        await _repos.DeleteUserByIdAsync(req.UserId);

        /// TODO: consider checking if user existed before deletion
        //if(id == Guid.Empty)
        //return TypedResults.NotFound();
        
        Log.Debug("<--respose user with Id {@UserId} successfully deleted", req.UserId);
        return TypedResults.Ok(req.UserId);
    }
}
