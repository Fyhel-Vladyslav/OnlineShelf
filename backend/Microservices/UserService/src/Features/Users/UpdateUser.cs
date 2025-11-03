using FastEndpoints;
using MediatR;
using ErrorOr;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UserService.src.Models;
using UserService.src.Data;
using UserService.src.Common;
using Microsoft.AspNetCore.Identity;

namespace UserService.src.Features.Users;
public sealed record UpdateUserCommand(UpdateUserDto newUser) : IRequest<ErrorOr<User>>;
internal sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    private readonly DataContext _context;

    public UpdateUserCommandValidator(DataContext context)
    {
        _context = context;

        RuleFor(v => v.newUser.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters.")
            .MustAsync(BeUniqueEmail).WithMessage("The specified Email already exists.")
            ;
        RuleFor(v => v.newUser.Login)
            .NotEmpty().WithMessage("Login is required.")
            .MaximumLength(200).WithMessage("Login must not exceed 200 characters.")
            .MustAsync(BeUniqueLogin).WithMessage("The specified Login already exists.")
            ;
    }

    private Task<bool> BeUniqueLogin(string login, CancellationToken cancellationToken) =>
        _context.Users
            .AllAsync(l => l.Login != login, cancellationToken);

    private Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken) =>
        _context.Users
            .AllAsync(l => l.Email != email, cancellationToken);

}
public class UpdateUserCommandHandler(
    IPasswordHasher<User> passwordHasher,
    DataContext context
    ) : Endpoint<UpdateUserCommand, User>
{

    public override void Configure()
    {
        Post("/api/update-user");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateUserCommand request, CancellationToken ct)
    {
        var user = context.Users.FirstOrDefault(u => u.Id == request.newUser.Id);

        if (user == null)
        {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        SetUserFromDTO(user, request.newUser);

        await context.SaveChangesAsync(ct);

        await Send.OkAsync(user, cancellation: ct);

    }
    private void SetUserFromDTO(User user, UpdateUserDto dto)
    {
        user.Login  = dto.Login;
        user.Email = dto.Email;
        user.Role = dto.Role;
        user.UpdatedAt = DateTime.UtcNow;
        user.State = dto.State;
        user.Avatar = dto.Avatar;
        user.EmailVerified = dto.EmailVerified;
        user.State = dto.State;

    }
}
