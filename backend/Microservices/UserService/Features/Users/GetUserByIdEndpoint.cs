using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using FastEndpoints;
using UserService.Features.Common;
using UserService.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.Features.Users;


namespace UserService.Features.Users;
internal sealed record GetUserByIdRequest(int UserId);


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

internal sealed class GetUserByIdEndpoint : Endpoint<GetUserByIdRequest, Results<Ok<UserDto>, NotFound>>
{
    private readonly DataContext _dbContext;
    public GetUserByIdEndpoint(DataContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Post($"{ApiRoutes.Users}/UserId");
        AllowAnonymous();
        // Policy(x => x.RequireUserServicePolicy(PolicyNames.View));
    }
    public override async Task<Results<Ok<UserDto>, NotFound>> ExecuteAsync(GetUserByIdRequest req, CancellationToken ct)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(p => p.Id == req.UserId, ct);

        if (user is null)
        {
            // 3. Return the TypedResult
            return TypedResults.NotFound();
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email
        };

        // 3. Return the TypedResult
        return TypedResults.Ok(userDto);
    }
}
