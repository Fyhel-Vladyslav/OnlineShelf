using Microsoft.AspNetCore.Http.HttpResults;
using FastEndpoints;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.src.UserService.Repository.EfCore;
using UserService.src.UserService.Repository.EfCore.Entities;
using UserService.src.UserService.Common.Interfaces;
using UserService.src.UserService.Common.DTOs;
using UserService.Extentions;
using UserService.src.UserService.Common;


namespace UserService.src.UserService.Host.Features.Users;
internal sealed record GetUserByIdRequest
{
    public Guid UserId { get; init; }
}


//internal sealed class GetAccountsRequestValidator : Validator<GetAccounts.GetAccountsRequest>
//{
//    public GetAccountsRequestValidator()
//    {
//        RuleFor(x => x.TerminalId)
//            .NotNull();

//        RuleFor(x => x.TransactionUuid)
//            .NotNull();
//    }
//}

internal sealed class GetUserById : Endpoint<GetUserByIdRequest, Results<Ok<UserDto>, NotFound>>
{
    private readonly IUserRepository _repos;
    public GetUserById(IUserRepository repos)
    {
        _repos = repos;
    }

    public override void Configure()
    {
        Post(ApiRoutes.GetUserById);
        Policies("AdminPolicy");
        // AllowAnonymous();
    }

    public override async Task<Results<Ok<UserDto>, NotFound>> ExecuteAsync(GetUserByIdRequest req, CancellationToken ct)
    {
        var user = await _repos.GetUserByIdAsync(req.UserId, ct);

        if (user is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(user.ToDto());
    }
}
