using ErrorOr;
using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Linq;
using UserService.Extentions;
using UserService.src.UserService.Common;
using UserService.src.UserService.Common.DTOs;
using UserService.src.UserService.Common.Interfaces;
using UserService.src.UserService.Repository.EfCore.Entities;

namespace UserService.src.UserService.Host.Features.Roles;

public sealed record GetRolesListResponse(
    List<RoleDto> Roles
);
sealed class GetRolesEndpoint : EndpointWithoutRequest<GetRolesListResponse>
{
    private readonly IUserRepository _repository;

    public GetRolesEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public override void Configure()
    {
        Get(ApiRoutes.AddRoleToUser);
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