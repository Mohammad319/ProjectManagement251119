using ProjectImportHub.Entities.Tasks;
using ProjectManagement.Shared.Base.ProjectAppStorage;

namespace ProjectImportHub.Entities.Questions.Groups
{
    public class QuestionGroupDefinition
    {
        public int Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;
        public int SortOrder { get; set; } = 0;
        public SelectionMode SelectionMode { get; set; } = SelectionMode.Multiple;
        public string? SectionKey { get; set; }
        public int TaskId { get; set; }
        public TaskDefinition Task { get; set; } = null!;
        public List<QuestionOptionDefinition> Options { get; set; } = [];
    }
}
