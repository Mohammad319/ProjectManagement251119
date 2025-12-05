namespace ProjectImportHub.Entities.Resources
{
    public class ResourceAttributeValue
    {
        public int Id { get; set; }
        public int ResourceId { get; set; }
        public ResourceDefinition? Resource { get; set; }
        public int AttributeId { get; set; }
        public ResourceAttribute? Attribute { get; set; }
        public string? TextValue { get; set; }
        public double? NumericValue { get; set; }
    }
}
