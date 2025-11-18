using ErrorOr;
using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Data;
using UserService.Extentions;
using UserService.src.UserService.Common;
using UserService.src.UserService.Common.DTOs;
using UserService.src.UserService.Common.Interfaces;
using UserService.src.UserService.Repository.EfCore.Entities;

namespace UserService.src.UserService.Host.Features.Users;

public sealed record AddRoleToUserCommand : IRequest<ErrorOr<User>>
{
    public Guid UserId { get; init; }
    public string Role { get; init; }
};
public class AddRoleToUserCommandHandler(
  IUserRepository repos
  ) : Endpoint<AddRoleToUserCommand, UserDto>
{
    public override void Configure()
    {
        Post("/api/add-role");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AddRoleToUserCommand request, CancellationToken ct)
    {

        UserRole newRole;

        bool isNewRoleValid = Enum.TryParse<UserRole>(request.Role, true, out newRole);

        if (!isNewRoleValid)
        {
            await Send.NotFoundAsync(cancellation: ct);  
            return;
        }

        var user = await repos.AddRoleToUser(request.UserId, newRole, ct);

        await Send.OkAsync(user.ToDto(), cancellation: ct);
    }
}

