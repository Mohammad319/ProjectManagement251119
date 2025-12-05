using ProjectImportHub.Entities.Questions.Groups;

namespace ProjectImportHub.Entities.Questions.Conditions
{
    public class OptionConditionRule
    {
        public int Id { get; set; }  
        public int ConditionId { get; set; }
        public ConditionDefinition Condition { get; set; } = null!;

        public int QuestionGroupId { get; set; }
        public QuestionGroupDefinition QuestionGroup { get; set; } = null!;

        public int OptionId { get; set; }
        public QuestionOptionDefinition Option { get; set; } = null!;

        public int GroupKey { get; set; } = 1;
    }
}
