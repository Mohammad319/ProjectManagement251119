using ProjectImportHub.Entities.Questions.Groups;

namespace ProjectImportHub.Entities.Questions.Conditions
{
    public class ConditionResourceRequirementEntity
    {
        public int Id { get; set; }

        public int QuestionConditionId { get; set; }
        public TaskConditionEntity QuestionCondition { get; set; } = null!;
        public int ResourceOptionGroupId { get; set; }
        public ResourceOptionGroupEntity ResourceOptionGroup { get; set; } = null!;
        public int ResourceOptionItemId { get; set; }
        public ResourceOptionItemEntity ResourceOptionItem { get; set; } = null!;
        public int SetKey { get; set; } = 1;
    }
}
