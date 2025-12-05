using TaskResourceBlueprints.Entities.Questions.Groups;

namespace TaskResourceBlueprints.Entities.Questions.Conditions
{
    public class OptionConditionRule
    {
        public int Id { get; set; }

        /// <summary>Parent condition this rule belongs to.</summary>
        public int ConditionId { get; set; }
        public ConditionDefinition Condition { get; set; } = null!;

        /// <summary>The question group containing the target option.</summary>
        public int QuestionGroupId { get; set; }
        public QuestionGroupDefinition QuestionGroup { get; set; } = null!;

        /// <summary>The selected option used as part of the condition rule.</summary>
        public int OptionId { get; set; }
        public QuestionOptionDefinition Option { get; set; } = null!;

        /// <summary>
        /// Grouping key used to link related option rules inside a single condition.
        /// </summary>
        public int GroupKey { get; set; } = 1;
    }
}
