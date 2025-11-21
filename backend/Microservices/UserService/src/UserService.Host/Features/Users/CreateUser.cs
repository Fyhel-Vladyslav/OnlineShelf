using FastEndpoints;
using MediatR;
using ErrorOr;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using UserService.src.UserService.Repository.EfCore.Entities;
using UserService.src.UserService.Common.DTOs;
using UserService.src.UserService.Common.Interfaces;

namespace UserService.src.UserService.Host.Features.Users;
public sealed record CreateUserCommand(CreateUserDto newUser) : IRequest<ErrorOr<User>>;
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
    IUserRepository repos
    ) : Endpoint<CreateUserCommand, User>
{
    public override void Configure()
    {
        Post("/api/add-user");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateUserCommand request, CancellationToken ct)
    {
        var newUser = FromDTO(request.newUser);

        if (! await repos.CheckUserLoginAndEmailUniqueAsync(request.newUser.Login, request.newUser.Email, ct))
        {Logger.LogError("User with login [{Login}] or email [{Email}] already exists", request.newUser.Login, request.newUser.Email);
            await Send.ResultAsync(TypedResults.Conflict());
            return;
        }


        await repos.CreateUserAsync(FromDTO(request.newUser), ct);

        await Send.OkAsync(newUser, cancellation: ct);
    }
    private User FromDTO(CreateUserDto dto)
    {


        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Login = String.IsNullOrEmpty(dto.Login)? dto.Email : dto.Login,
            Email = dto.Email,
            PasswordHash = "",
            DateCreated = DateTime.UtcNow

        };

        foreach (var role in dto.Roles)
        {
            newUser.Roles.Add(new UserRoleLink
            {
                Role = role,
                UserId = newUser.Id
            });
        }

        /// create dto with password field
        newUser.PasswordHash = passwordHasher.HashPassword(newUser, dto.Password);

        return newUser;
    }
}
