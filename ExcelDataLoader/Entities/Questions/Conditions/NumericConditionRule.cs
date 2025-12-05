using ProjectImportHub.Entities.Questions.Groups;

namespace ProjectImportHub.Entities.Questions.Conditions
{
    public class NumericConditionRule
    {
        public int Id { get; set; }
        public int ConditionId { get; set; }
        public ConditionDefinition Condition { get; set; } = null!;

        public int NumericQuestionId { get; set; }
        public NumericQuestionDefinition NumericQuestion { get; set; } = null!;

        public double? MaxAllowedValue { get; set; }
        public double? MinAllowedValue { get; set; }
        public double? DefaultValue { get; set; }

        public int GroupKey { get; set; } = 1;
    }
}
