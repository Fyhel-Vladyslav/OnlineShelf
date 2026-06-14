using ErrorOr;
using FastEndpoints;
using FluentValidation;
using MediatR;
using ShelfsService.Extentions;
using ShelfsService.src.ShelfsService.Common;
using ShelfsService.src.ShelfsService.Common.DTOs.Shelfs;
using ShelfsService.src.ShelfsService.Common.Interfaces;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;

namespace ShelfsService.src.ShelfsService.Host.Features.Shelfs;
public sealed record UpdateShelfCommand(UpdateShelfDto shelfDto) : IRequest<ErrorOr<ShelfDto>>;
    internal sealed class UpdateShelfCommandValidator : AbstractValidator<UpdateShelfCommand>
    {
        public UpdateShelfCommandValidator()
        {
        RuleFor(v => v.shelfDto.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.")
            ;
    }

    }
    public class UpdateShelfCommandHandler(
        IShelfsRepository repos
        ) : Endpoint<UpdateShelfCommand, ShelfDto>
    {

        public override void Configure()
        {
            Put(ApiRoutes.UpdateShelf);
            AllowAnonymous();
        }

        public override async Task HandleAsync(UpdateShelfCommand request, CancellationToken ct)
        {
            if (request.shelfDto == null)
            {
                await Send.ErrorsAsync(400, cancellation: ct);
                return;
            }

        var shelf = await repos.GetShelfByIdAsync(request.shelfDto.Id);

            if (shelf == null)
            {
                await Send.NotFoundAsync(cancellation: ct);
                return;
            }

        if (!await repos.CheckShelfNameUniqueAsync(request.shelfDto.Name, shelf.UserId, ct))
        {
            await Send.ErrorsAsync(400, cancellation: ct);
            //TODO: log duplicate name attempt
            return;
        }

        SetShelfFromDTO(shelf, request.shelfDto);
            var resShelf = await repos.UpdateShelfAsync(shelf, ct);
        if (resShelf ==null)
        {
            await Send.ErrorsAsync(500, cancellation: ct);
            return;
        }

            await Send.OkAsync(shelf.ToDto(), cancellation: ct);

        }

        private void SetShelfFromDTO(Shelf Shelf, UpdateShelfDto dto)
        {
            Shelf.Name = dto.Name;
    }
}
