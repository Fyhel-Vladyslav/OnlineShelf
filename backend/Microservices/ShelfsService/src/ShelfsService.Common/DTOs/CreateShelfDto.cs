namespace ShelfsService.src.ShelfsService.Common.DTOs;

public class CreateShelfDto
{
    public string Name { get; set; }    
    public Guid UserId { get; set; } 
    public List<ItemPreviewDto> Items { get; set; } = new();
}

