using FastEndpoints;
using ShelfsService.Extentions;
using ShelfsService.src.ShelfsService.Common;
using ShelfsService.src.ShelfsService.Common.DTOs.Shelfs;
using ShelfsService.src.ShelfsService.Common.Interfaces;
using System.Security.Claims;

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
            //AllowAnonymous();
            // перевіряє валідність JWT токена і поверне 401 Unauthorized, якщо токена немає або він «тухлий».
        }

        public override async Task<GetAllShelfsResponse> ExecuteAsync(CancellationToken ct)
        {
            var userId = User.GetUserId();

            var shelfs = await _repos.GetAllUserShelfsAsync(userId);

            return new GetAllShelfsResponse(
                shelfs.Select(u => u.ToDto()).ToList()
            );
        }
    }