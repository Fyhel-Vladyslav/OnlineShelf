
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UserService.src.Common;
using UserService.src.Data;
using UserService.src.Models;

namespace UserService.src.Features.Users;

internal sealed record VerifyUserPasswordRequest
{
    public Guid UserId { get; init; }
    public string Password { get; init; } = null!;
}

//internal sealed class VerifyUserPasswordRequestValidator : Validator<VerifyUserPasswordRequest>
//{
//    public VerifyUserPasswordRequestValidator()
//    {
//        RuleFor(x => x.Password)
//            .();
//    }
//}

internal sealed class VerifyUserPasswordEndpoint(DataContext dbCon)
    : Endpoint<VerifyUserPasswordRequest, Results<Ok, NotFound, BadRequest<string>>>
{
    private readonly DataContext _dbCon = dbCon ?? throw new ArgumentNullException(nameof(dbCon));

    public override void Configure()
    {
        AllowAnonymous();
        Post($"{ApiRoutes.Users}/verify-user-password");
        DontThrowIfValidationFails();
    }
    public override async Task HandleAsync(VerifyUserPasswordRequest request, CancellationToken ct)
    {
        if (ValidationFailed)
        {
            //await SendBadRequestAsync(ct);
            return;
        }

        var user = await _dbCon.Users.FirstOrDefaultAsync(p => p.Id == request.UserId);
        if (user is null)
        {
            Logger.LogInformation("User with Id: {UserId} not found", request.UserId);
            return;
        }

        //var passwordValidator = Resolve<ICompositePasswordValidator<User>>();
        //var result = await passwordValidator.ValidateAsync(user, request.Password);
        //if (!result.Succeeded)
        //{
        //    //await SendResultAsync(result.ToRsCoreBadRequest());
        //    return;
        //}

        //return TypedResults.Ok();
    }
}

