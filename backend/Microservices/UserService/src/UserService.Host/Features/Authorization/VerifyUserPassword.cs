
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserService.src.UserService.Common;
using UserService.src.UserService.Common.Interfaces;
using UserService.src.UserService.Repository.EfCore;
using UserService.src.UserService.Repository.EfCore.Entities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace UserService.src.UserService.Host.Features.Authorization;

internal sealed record VerifyUserPasswordRequest
{
    public Guid UserId { get; init; }
    public string Password { get; init; } = null!;
}

internal sealed class VerifyUserPasswordEndpoint(
   IUserRepository repos
   )
   : Endpoint<VerifyUserPasswordRequest, Results<Ok, NotFound, BadRequest<string>>>
{
    public override void Configure()
    {
        AllowAnonymous();
        Post(ApiRoutes.VerifyPassword);
        DontThrowIfValidationFails();
    }
    public override async Task<object> HandleAsync(VerifyUserPasswordRequest request, CancellationToken ct)
    {
        if (ValidationFailed)
        {
            await Send.NoContentAsync(ct);
            return TypedResults.NotFound();
        }

        var user = await repos.Users.FirstOrDefaultAsync(p => p.Id == request.UserId);
        if (user is null)
        {
            Logger.LogInformation("User with Id: {UserId} not found", request.UserId);
            await Send.NotFoundAsync(ct);
            return TypedResults.NotFound();
        }

        var isPasswordСongruent = await repos.VerifyUserPasswordAsync(user, request.Password, ct);

        if (!isPasswordСongruent)
        {
            Logger.LogInformation("Password for User with Id: {UserId} is incorrect", request.UserId);
            await Send.ErrorsAsync(400, ct);
            return TypedResults.BadRequest("Password is incorrect");
        }

        await Send.OkAsync(TypedResults.Ok());
        return TypedResults.Ok();
    }
}

