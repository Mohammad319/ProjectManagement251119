using TaskResourceBlueprints.Entities.Questions.Assignments;

namespace TaskResourceBlueprints.Entities.Questions.Groups
{
    public class QuestionOptionDefinition
    {
        /// <summary>
        /// Unique identifier for this option.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Display text shown to the user for this option.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// The question group this option belongs to.
        /// </summary>
        public int QuestionGroupId { get; set; }

        /// <summary>
        /// Reference to the parent question group containing this option.
        /// </summary>
        public QuestionGroupDefinition QuestionGroup { get; set; } = null!;

        /// <summary>
        /// List of UI section keys that should be revealed when this option is selected.
        /// </summary>
        public List<string> RevealedSectionKeys { get; set; } = [];

        /// <summary>
        /// Resource assignments that become active when this option is selected.
        /// </summary>
        public List<OptionResourceAssignment> OptionResourceAssignments { get; set; } = [];
    }

}
