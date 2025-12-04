using ProjectImportHub.Entities.Questions.Assignments;

namespace ProjectImportHub.Entities.Questions.Groups
{
    public class NumericInputEntity
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int SortOrder { get; set; } = 0;

        public double? MinInputValue { get; set; }
        public double? MaxInputValue { get; set; }
        public string? SectionKey { get; set; }
        public int TaskId { get; set; }
        public ProjectTaskEntity Task { get; set; } = null!;
        public List<NumericResourceAssignmentEntity> ResourceAssignments { get; set; } = new();
    }
}
