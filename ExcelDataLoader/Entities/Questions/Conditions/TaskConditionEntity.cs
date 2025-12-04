using ProjectImportHub.Entities.Questions.Assignments;
using ProjectManagement.Shared.Base.ProjectAppStorage;

namespace ProjectImportHub.Entities.Questions.Conditions
{
    public class TaskConditionEntity
    {
        public int Id { get; set; }
        public ConditionLogic OptionToResourceLogic { get; set; } = ConditionLogic.And;
        public ConditionLogic OptionToNumericLogic { get; set; } = ConditionLogic.And;
        public ConditionLogic NumericToResourceLogic { get; set; } = ConditionLogic.And;

        public int TaskId { get; set; }
        public ProjectTaskEntity Task { get; set; } = null!;
        public int? DefaultOptionRequirementId { get; set; }
        public ConditionOptionRequirementEntity? DefaultOptionRequirement { get; set; }
        public int? DefaultResourceRequirementId { get; set; }
        public ConditionResourceRequirementEntity? DefaultResourceRequirement { get; set; }

        public List<ConditionOptionRequirementEntity> OptionRequirements { get; set; } = [];
        public List<ConditionResourceRequirementEntity> ResourceRequirements { get; set; } = [];
        public List<ConditionNumericRequirementEntity> NumericRequirements { get; set; } = [];
        public List<ConditionVariableRequirementEntity> VariableRequirements { get; set; } = [];
        public List<ResourceAssignmentEntity> ConditionResourceAssignments { get; set; } = [];
    }
}
