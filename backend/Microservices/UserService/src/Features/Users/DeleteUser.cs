using Microsoft.AspNetCore.Http.HttpResults;
using FastEndpoints;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.src.Data;
using UserService.src.Common;

namespace UserService.src.Features.Users;
internal sealed record DeleteUserRequest
{
    public Guid UserId { get; init; }
};


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

internal sealed class DeleteUser : Endpoint<DeleteUserRequest, Results<Ok<Guid>, NotFound>>
{
    private readonly DataContext _dbContext;
    private readonly IHostEnvironment _env; // Add a field for IHostEnvironment

    public DeleteUser(DataContext dbContext, IHostEnvironment env) // Inject IHostEnvironment
    {
        _dbContext = dbContext;
        _env = env;
    }

    public override void Configure()
    {
        Delete($"{ApiRoutes.DeleteUser}");
        if (_env.IsDevelopment())
        {
            AllowAnonymous();
        }
        else
        {
            Policies("AdminPolicy"); // prod/release
        }
    }

    public override async Task<Results<Ok<Guid>, NotFound>> ExecuteAsync(DeleteUserRequest req, CancellationToken ct)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(p => p.Id == req.UserId, ct);

        if (user is null)
        {
            return TypedResults.NotFound();
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(req.UserId);
    }
}
