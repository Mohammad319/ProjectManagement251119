using ProjectImportHub.Entities.Questions.Assignments;
using ProjectImportHub.Entities.Tasks;

namespace ProjectImportHub.Entities.Questions.Groups
{
    public class NumericQuestionDefinition
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int SortOrder { get; set; } = 0;

        public double? MinInputValue { get; set; }
        public double? MaxInputValue { get; set; }
        public string? SectionKey { get; set; }
        public int TaskId { get; set; }
        public TaskDefinition Task { get; set; } = null!;
        public List<NumericResourceAssignment> ResourceAssignments { get; set; } = new();
    }
}
