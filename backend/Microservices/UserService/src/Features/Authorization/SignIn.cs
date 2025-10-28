using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using UserService.src.Common;
using UserService.src.Models;

namespace UserService.src.Features.Authorization;
internal sealed record SignInRequest(string Login, string Password);


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
    //IJwtTokenProvider jwtTokenProvider,
    //IUserClaimProvider userClaimProvider,
    //ILdapAuthenticationService ldapAuthenticationService
    ) : Endpoint<SignInRequest, Results<Ok<SignInResult>, NotFound>>
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
        if (ValidationFailed)
        {
            // await SendUnauthorizedAsync(cancellation: ct);
            return;
        }

        var userManager = Resolve<UserManager<User>>();
        //  var user = await userManager.FindBySignInLoginAsync(request.Login);
        // if (user is null)
        {
            Logger.LogInformation("User with login: {Login} not found", request.Login);
            //await SendResultAsync(TypedResults.Ok(
            //    new
            //    {
            //        error = new SignInError()
            //        {
            //            Code = SignInErrorCode.InvalidLoginOrPassword,
            //            Message = "Invalid login or password"
            //        }
            //    }
            //));
            return;
        }

        //    var signInManager = Resolve<RsCoreSignInManager>();
        //    var passwordPolicyProvider = Resolve<IPasswordPolicyProvider>();
        //    var lockoutEnabled = passwordPolicyProvider.GetPolicy().AccountLockoutDurationEnabled;
        //    var signInResult = await signInManager.PasswordSignInAsync(user, request.Password, false, lockoutEnabled);

        //    switch (signInResult)
        //    {
        //        case RsCoreSignInResult { IsActiveDirectoryUser: true } when user.AdLogin is not null:
        //            {
        //                Logger.LogInformation("Validate active directory user: {Login}...", request.Login);
        //                var authenticated = await _ldapAuthenticationService.AuthenticateAsync(user.AdLogin, request.Password, ct);

        //                if (authenticated)
        //                {
        //                    Logger.LogInformation("User authenticated in active directory");

        //                    // Publish audit event
        //                    await _auditPublisher.PublishAsync(new AuditEvent()
        //                    {
        //                        Action = "User.Login",
        //                        Initiator = user.Login,
        //                        Resource = $"User:{user.Id}",
        //                        TenantId = user.TenantId,
        //                        Details = new
        //                        {
        //                            ActiveDirectoryLogin = user.AdLogin,
        //                            Message = "User authenticated in active directory"
        //                        }
        //                    }, ct);

        //                    await SendResultAsync(TypedResults.Ok(
        //                        new
        //                        {
        //                            access_token = await _jwtTokenProvider.RequestAccessToken(
        //                                await _userClaimProvider.GetUserClaims(user.Id, user.Login, user.TenantId)),
        //                            error = new SignInError()
        //                            {
        //                                Code = SignInErrorCode.Success,
        //                            }
        //                        }
        //                    ));
        //                    return;
        //                }

        //                Logger.LogInformation("LDAP Authentication failed");

        //                // Publish audit event
        //                await _auditPublisher.PublishAsync(new AuditEvent()
        //                {
        //                    Action = "User.Login",
        //                    Initiator = user.Login,
        //                    Resource = $"User:{user.Id}",
        //                    TenantId = user.TenantId,
        //                    Status = AuditEventStatus.Error,
        //                    Details = new
        //                    {
        //                        ActiveDirectoryLogin = user.AdLogin,
        //                        Message = "User authentication in active directory failed"
        //                    }
        //                }, ct);

        //                await SendResultAsync(TypedResults.Ok(
        //                    new
        //                    {
        //                        error = new SignInError()
        //                        {
        //                            Code = SignInErrorCode.InvalidLoginOrPassword,
        //                            Message = "Invalid login or password"
        //                        }
        //                    }
        //                ));
        //                return;
        //            }
        //        case { IsLockedOut: true }:

        //            // Publish audit event
        //            await _auditPublisher.PublishAsync(new AuditEvent()
        //            {
        //                Action = "User.Login",
        //                Initiator = user.Login,
        //                Resource = $"User:{user.Id}",
        //                TenantId = user.TenantId,
        //                Status = AuditEventStatus.Error,
        //                Details = new
        //                {
        //                    user.FailedAttempts,
        //                    Message = "The maximum number of login attempts has been reached. User locked out"
        //                }
        //            }, ct);

        //            await SendResultAsync(TypedResults.Ok(
        //                new
        //                {
        //                    error = new SignInError()
        //                    {
        //                        Code = SignInErrorCode.Blocked,
        //                        Message = "The maximum number of login attempts has been reached."
        //                    }
        //                }
        //            ));
        //            return;
        //        case RsCoreSignInResult { IsPasswordExpired: true }:

        //            // Publish audit event
        //            await _auditPublisher.PublishAsync(new AuditEvent()
        //            {
        //                Action = "User.Login",
        //                Initiator = user.Login,
        //                Resource = $"User:{user.Id}",
        //                TenantId = user.TenantId,
        //                Status = AuditEventStatus.Warning,
        //                Details = new
        //                {
        //                    user.PasswordUpdatedAt,
        //                    Message = "The password has expired, but within grace period for change"
        //                }
        //            }, ct);

        //            await SendResultAsync(TypedResults.Ok(
        //                new
        //                {
        //                    access_token = await _jwtTokenProvider.RequestAccessToken(
        //                        await _userClaimProvider.GetUserClaims(user.Id, user.Login, user.TenantId)),
        //                    error = new SignInError()
        //                    {
        //                        Code = SignInErrorCode.PasswordExpired,
        //                    },
        //                }));

        //            return;
        //        case RsCoreSignInResult { IsPasswordExpireSoon: true } result:

        //            // Publish audit event
        //            await _auditPublisher.PublishAsync(new AuditEvent()
        //            {
        //                Action = "User.Login",
        //                Initiator = user.Login,
        //                Resource = $"User:{user.Id}",
        //                TenantId = user.TenantId,
        //                Details = new
        //                {
        //                    user.PasswordUpdatedAt,
        //                    Message = result.PasswordExpireSoonMessage ?? "Your password will expire soon."
        //                }
        //            }, ct);

        //            await SendResultAsync(TypedResults.Ok(
        //                new
        //                {
        //                    access_token = await _jwtTokenProvider.RequestAccessToken(
        //                        await _userClaimProvider.GetUserClaims(user.Id, user.Login, user.TenantId)),
        //                    error = new SignInError()
        //                    {
        //                        Code = SignInErrorCode.PasswordExpireSoon,
        //                        Message = result.PasswordExpireSoonMessage ?? "Your password will expire soon."
        //                    },
        //                }));

        //            return;
        //    }

        //    if (signInResult.Succeeded)
        //    {
        //        // Publish audit event
        //        await _auditPublisher.PublishAsync(new AuditEvent()
        //        {
        //            Action = "User.Login",
        //            Initiator = user.Login,
        //            Resource = $"User:{user.Id}",
        //            TenantId = user.TenantId,
        //            Details = new
        //            {
        //                user.Email,
        //                Message = "User logged in"
        //            }
        //        }, ct);

        //        await SendResultAsync(TypedResults.Ok(
        //            new
        //            {
        //                access_token = await _jwtTokenProvider.RequestAccessToken(
        //                    await _userClaimProvider.GetUserClaims(user.Id, user.Login, user.TenantId)),
        //                error = new SignInError()
        //                {
        //                    Code = SignInErrorCode.Success,
        //                },
        //            }));

        //        return;
        //    }

        //    await _auditPublisher.PublishAsync(new AuditEvent()
        //    {
        //        Action = "User.Login",
        //        Initiator = user.Login,
        //        Resource = $"User:{user.Id}",
        //        TenantId = user.TenantId,
        //        Status = AuditEventStatus.Error,
        //        Details = new
        //        {
        //            user.FailedAttempts,
        //            Message = "Invalid login or password"
        //        }
        //    }, ct);

        //    await SendResultAsync(TypedResults.Ok(
        //        new
        //        {
        //            error = new SignInError()
        //            {
        //                Code = SignInErrorCode.InvalidLoginOrPassword,
        //                Message = "Invalid login or password"
        //            }
        //        }
        //    ));
        //}

        //  internal static class UserManagerExtensions
        //{
        //    /// <summary>  
        //    /// Finds a user by their login.  
        //    /// </summary>  
        //    /// <param name="userManager">The user manager instance.</param>  
        //    /// <param name="login">The login to search for.</param>  
        //    /// <returns>The user if found, otherwise null.</returns>  
        //    public static async Task<User?> FindBySignInLoginAsync(this UserManager<User> userManager, string login)
        //    {
        //        // Assuming login is unique, you can use FindByNameAsync or FindByEmailAsync based on your logic.  
        //        // Replace this logic with the appropriate implementation for your application.  
        //        return await userManager.FindByNameAsync(login) ?? await userManager.FindByEmailAsync(login);
        //    }
        //}

    }
}