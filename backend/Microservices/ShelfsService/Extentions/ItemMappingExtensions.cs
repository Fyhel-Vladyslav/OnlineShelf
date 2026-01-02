using ShelfsService.src.ShelfsService.Common.DTOs;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;

namespace ShelfsService.Extentions;
public static class ItemMappingExtensions
{
    public static ItemDto ToDto(this Item item)
    {
        // Null check for safety  
        if (item == null)
        {
            return new ItemDto();
        }

        return new ItemDto
        {
            Id = item.Id,
            Name = item.Name,
            ShelfId = item.ShelfId,
            BigImage = item.BigImage,
            DateCreated = item.DateCreated,
            UpdatedAt = item.UpdatedAt,
            Tags = item.Tags

        };
    }
}