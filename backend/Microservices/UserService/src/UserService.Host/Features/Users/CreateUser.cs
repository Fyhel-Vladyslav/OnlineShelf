using FastEndpoints;
using MediatR;
using ErrorOr;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using UserService.src.UserService.Repository.EfCore.Entities;
using UserService.src.UserService.Common.DTOs;
using UserService.src.UserService.Common.Interfaces;
using UserService.src.UserService.Common;
using UserService.Extentions;

namespace UserService.src.UserService.Host.Features.Users;
public sealed record CreateUserCommand(CreateUserDto newUser) : IRequest<ErrorOr<UserDto>>;
internal sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{

    public CreateUserCommandValidator()
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
public class CreateUserCommandHandler(
    IPasswordHasher<User> passwordHasher,
    IUserRepository repos,
    IRoleResolver roleResolver
    ) : Endpoint<CreateUserCommand, UserDto>
{
    public override void Configure()
    {
        Post(ApiRoutes.AddUser);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateUserCommand request, CancellationToken ct)
    {
        var user = FromDTO(request.newUser);

        if (!await repos.CheckUserLoginAndEmailUniqueAsync(request.newUser.Login, request.newUser.Email, ct))
        {
            Logger.LogError("User with login [{Login}] or email [{Email}] already exists", request.newUser.Login, request.newUser.Email);
            await Send.ResultAsync(TypedResults.Conflict());
            return;
        }


        var newUser = await repos.CreateUserAsync(user, ct);

        await roleResolver.ResolveRolesAsync(request.newUser.Roles, newUser.Id);

        await Send.OkAsync(newUser.ToDto(), cancellation: ct);
    }
    private User FromDTO(CreateUserDto dto)
    {


        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Login = String.IsNullOrEmpty(dto.Login) ? dto.Email : dto.Login,
            Email = dto.Email,
            PasswordHash = "",
            DateCreated = DateTime.UtcNow

        };
        /// create dto with password field
        newUser.PasswordHash = passwordHasher.HashPassword(newUser, dto.Password);

        return newUser;
    }
}
