using ShelfsService.src.ShelfsService.Common.DTOs.Items;

namespace ShelfsService.src.ShelfsService.Common.DTOs.Shelfs;

public class CreateShelfDto
{
    public string Name { get; set; }
    public Guid UserId { get; set; }
}

