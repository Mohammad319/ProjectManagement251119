using ProjectImportHub.Entities.Questions.Assignments;

namespace ProjectImportHub.Entities.Questions.Groups
{
    public class OptionItemEntity
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;

        public int OptionGroupId { get; set; }
        public OptionGroupEntity OptionGroup { get; set; } = null!;
        public List<string> RevealedSectionKeys { get; set; } = [];
        public List<OptionResourceAssignmentEntity> OptionResourceAssignments { get; set; } = [];
    }
}
