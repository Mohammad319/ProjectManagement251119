using ProjectManagement.Shared.Base.ProjectAppStorage;

namespace ProjectImportHub.Entities.Questions.Groups
{
    public class OptionGroupEntity
    {
        public int Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;
        public int SortOrder { get; set; } = 0;
        public SelectionMode SelectionMode { get; set; } = SelectionMode.Multiple;
        public string? SectionKey { get; set; }
        public int TaskId { get; set; }
        public ProjectTaskEntity Task { get; set; } = null!;
        public List<OptionItemEntity> Options { get; set; } = [];
    }
}
