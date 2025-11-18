using ErrorOr;
using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using UserService.src.UserService.Common.DTOs;
using UserService.src.UserService.Common.Interfaces;
using UserService.src.UserService.Repository.EfCore.Entities;

namespace UserService.src.UserService.Host.Features.Users;

public sealed record AddRoleToUserCommand : IRequest<ErrorOr<User>>;

public class AddRoleToUserCommandHandler(
   IUserRepository repos
   ) : Endpoint<AddRoleToUserCommand, User>
{
    public override void Configure()
    {
        Post("/api/add-role");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AddRoleToUserCommand request, CancellationToken ct)
    {
       // await Send.OkAsync(newUser, cancellation: ct);
    }
}

