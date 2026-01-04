namespace ShelfsService.src.ShelfsService.Common.DTOs.Items;
public class UpdateItemDto
{
    public Guid Id { get; init; }
    public string Name { get; set; }
    public Guid ShelfId { get; init; }
    public string? BigImage { get; set; }
    public string? SmallImage { get; set; }
}
