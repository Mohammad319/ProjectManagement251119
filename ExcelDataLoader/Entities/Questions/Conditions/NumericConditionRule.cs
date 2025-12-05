using ProjectImportHub.Entities.Questions.Groups;

namespace ProjectImportHub.Entities.Questions.Conditions
{
    public class NumericConditionRule
    {
        public int Id { get; set; }
        public int QuestionConditionId { get; set; }
        public ConditionDefinition QuestionCondition { get; set; } = null!;

        public int NumericInputId { get; set; }
        public NumericQuestionDefinition NumericInput { get; set; } = null!;

        public double? MaxAllowedValue { get; set; }
        public double? MinAllowedValue { get; set; }
        public double? DefaultValue { get; set; }

        public int SetKey { get; set; } = 1;
    }
}
