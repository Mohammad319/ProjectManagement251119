using ProjectImportHub.Entities.Tasks;

namespace ProjectImportHub.Entities.Questions.Groups
{
    public class ResourceSelectorDefinition
    {
        public int Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;
        public int SortOrder { get; set; } = 0;
        public string? SectionKey { get; set; }
        public int TaskId { get; set; }
        public TaskDefinition Task { get; set; } = null!;
        public List<ResourceChoiceOptionDefinition> Items { get; set; } = [];
    }
}
