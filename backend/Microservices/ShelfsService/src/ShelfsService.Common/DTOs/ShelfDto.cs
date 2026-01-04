using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;

namespace ShelfsService.src.ShelfsService.Common.DTOs;
public class ShelfDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Guid UserId { get; init; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<ItemPreviewDto> Items { get; set; } = new();
}

