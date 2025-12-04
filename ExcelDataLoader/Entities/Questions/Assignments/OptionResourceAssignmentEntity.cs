using ProjectImportHub.Entities.Questions.Groups;

namespace ProjectImportHub.Entities.Questions.Assignments
{
    public class OptionResourceAssignmentEntity
    {
        public int Id { get; set; }
        public int ChoiceOptionId { get; set; }
        public OptionItemEntity ChoiceOption { get; set; } = null!;
        public int ResourceAssignmentId { get; set; }
        public ResourceAssignmentEntity ResourceAssignment { get; set; } = null!;
        public List<string> Formulas { get; set; } = [];
    }
}
