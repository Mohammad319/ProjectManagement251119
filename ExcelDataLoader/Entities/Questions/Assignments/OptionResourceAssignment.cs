using ProjectImportHub.Entities.Questions.Groups;

namespace ProjectImportHub.Entities.Questions.Assignments
{
    public class OptionResourceAssignment
    {
        public int Id { get; set; }
        public int ChoiceOptionId { get; set; }
        public QuestionOptionDefinition ChoiceOption { get; set; } = null!;
        public int ResourceAssignmentId { get; set; }
        public ConditionResourceAssignment ResourceAssignment { get; set; } = null!;
        public List<string> Formulas { get; set; } = [];
    }
}
