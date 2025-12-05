using ProjectImportHub.Entities.Questions.Assignments;

namespace ProjectImportHub.Entities.Questions.Groups
{
    public class QuestionOptionDefinition
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;

        public int OptionGroupId { get; set; }
        public QuestionGroupDefinition OptionGroup { get; set; } = null!;
        public List<string> RevealedSectionKeys { get; set; } = [];
        public List<OptionResourceAssignment> OptionResourceAssignments { get; set; } = [];
    }
}
