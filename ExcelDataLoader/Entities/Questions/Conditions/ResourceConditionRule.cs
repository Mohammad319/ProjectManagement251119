using ProjectImportHub.Entities.Questions.Groups;

namespace ProjectImportHub.Entities.Questions.Conditions
{
    public class ResourceConditionRule
    {
        /// <summary>
        /// Unique identifier for this resource-based condition rule.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The parent condition that this rule belongs to.
        /// </summary>
        public int ConditionId { get; set; }
        public ConditionDefinition Condition { get; set; } = null!;

        /// <summary>
        /// The resource selector group that this rule checks.
        /// </summary>
        public int SelectorId { get; set; }
        public ResourceSelectorDefinition Selector { get; set; } = null!;

        /// <summary>
        /// The specific resource option within the selector that must be matched.
        /// </summary>
        public int SelectorItemId { get; set; }
        public ResourceChoiceOptionDefinition SelectorItem { get; set; } = null!;

        /// <summary>
        /// Grouping key that allows multiple rules to belong to the same logical rule group.
        /// </summary>
        public int GroupKey { get; set; } = 1;
    }

}
