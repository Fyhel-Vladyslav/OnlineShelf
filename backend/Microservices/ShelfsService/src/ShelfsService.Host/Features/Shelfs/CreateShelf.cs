using FastEndpoints;
using MediatR;
using ErrorOr;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
//using UserService.src.UserService.Repository.EfCore.Entities;
//using UserService.src.UserService.Common.DTOs;
//using UserService.src.UserService.Common.Interfaces;
//using UserService.src.UserService.Common;
//using UserService.Extentions;
using System;
using ShelfsService.src.ShelfsService.Common.DTOs;
using ShelfsService.src.ShelfsService.Common;


namespace ShelfsService.src.UserService.Host.Features.Shelfs;
public sealed record CreateShelfCommand(CreateShelfDto newShelf) : IRequest<ErrorOr<ShelfDto>>;
    internal sealed class CreateShelfCommandValidator : AbstractValidator<CreateShelfCommand>
    {

        //public CreateShelfCommandValidator()
        //{

        //    RuleFor(v => v.newUser.Email)
        //        .NotEmpty().WithMessage("Email is required.")
        //        .MaximumLength(200).WithMessage("Email must not exceed 200 characters.")
        //        ;
        //    RuleFor(v => v.newUser.Login)
        //        .NotEmpty().WithMessage("Login is required.")
        //        .MaximumLength(200).WithMessage("Login must not exceed 200 characters.")
        //        ;
        //}

    }
    public class CreateShelfCommandHandler(
       // IShelfRepository repos
        ) : Endpoint<CreateShelfCommand, ShelfDto>
    {
        public override void Configure()
        {
            Post(ApiRoutes.AddShelf);
            AllowAnonymous();
        }

        public override async Task HandleAsync(CreateShelfCommand request, CancellationToken ct)
        {
            //var user = FromDTO(request.newUser);

            //if (!await repos.CheckUserLoginAndEmailUniqueAsync(request.newUser.Login, request.newUser.Email, ct))
            //{
            //    Logger.LogError("User with login [{Login}] or email [{Email}] already exists", request.newUser.Login, request.newUser.Email);
            //    await Send.ResultAsync(TypedResults.Conflict());
            //    return;
            //}


            //var newUser = await repos.CreateShelfAsync(user, ct);

            //await roleResolver.ResolveRolesAsync(request.newUser.Roles, newUser.Id);

            //await Send.OkAsync(newUser.ToDto(), cancellation: ct);
        }
        //private Shelf FromDTO(CreateShelfDto dto)
        //{


        //    var newUser = new Shelf
        //    {
        //        Id = Guid.NewGuid(),
        //        Login = String.IsNullOrEmpty(dto.Login) ? dto.Email : dto.Login,
        //        Email = dto.Email,
        //        PasswordHash = "",
        //        DateCreated = DateTime.UtcNow

        //    };
        //    /// create dto with password field
        //    newUser.PasswordHash = passwordHasher.HashPassword(newUser, dto.Password);

        //    return newUser;
        //}
    }
