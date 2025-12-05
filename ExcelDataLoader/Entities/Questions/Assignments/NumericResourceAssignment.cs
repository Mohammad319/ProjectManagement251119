using ProjectImportHub.Entities.Questions.Groups;

namespace ProjectImportHub.Entities.Questions.Assignments
{
    public class NumericResourceAssignment
    {
        public int Id { get; set; }

        public int NumericId { get; set; }
        public NumericQuestionDefinition Numeric { get; set; } = null!;

        public int AssignmentId { get; set; }
        public ConditionResourceAssignment Assignment { get; set; } = null!;
        public double? MinInputValue { get; set; }
        public double? MaxInputValue { get; set; }
        public List<string> Expressions { get; set; } = [];
    }
}
