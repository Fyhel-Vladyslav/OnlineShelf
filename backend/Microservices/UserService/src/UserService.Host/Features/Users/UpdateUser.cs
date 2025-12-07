using FastEndpoints;
using MediatR;
using ErrorOr;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using UserService.src.UserService.Repository.EfCore;
using UserService.src.UserService.Repository.EfCore.Entities;
using UserService.src.UserService.Common.DTOs;
using UserService.src.UserService.Common.Interfaces;
using UserService.src.UserService.Common;

namespace UserService.src.UserService.Host.Features.Users;
public sealed record UpdateUserCommand(UpdateUserDto newUser) : IRequest<ErrorOr<User>>;
internal sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {

        RuleFor(v => v.newUser.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters.")
            ;
        RuleFor(v => v.newUser.Login)
            .NotEmpty().WithMessage("Login is required.")
            .MaximumLength(200).WithMessage("Login must not exceed 200 characters.")
            ;
    }

}
public class UpdateUserCommandHandler(
    IUserRepository repos

    ) : Endpoint<UpdateUserCommand, User>
{

    public override void Configure()
    {
        Post(ApiRoutes.UpdateUser);
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateUserCommand request, CancellationToken ct)
    {
        var user = await repos.GetUserByIdAsync(request.newUser.Id);

        if (user == null)
        {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        if (!await repos.CheckUserLoginAndEmailUniqueAsync(request.newUser.Login, request.newUser.Email, ct))
        {
            Logger.LogError("User with login [{Login}] or email [{Email}] already exists", request.newUser.Login, request.newUser.Email);
            await Send.ResultAsync(TypedResults.Conflict("User with this login or email already exists"));
            return;
        }

        SetUserFromDTO(user, request.newUser);


        await Send.OkAsync(user, cancellation: ct);

    }

    private void SetUserFromDTO(User user, UpdateUserDto dto)
    {
        user.Login = dto.Login;
        user.Email = dto.Email;
        user.UpdatedAt = DateTime.UtcNow;
        user.State = dto.State;
        user.Avatar = dto.Avatar;
        user.EmailVerified = dto.EmailVerified;
        user.State = dto.State;

        foreach (var role in dto.Roles)
        {
            var existingRoleLink = user.Roles.FirstOrDefault(r => r.Role == role);
            if (existingRoleLink != null)
                user.Roles.Add(new UserRoleLink
                {
                    Role = role,
                    UserId = user.Id
                });
        }
    }
}
