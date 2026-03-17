using ShelfsService.src.ShelfsService.Common.DTOs.Items;
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
            AttributeColorMain = item.AttributeColorMain,
            AttributeColorSecond = item.AttributeColorSecond,
            AttributeType = item.AttributeType,
            AttributeSeason = item.AttributeSeason,
            AttributePattern = item.AttributePattern,
            AttributeMatterial = item.AttributeMatterial,
            isFavorite = item.isFavorite,
            DateCreated = item.DateCreated,
            UpdatedAt = item.UpdatedAt,

        };
    }
}