using TaskResourceBlueprints.Entities.Tasks;
using ProjectManagement.Shared.Base.ProjectAppStorage;

namespace TaskResourceBlueprints.Entities.Questions.Groups
{
    public class QuestionGroupDefinition
    {
        /// <summary>
        /// Unique identifier for the question group.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Display name of the group shown to the user.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Sort order used to define the display sequence within the task.
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Defines how many options can be selected (single or multiple).
        /// </summary>
        public SelectionMode SelectionMode { get; set; } = SelectionMode.Multiple;

        /// <summary>
        /// Optional section key used to categorize or filter this group in the UI.
        /// </summary>
        public string? SectionKey { get; set; }

        /// <summary>
        /// The task this question group belongs to.
        /// </summary>
        public int TaskId { get; set; }
        public TaskDefinition Task { get; set; } = null!;

        /// <summary>
        /// The list of selectable options within this group.
        /// </summary>
        public List<QuestionOptionDefinition> Options { get; set; } = [];
    }

}
