using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;
using System.ComponentModel.DataAnnotations;

namespace ShelfsService.src.ShelfsService.Common.DTOs.Items;
public class ItemDto
{
    public Guid Id { get; init; }
    public string Name { get; set; }
    public Guid ShelfId { get; init; }
    public string? BigImage { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ItemTag> Tags { get; set; } = new();
}