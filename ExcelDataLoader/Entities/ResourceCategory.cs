namespace ProjectImportHub.Entities
{
    public class ResourceCategory
    {
        public int Id { get; set; }
        public required string DisplayName { get; set; }
        public string? Note { get; set; }
        public bool IsVisible { get; set; } = true;
        public int SortOrder { get; set; }

        public ResourceCategory? ParentFolder { get; set; }
        public int? ParentFolderId { get; set; }
        public List<ResourceDefinition> Resources { get; set; } = [];
    }
}
