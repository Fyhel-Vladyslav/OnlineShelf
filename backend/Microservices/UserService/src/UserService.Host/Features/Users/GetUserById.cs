using Microsoft.AspNetCore.Http.HttpResults;
using FastEndpoints;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.src.UserService.Repository.EfCore;
using UserService.src.UserService.Repository.EfCore.Entities;
using UserService.src.UserService.Common.Interfaces;


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

internal sealed class GetUserById : Endpoint<GetUserByIdRequest, Results<Ok<User>, NotFound>>
{
    private readonly IUserRepository _repos;
    public GetUserById(IUserRepository repos)
    {
        _repos = repos;
    }

    public override void Configure()
    {
        Post("/api/users");
        AllowAnonymous();
        // Policy(x => x.RequireUserServicePolicy(PolicyNames.View));
    }
    public override async Task<Results<Ok<User>, NotFound>> ExecuteAsync(GetUserByIdRequest req, CancellationToken ct)
    {
        var user = await _repos.GetUserByIdAsync(req.UserId, ct);

        if (user is null)
        {
            return TypedResults.NotFound();
        }  
        return TypedResults.Ok(user);
    }
}
