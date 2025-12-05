using ProjectImportHub.Entities.Questions.Groups;

namespace ProjectImportHub.Entities.Questions.Assignments
{
    public class OptionResourceAssignment
    {
        public int Id { get; set; }

        /// <summary>
        /// The option selected by the user which triggers this resource assignment.
        /// </summary>
        public int OptionId { get; set; }

        /// <summary>
        /// Reference to the option definition that activates this assignment.
        /// </summary>
        public QuestionOptionDefinition Option { get; set; } = null!;

        /// <summary>
        /// The parent assignment that will be modified when this option is selected.
        /// </summary>
        public int AssignmentId { get; set; }

        /// <summary>
        /// The resource assignment that receives changes when this option is active.
        /// </summary>
        public ConditionResourceAssignment Assignment { get; set; } = null!;

        /// <summary>
        /// A list of expressions (formulas) used to compute or adjust resource values.
        /// </summary>
        public List<string> Expressions { get; set; } = [];
    }

}
