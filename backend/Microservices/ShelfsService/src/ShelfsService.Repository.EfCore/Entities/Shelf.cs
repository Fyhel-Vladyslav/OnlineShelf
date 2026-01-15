using System.ComponentModel.DataAnnotations;

namespace ShelfsService.src.ShelfsService.Repository.EfCore.Entities;
public class Shelf
{
    [Required]
    public required Guid Id { get; init; }
    [Required]
    public required Guid UserId { get; init; }
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<Item> Items { get; set; } = new();

    public string positionKey { get; set; } = string.Empty;

    //public IList<AttributeValue> AttributeValues { get; init; } = [];

}
