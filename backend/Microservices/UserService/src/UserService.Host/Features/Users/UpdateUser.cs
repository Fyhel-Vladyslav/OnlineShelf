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
using UserService.src.UserService.Host.Features.Roles;
using UserService.Extentions;

namespace UserService.src.UserService.Host.Features.Users;
public sealed record UpdateUserCommand(UpdateUserDto newUser) : IRequest<ErrorOr<UserDto>>;
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
    IUserRepository repos,
    IRoleResolver roleResolver
    ) : Endpoint<UpdateUserCommand, UserDto>
{

    public override void Configure()
    {
        Post(ApiRoutes.UpdateUser);
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateUserCommand request, CancellationToken ct)
    {
        if (request.newUser == null)
        {
            await Send.ErrorsAsync(400, cancellation: ct);
            return;
        }
        var user = await repos.GetUserByIdAsync(request.newUser.Id);

        if (user == null)
        {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }
        // TODO: check unique login/email except current user
        //if (!await repos.CheckUserLoginAndEmailUniqueAsync(request.newUser.Login, request.newUser.Email, ct))
        //{
        //    Logger.LogError("User with login [{Login}] or email [{Email}] already exists", request.newUser.Login, request.newUser.Email);
        //    await Send.ResultAsync(TypedResults.Conflict("User with this login or email already exists"));
        //    return;
        //}

        SetUserFromDTO(user, request.newUser);
        await repos.UpdateUserAsync(user);

        await roleResolver.ResolveRolesAsync(request.newUser.Roles, user.Id);

        await Send.OkAsync(user.ToDto(), cancellation: ct);

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

    }
}
