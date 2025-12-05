using ProjectImportHub.Entities.Questions.Groups;

namespace ProjectImportHub.Entities.Questions.Conditions
{
    public class ResourceConditionRule
    {
        public int Id { get; set; }

        public int QuestionConditionId { get; set; }
        public ConditionDefinition QuestionCondition { get; set; } = null!;
        public int ResourceOptionGroupId { get; set; }
        public ResourceSelectorDefinition ResourceOptionGroup { get; set; } = null!;
        public int ResourceOptionItemId { get; set; }
        public ResourceChoiceOptionDefinition ResourceOptionItem { get; set; } = null!;
        public int SetKey { get; set; } = 1;
    }
}
