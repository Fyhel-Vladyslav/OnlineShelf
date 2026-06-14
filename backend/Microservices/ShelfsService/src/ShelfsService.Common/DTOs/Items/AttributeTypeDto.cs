using ShelfsService.src.ShelfsService.Host.Features.Attributes;

namespace ShelfsService.src.ShelfsService.Common.DTOs.Items;
    public class AttributeTypeDto
    {
        public int AttributeId { get; set; }
        public string TypeName { get; set; }
        public List<AttributeOption> Options { get; set; } = new();
    }
