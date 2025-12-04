using ProjectImportHub.Entities.Questions.Groups;

namespace ProjectImportHub.Entities.Questions.Conditions
{
    public class ConditionNumericRequirementEntity
    {
        public int Id { get; set; }
        public int QuestionConditionId { get; set; }
        public TaskConditionEntity QuestionCondition { get; set; } = null!;

        public int NumericInputId { get; set; }
        public NumericInputEntity NumericInput { get; set; } = null!;

        public double? MaxAllowedValue { get; set; }
        public double? MinAllowedValue { get; set; }
        public double? DefaultValue { get; set; }

        public int SetKey { get; set; } = 1;
    }
}
