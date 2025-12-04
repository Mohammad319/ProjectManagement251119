using ProjectImportHub.Entities.Questions.Groups;

namespace ProjectImportHub.Entities.Questions.Conditions
{
    public class ConditionOptionRequirementEntity
    {
        public int Id { get; set; }  
        public int QuestionConditionId { get; set; }
        public TaskConditionEntity QuestionCondition { get; set; } = null!;

        public int OptionGroupId { get; set; }
        public OptionGroupEntity OptionGroup { get; set; } = null!;

        public int OptionItemId { get; set; }
        public OptionItemEntity OptionItem { get; set; } = null!;

        public int SetKey { get; set; } = 1;
    }
}
