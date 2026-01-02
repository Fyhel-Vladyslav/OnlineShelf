using ShelfsService.src.ShelfsService.Common.DTOs;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;

namespace ShelfsService.Extentions;
public static class ShelfMappingExtensions
{
    public static ShelfDto ToDto(this Shelf shelf)
    {
        // Null check for safety  
        if (shelf == null)
        {
            return new ShelfDto();
        }

        return new ShelfDto
        {
            Id = shelf.Id,
            Name = shelf.Name,
            Items = shelf.Items
            .Select(i => new ItemPreviewDto
            {
                Id = i.Id,
                Name = i.Name,
                SmallImage = i.SmallImage
            })
            .ToList()
        };
    }
}