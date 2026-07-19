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
using Serilog;
using UserService.src.UserService.Repository.EfCore;
using UserService.src.UserService.Repository.EfCore.Entities;
using static FastEndpoints.Ep;

namespace UserService.src.UserService.Host.Features.Authorization;
internal sealed record SignInRequest(string Login, string Password);
public sealed record SignInResult(string AccessToken, string RefreshAccessToken);

internal sealed class SignInRequestValidator : Validator<SignInRequest>
{

    public SignInRequestValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(128);
    }
}

internal sealed class UserSignInEndpoint(
    JwtTokenService jwt,
    IUserRepository repos,
    IPasswordHasher<User> passwordHasher
    ) : Endpoint<SignInRequest, SignInResult>
{
    //private readonly IJwtTokenProvider _jwtTokenProvider = jwtTokenProvider ?? throw new ArgumentNullException(nameof(jwtTokenProvider));
    //private readonly IUserClaimProvider _userClaimProvider = userClaimProvider ?? throw new ArgumentNullException(nameof(userClaimProvider));
    //private readonly ILdapAuthenticationService _ldapAuthenticationService = ldapAuthenticationService ?? throw new ArgumentNullException(nameof(ldapAuthenticationService));

    public override void Configure()
    {

        Post(ApiRoutes.SignIn);
        AllowAnonymous();
        DontThrowIfValidationFails();
    }

    public override async Task HandleAsync(SignInRequest request, CancellationToken ct)
    {
        Log.Information("Got sign in request");
        Log.Debug("-->request: {Request}", request);
        var user = await repos.GetUserByLoginAsync(request.Login, ct);

        if (user is null)
        {
            Log.Information("User {Login} not found", request.Login);
            await Send.UnauthorizedAsync(cancellation: ct);
            return;
        }

        var verifyResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password
        );

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            Log.Information("Login or password is invald");
            await Send.UnauthorizedAsync(cancellation: ct);
        }

        // TODO delete
        if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        }
        
        
        var accessTokenLifeTime = TimeSpan.FromHours(1); 
        var accessToken = jwt.CreateToken(user.Id, user.Roles, accessTokenLifeTime);
        
        Log.Information("Login was succesfull");
        Log.Debug("<--response: {Token}", accessToken);
        
        var refreshAccessToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        await repos.SaveRefreshTokenAsync(user.Id, refreshAccessToken, ct);
        
        await Send.OkAsync(new SignInResult(accessToken, refreshAccessToken), ct);
    }
}