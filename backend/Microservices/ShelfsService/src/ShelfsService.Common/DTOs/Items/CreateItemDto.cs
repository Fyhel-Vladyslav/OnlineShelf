using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;

namespace ShelfsService.src.ShelfsService.Common.DTOs.Items;
public class CreateItemDto
{
    public string Name { get; set; }
    public Guid ShelfId { get; init; }
    public string? BigImage { get; set; }
    public string AttributeColorMain { get; set; }
    public string AttributeColorSecond { get; set; }
    public int AttributeType { get; set; } = 0;
    public int AttributeSeason { get; set; } = 0;
    public int AttributePattern { get; set; } = 0;
    public int AttributeMatterial { get; set; } = 0;
    public bool isFavorite { get; set; } = false;

}
