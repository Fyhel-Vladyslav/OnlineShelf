using System.ComponentModel.DataAnnotations;

namespace ShelfsService.src.ShelfsService.Repository.EfCore.Entities;
public class Item
{
    [Required]
    public required Guid Id { get; init; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } 
    public required Guid UserId { get; init; }

    [Required]
    public Guid ShelfId { get; set; }
    public string positionKey { get; set; } = string.Empty;

    public string? BigImage { get; set; }
    public string? SmallImage { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    
    
    public List<ItemTag> Tags { get; set; } = new();
    public Shelf Shelf { get; set; }


    //public IList<AttributeValue> AttributeValues { get; init; } = [];
}