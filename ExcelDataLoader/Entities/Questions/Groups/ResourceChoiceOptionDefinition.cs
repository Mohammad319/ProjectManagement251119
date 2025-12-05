namespace ProjectImportHub.Entities.Questions.Groups
{
    public class ResourceChoiceOptionDefinition
    {
        public int Id { get; set; }

        public int ResourceChoiceGroupId { get; set; }
        public ResourceSelectorDefinition ResourceOptionGroup { get; set; } = null!;

        public int ResourceId { get; set; }
        public ResourceDefinition Resource { get; set; } = null!;
    }
}
