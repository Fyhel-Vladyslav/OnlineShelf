namespace ShelfsService.src.ShelfsService.Repository.EfCore.Entities
{
    public class AttributeName
    {
        public int Id { get; set; }
        public int AttributeKey { get; set; }
        public string Name { get; set; }
        public ICollection<AttributesValue> Values { get; set; } = new List<AttributesValue>();
    }
}
