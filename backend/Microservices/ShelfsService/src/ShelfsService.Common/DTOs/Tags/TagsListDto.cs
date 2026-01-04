namespace ShelfsService.src.ShelfsService.Common.DTOs.Tags;
public class TagsListDto
{
    public Guid ItemId { get; set; }
    public IList<string> Tags{ get; set; }
}
