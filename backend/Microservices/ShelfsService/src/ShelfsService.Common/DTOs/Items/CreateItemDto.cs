using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;

namespace ShelfsService.src.ShelfsService.Common.DTOs.Items;
public class CreateItemDto
{
    public string Name { get; set; }
    public Guid ShelfId { get; init; }
    public Guid UserId { get; init; }
    public string? BigImage { get; set; }
    public List<string> Tags { get; set; }
}
