using ErrorOr;
using FastEndpoints;
using MediatR;
using UserService.src.UserService.Common.DTOs;
using UserService.src.UserService.Common;
using UserService.src.UserService.Repository.EfCore.Entities;
using UserService.src.UserService.Common.Interfaces;
using UserService.src.UserService.Host.Features.Users;
using UserService.Extentions;
using System.Linq;

namespace UserService.src.UserService.Host.Features.Roles;
sealed record GetRolesListResponse(List<RoleDto> roles);

sealed class GetRolesListEndpoint : EndpointWithoutRequest<GetRolesListResponse>
{
    private readonly IUserRepository _repository;

    public GetRolesListEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public override void Configure()
    {
        Get(ApiRoutes.GetRoles);
        AllowAnonymous();
        // Policies("AdminPolicy");
        Summary(s =>
        {
            s.Summary = "Get list of all roles";
            s.Description = "Returns all available user roles";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var roles = await _repository.GetUserRoles();

        var response = new GetRolesListResponse(
            roles.Select(r => new RoleDto(r.Id, r.Name, r.Description))
                 .ToList()
        );

        await Send.OkAsync(response, ct);
    }
}