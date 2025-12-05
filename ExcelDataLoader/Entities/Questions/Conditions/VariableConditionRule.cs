using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectImportHub.Entities.Questions.Conditions
{
    [Table("ConditionVariableRequirement")] // اسم الجدول الحقيقي في SQL
    public class VariableConditionRule
    {
        public int Id { get; set; }
        public int QuestionConditionId { get; set; }
        public ConditionDefinition QuestionCondition { get; set; } = null!;
        public string? VariableName { get; set; }
        public double? MinAllowedValue { get; set; }
        public double? MaxAllowedValue { get; set; }
        public int SetKey { get; set; } = 1;
    }
}
