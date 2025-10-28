using Microsoft.AspNetCore.Http.HttpResults;
using FastEndpoints;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.src.Data;
using UserService.src.Common.DTOs;


namespace UserService.src.Features.Users;
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
    private readonly DataContext _dbContext;
    public GetUserById(DataContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Post("/api/users");
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
            Login = user.Login,
            Email = user.Email
        };

        // 3. Return the TypedResult
        return TypedResults.Ok(userDto);
    }
}
