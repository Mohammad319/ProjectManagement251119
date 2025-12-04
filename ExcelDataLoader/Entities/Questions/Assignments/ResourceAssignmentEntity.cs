using ProjectImportHub.Entities.Assignments;
using ProjectImportHub.Entities.Questions.Conditions;
using ProjectManagement.Shared.Base.AppTenant;

namespace ProjectImportHub.Entities.Questions.Assignments
{
    public class ResourceAssignmentEntity
    {
        public int Id { get; set; }

        public int ResourceId { get; set; }
        public ResourceEntity Resource { get; set; } = null!;

        public int QuestionConditionId { get; set; }
        public TaskConditionEntity QuestionCondition { get; set; } = null!;
        public int? MenuId { get; set; }

        public double ChangeFactor1 { get; set; } = 1;
        public double ChangeFactor2 { get; set; } = 1;
        public double CapWaste { get; set; } = 1;
        public double? BaseCost { get; set; } = 0;
        public bool Uncontrollable { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public List<RoleDTO>? CapRole { get; set; } = [];
        public List<string> Formulas { get; set; } = [];

        public List<NumericResourceAssignmentEntity> NumericResourceFormulas { get; set; } = [];
        public List<OptionResourceAssignmentEntity> OptionResourceFormulas { get; set; } = [];

    }
}
