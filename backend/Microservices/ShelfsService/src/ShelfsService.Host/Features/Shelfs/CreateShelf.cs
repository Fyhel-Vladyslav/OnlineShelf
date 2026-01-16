using FastEndpoints;
using MediatR;
using ErrorOr;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using System;
using ShelfsService.src.ShelfsService.Common;
using ShelfsService.src.ShelfsService.Common.Interfaces;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;
using ShelfsService.Extentions;
using ShelfsService.src.ShelfsService.Common.DTOs.Shelfs;


namespace ShelfsService.src.UserService.Host.Features.Shelfs;
public sealed record CreateShelfCommand(CreateShelfDto newShelf) : IRequest<ErrorOr<ShelfDto>>;
    internal sealed class CreateShelfCommandValidator : AbstractValidator<CreateShelfCommand>
    {

    public CreateShelfCommandValidator()
    {

        RuleFor(v => v.newShelf.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.")
            ;
    }

}
    public class CreateShelfCommandHandler(
        IShelfsRepository repos
        ) : Endpoint<CreateShelfCommand, ShelfDto>
    {
        public override void Configure()
        {
            Post(ApiRoutes.AddShelf);
            AllowAnonymous();
        }

    public override async Task HandleAsync(CreateShelfCommand request, CancellationToken ct)
    {

        var userId = User.GetUserId();
        var isAuthenticated = User.Identity?.IsAuthenticated;

        if (isAuthenticated != true)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var shelf = FromDTO(request.newShelf, userId);


        if (!await repos.CheckShelfNameUniqueAsync(request.newShelf.Name, userId, ct))
        {
            Logger.LogError("Shelf with name [{Name}] already exists", request.newShelf.Name);
            await Send.ResultAsync(TypedResults.Conflict("Shelf with this name already exists"));
            return;
        }


        var newShelf = await repos.CreateShelfAsync(shelf, ct);

        await Send.OkAsync(newShelf.ToDto(), cancellation: ct);
    }

    private Shelf FromDTO(CreateShelfDto dto, Guid userId)
    {

        var reqTime = DateTime.UtcNow;
        var newShelf = new Shelf
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name,
            Items = new List<Item>(),
            DateCreated = reqTime,
            UpdatedAt = reqTime
        };

        return newShelf;
    }

}
