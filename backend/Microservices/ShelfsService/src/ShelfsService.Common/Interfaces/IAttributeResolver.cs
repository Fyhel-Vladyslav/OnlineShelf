using ShelfsService.src.ShelfsService.Common.DTOs.Items;
using ShelfsService.src.ShelfsService.Host.Features.Attributes;

namespace ShelfsService.src.ShelfsService.Common.Interfaces;
public interface IAttributeResolver
{
    string GetValue(int attributeTypeId, int attributeKey);
    IEnumerable<AttributeTypeDto> GetAllData();
}
