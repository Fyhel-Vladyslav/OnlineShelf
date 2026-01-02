using System.ComponentModel.DataAnnotations;

namespace ShelfsService.src.ShelfsService.Repository.EfCore.Entities;
public class ItemTag
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    [Required]
    public int TagTypeId { get; set; }
    public TagType Type { get; set; }
    [MaxLength(100)]
    public string Source { get; set; }
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
}
