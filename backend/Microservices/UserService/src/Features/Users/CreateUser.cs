using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.src.Models;
using UserService.src.Data;
using UserService.src.Common.DTOs;
using UserService.src.Common;

namespace UserService.src.Features.Users;
public sealed record CreateUserCommand(CreateUserDto newUser) : IRequest<ErrorOr<User>>;
internal sealed class CreateTodoListCommandValidator : AbstractValidator<CreateUserCommand>
{
    private readonly DataContext _context;

    public CreateTodoListCommandValidator(DataContext context)
    {
        _context = context;

        RuleFor(v => v.newUser.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
            .MustAsync(BeUniqueEmail).WithMessage("The specified title already exists.");
    }

    private Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        return _context.Users
            .AllAsync(l => l.Email != email, cancellationToken);
    }
}
public class CreateUserCommandHandler : Endpoint<CreateUserCommand, User>
{
    private readonly DataContext _context;

    public CreateUserCommandHandler(DataContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("/api/add-user");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateUserCommand request, CancellationToken ct)
    {
        var newUser = FromDTO(request.newUser);

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync(ct);

        await Send.OkAsync(newUser, cancellation: ct);

    }
    private User FromDTO(CreateUserDto dto)
    {
        return new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = dto.PasswordHash,
            DateCreated = DateTime.UtcNow
        };
    }
}
