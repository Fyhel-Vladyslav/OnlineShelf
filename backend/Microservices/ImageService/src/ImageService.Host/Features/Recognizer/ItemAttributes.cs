namespace ImageService.src.ImageService.Host.Features.Recognizer
{
    public class ItemAttributes
    {
        public ItemAttributes() { }
        public ItemAttributes(string attributeColorMain, int attributeType) {
            AttributeColorMain = attributeColorMain;
            AttributeType = attributeType;
        }
        public string AttributeColorMain { get; set; }
        public int AttributeType { get; set; }
    }
}
