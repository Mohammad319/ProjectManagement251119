using ProjectImportHub.Entities.Questions.Assignments;
using ProjectImportHub.Entities.Tasks;

namespace ProjectImportHub.Entities.Questions.Groups
{
    public class NumericQuestionDefinition
    {
        /// <summary>
        /// Unique identifier for the numeric question.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Display name shown to the user for this numeric question.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Ordering index used to sort questions within the task.
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Minimum value that the user is allowed (or expected) to enter.
        /// </summary>
        public double? MinInputValue { get; set; }

        /// <summary>
        /// Maximum value that the user is allowed (or expected) to enter.
        /// </summary>
        public double? MaxInputValue { get; set; }

        /// <summary>
        /// Optional section key used to group or filter this question in the UI.
        /// </summary>
        public string? SectionKey { get; set; }

        /// <summary>
        /// The task this numeric question belongs to.
        /// </summary>
        public int TaskId { get; set; }
        public TaskDefinition Task { get; set; } = null!;

        /// <summary>
        /// Resource assignments that depend on the value of this numeric question.
        /// </summary>
        public List<NumericResourceAssignment> ResourceAssignments { get; set; } = new();
    }

}
