using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectImportHub.Entities.Questions.Conditions
{
    public class VariableConditionRule
    {
        public int Id { get; set; }
        public int ConditionId { get; set; }
        public ConditionDefinition Condition { get; set; } = null!;
        public string? VariableName { get; set; }
        public double? MinAllowedValue { get; set; }
        public double? MaxAllowedValue { get; set; }
        public int GroupKey { get; set; } = 1;
    }
}
