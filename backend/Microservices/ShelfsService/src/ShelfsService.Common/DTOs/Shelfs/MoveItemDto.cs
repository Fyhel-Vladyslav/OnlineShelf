namespace ShelfsService.src.ShelfsService.Common.DTOs.Shelfs;
public class MoveItemDto
{
    public Guid ItemId { get; set; }
    public Guid NewShelfId { get; set; }
}
