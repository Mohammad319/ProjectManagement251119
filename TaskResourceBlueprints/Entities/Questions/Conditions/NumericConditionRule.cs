using TaskResourceBlueprints.Entities.Questions.Groups;

namespace TaskResourceBlueprints.Entities.Questions.Conditions
{
    public class NumericConditionRule
    {
        public int Id { get; set; }
        public int ConditionId { get; set; }
        public ConditionDefinition Condition { get; set; } = null!;

        public int NumericQuestionId { get; set; }
        public NumericQuestionDefinition NumericQuestion { get; set; } = null!;

        public decimal? MaxAllowedValue { get; set; }
        public decimal? MinAllowedValue { get; set; }
        public decimal? DefaultValue { get; set; }

        public int GroupKey { get; set; } = 1;
    }
}
