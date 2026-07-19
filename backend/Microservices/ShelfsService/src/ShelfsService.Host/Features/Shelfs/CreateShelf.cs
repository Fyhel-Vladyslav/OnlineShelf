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
            //AllowAnonymous();
        }

        public override async Task HandleAsync(CreateShelfCommand request, CancellationToken ct)
        {
            // 1. Отримуємо ID (Крок 1.а виконано ідеально). 
            // Ми впевнені, що юзер є, бо FastEndpoints вже перевірив токен перед входом у цей метод.
            var userId = User.GetUserId();
    
            Logger.LogDebug("Creating shelf {ShelfName} for user {UserId}", request.newShelf.Name, userId);

            // 2. Валідація бізнес-логіки (Унікальність імені)
            if (!await repos.CheckShelfNameUniqueAsync(request.newShelf.Name, userId, ct))
            {
                Logger.LogWarning("Shelf creation rejected because shelf {ShelfName} already exists for user {UserId}", request.newShelf.Name, userId);
        
                // Використання TypedResults — це чудовий сучасний підхід .NET
                await Send.ResultAsync(TypedResults.Conflict("Shelf with this name already exists"));
                return;
            }

            // 3. Створення та мапінг
            var shelf = FromDTO(request.newShelf, userId);
            var newShelf = await repos.CreateShelfAsync(shelf, ct);
    
            Logger.LogInformation("Shelf {ShelfId} created for user {UserId}", newShelf.Id, userId);

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
