using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserService.src.Common;
using UserService.src.Data;
using UserService.src.Features.JwtToken;
using UserService.src.Models;
using static FastEndpoints.Ep;

namespace UserService.src.Features.Authorization;
internal sealed record SignInRequest(string Login, string Password);
public sealed record SignInResult(string Token);

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
    DataContext dbcon,
    IPasswordHasher<User> passwordHasher
    //IJwtTokenProvider jwtTokenProvider,
    //IUserClaimProvider userClaimProvider,
    //ILdapAuthenticationService ldapAuthenticationService
    ) : Endpoint<SignInRequest, SignInResult>
{    
    //private readonly IJwtTokenProvider _jwtTokenProvider = jwtTokenProvider ?? throw new ArgumentNullException(nameof(jwtTokenProvider));
    //private readonly IUserClaimProvider _userClaimProvider = userClaimProvider ?? throw new ArgumentNullException(nameof(userClaimProvider));
    //private readonly ILdapAuthenticationService _ldapAuthenticationService = ldapAuthenticationService ?? throw new ArgumentNullException(nameof(ldapAuthenticationService));
    
    public override void Configure()
    {

        Post($"{ApiRoutes.Users}/sign-in");
        AllowAnonymous();
        DontThrowIfValidationFails();
    }

    public override async Task HandleAsync(SignInRequest request, CancellationToken ct)
    {
        // Lookup by email
        var user = await dbcon.Users
            .FirstOrDefaultAsync(u => u.Login == request.Login, ct);

        if (user is null)
        {
            //await SendNotFoundAsync(ct);
            return;
        }

        // Password verification
        var verifyResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password
        );

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            ThrowError("Invalid credentials");
        }

        // Optional: if hash needs rehashing (iteration upgrades)
        if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            await dbcon.SaveChangesAsync(ct);
        }

        // Issue JWT
        var token = jwt.CreateToken(user.Id, user.Role.ToString(), TimeSpan.FromHours(1));


        await Send.OkAsync(new SignInResult(token), ct);
    }
}