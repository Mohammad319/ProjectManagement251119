using ProjectImportHub.Entities.Questions.Groups;

namespace ProjectImportHub.Entities.Questions.Assignments
{
    public class NumericResourceAssignment
    {
        public int Id { get; set; }

        public int NumericId { get; set; }
        public NumericQuestionDefinition Numeric { get; set; } = null!;

        public int ResourceAssignmentId { get; set; }
        public ConditionResourceAssignment ResourceAssignment { get; set; } = null!;
        public double? MinInputValue { get; set; }
        public double? MaxInputValue { get; set; }
        public List<string> Formulas { get; set; } = [];
    }
}
