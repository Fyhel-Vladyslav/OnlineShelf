using System.ComponentModel.DataAnnotations;

namespace ShelfsService.src.ShelfsService.Repository.EfCore.Entities;
public class AttributesValue
{
    public int Id { get; set; }
    [Required]
    public int AttributeId { get; set; }
    [Required]
    public int AttributeKey { get; set; }

    [MaxLength(100)]
    public string AttributeValue { get; set; }

    public AttributeName Attribute { get; set; }
}
