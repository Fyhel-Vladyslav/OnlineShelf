using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UserService.src.UserService.Common;
using UserService.src.UserService.Common.Interfaces;
using UserService.src.UserService.Host.Features.JwtToken;
using UserService.src.UserService.Repository.EfCore;
using UserService.src.UserService.Repository.EfCore.Entities;
using static FastEndpoints.Ep;
using Serilog;

namespace UserService.src.UserService.Host.Features.Authorization;

internal sealed record RefreshTokenRequest
{
    public string? RefreshToken { get; init; } 
};

public sealed record RefreshTokenResult(string AccessToken, string RefreshAccessToken);

internal sealed class RefreshTokenRequestValidator : Validator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .MaximumLength(50);
    }
}

internal sealed class UserRefreshTokenEndpoint(
    JwtTokenService jwt,
    IUserRepository repos
) : Endpoint<RefreshTokenRequest, RefreshTokenResult>
{
    public override void Configure()
    {
        Post(ApiRoutes.RefreshToken);
        AllowAnonymous();
        DontThrowIfValidationFails();
    }

    public override async Task HandleAsync(RefreshTokenRequest request, CancellationToken ct)
    {

        var user = await repos.GetUserByRefreshToken(request.RefreshToken);

        if (user == null)
        {
            // користувач можливо був взламаний, бо рефреш токен просто так не зникає. Просимо залогінитися
            Log.Information("Refresh token not found");
            // TODO додати якусь іще логіку перевірки на те чи не взламали користувача
            await Send.UnauthorizedAsync();
        }

        if (user.RefreshTokenExpiry.HasValue && user.RefreshTokenExpiry.Value < DateTime.UtcNow)
        {
            // користувач давно не заходив, все ок. Просимо перелогінитися
            Log.Information("Refresh token has expired");
            await Send.UnauthorizedAsync();
        }
        
        var accessTokenLifeTime = TimeSpan.FromHours(1);
        var accessToken = jwt.CreateToken(user.Id, user.Roles, accessTokenLifeTime);
        var newRefreshAccessToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
         
        await repos.SaveRefreshTokenAsync(user.Id, newRefreshAccessToken, ct);

        await Send.OkAsync(new RefreshTokenResult(accessToken, newRefreshAccessToken), ct);
    }
}