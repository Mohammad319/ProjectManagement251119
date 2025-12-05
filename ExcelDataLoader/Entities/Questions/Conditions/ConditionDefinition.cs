using ProjectImportHub.Entities.Questions.Assignments;
using ProjectImportHub.Entities.Tasks;
using ProjectManagement.Shared.Base.ProjectAppStorage;

namespace ProjectImportHub.Entities.Questions.Conditions
{
    /// <summary>
    /// Represents a logical condition attached to a task.
    /// A condition can evaluate option-based rules, resource-based rules,
    /// numeric input rules, and variable rules to determine which
    /// resource assignments should be activated.
    /// </summary>
    public class ConditionDefinition
    {
        public int Id { get; set; }

        /// <summary>
        /// Logic applied between option-based and resource-based rules.
        /// </summary>
        public ConditionLogic OptionResourceLogic { get; set; } = ConditionLogic.And;

        /// <summary>
        /// Logic applied between option-based and numeric-based rules.
        /// </summary>
        public ConditionLogic OptionNumericLogic { get; set; } = ConditionLogic.And;

        /// <summary>
        /// Logic applied between numeric-based and resource-based rules.
        /// </summary>
        public ConditionLogic NumericResourceLogic { get; set; } = ConditionLogic.And;

        /// <summary>
        /// The task this condition belongs to.
        /// </summary>
        public int TaskId { get; set; }
        public TaskDefinition Task { get; set; } = null!;

        /// <summary>
        /// Optional default option rule used when evaluating conditions.
        /// </summary>
        public int? DefaultOptionRuleId { get; set; }
        public OptionConditionRule? DefaultOptionRule { get; set; }

        /// <summary>
        /// Optional default resource rule used when evaluating conditions.
        /// </summary>
        public int? DefaultResourceRuleId { get; set; }
        public ResourceConditionRule? DefaultResourceRule { get; set; }

        /// <summary>
        /// All option-based rules that must be checked by the condition.
        /// </summary>
        public List<OptionConditionRule> OptionRules { get; set; } = [];

        /// <summary>
        /// All resource-based rules that must be checked by the condition.
        /// </summary>
        public List<ResourceConditionRule> ResourceRules { get; set; } = [];

        /// <summary>
        /// All numeric-based rules required by the condition.
        /// </summary>
        public List<NumericConditionRule> NumericRules { get; set; } = [];

        /// <summary>
        /// Additional variable-based rules associated with the condition.
        /// </summary>
        public List<VariableConditionRule> VariableRules { get; set; } = [];

        /// <summary>
        /// Resource assignments that will be activated when the condition is satisfied.
        /// </summary>
        public List<ConditionResourceAssignment> Assignments { get; set; } = [];
    }
}
