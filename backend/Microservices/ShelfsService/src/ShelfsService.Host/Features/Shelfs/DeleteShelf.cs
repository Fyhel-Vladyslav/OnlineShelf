using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using ShelfsService.src.ShelfsService.Common.DTOs;
using ShelfsService.src.ShelfsService.Common.Interfaces;
using ShelfsService.src.ShelfsService.Common;

namespace ShelfsService.src.ShelfsService.Host.Features.Shelfs;
internal sealed record DeleteShelfByIdRequest
{
    public Guid ShelfId { get; init; }
};

internal sealed class DeleteUser : Endpoint<DeleteShelfByIdRequest, Results<Ok<Guid>, NotFound>>
{
    private readonly IShelfsRepository _repos;
    private readonly IHostEnvironment _env;

    public DeleteUser(IShelfsRepository repos, IHostEnvironment env)
    {
        _repos = repos;
        _env = env;
    }

    public override void Configure()
    {
        Delete(ApiRoutes.DeleteShelf);
        //if (_env.IsDevelopment())
        AllowAnonymous();
        //else
        //Policies("AdminPolicy"); // prod/release
    }

    public override async Task<Results<Ok<Guid>, NotFound>> ExecuteAsync(DeleteShelfByIdRequest req, CancellationToken ct)
    {
        var id = await _repos.DeleteShelfByIdAsync(req.ShelfId, ct);
        if(id == Guid.Empty)
            return TypedResults.NotFound();
        return TypedResults.Ok(id);
    }
}
