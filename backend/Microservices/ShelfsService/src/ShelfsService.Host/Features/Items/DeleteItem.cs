using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using ShelfsService.src.ShelfsService.Common;
using ShelfsService.src.ShelfsService.Common.Interfaces;

namespace ItemsService.src.ShelfsService.Host.Features.Items;
internal sealed record DeleteItemByIdRequest
{
    public Guid ItemId { get; init; }
};

internal sealed class DeleteUser : Endpoint<DeleteItemByIdRequest, Results<Ok<Guid>, NotFound>>
{
    private readonly IItemRepository _repos;
    private readonly IHostEnvironment _env;

    public DeleteUser(IItemRepository repos, IHostEnvironment env)
    {
        _repos = repos;
        _env = env;
    }

    public override void Configure()
    {
        Delete(ApiRoutes.DeleteItem);
        //if (_env.IsDevelopment())
        AllowAnonymous();
        //else
        //Policies("AdminPolicy"); // prod/release
    }

    public override async Task<Results<Ok<Guid>, NotFound>> ExecuteAsync(DeleteItemByIdRequest req, CancellationToken ct)
    {
        var id = await _repos.DeleteItemByIdAsync(req.ItemId, ct);
        if (id == Guid.Empty)
            return TypedResults.NotFound();
        return TypedResults.Ok(id);
    }
}
