using FastEndpoints;
using ShelfsService.Extentions;
using ShelfsService.src.ShelfsService.Common;
using ShelfsService.src.ShelfsService.Common.DTOs;
using ShelfsService.src.ShelfsService.Common.Interfaces;

namespace ShelfsService.src.ShelfsService.Host.Features.Shelfs;
sealed record GetAllShelfsResponse(List<ShelfDto> shelfs);

class GetAllShelfs : EndpointWithoutRequest<GetAllShelfsResponse>
{
    private readonly IShelfsRepository _repos;

    public GetAllShelfs(IShelfsRepository repos)
    {
        _repos = repos;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Shelfs);
        AllowAnonymous();
        //Policies("AdminPolicy");
    }

    public override async Task<GetAllShelfsResponse> ExecuteAsync(CancellationToken ct)
    {
        var shelfs = await _repos.GetAllShelfsAsync();

        return new GetAllShelfsResponse(
            shelfs.Select(u => u.ToDto())
            .ToList()
            );
    }
}