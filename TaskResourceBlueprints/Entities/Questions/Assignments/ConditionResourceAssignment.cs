using TaskResourceBlueprints.Entities.Questions.Conditions;
using ProjectManagement.Shared.Base.AppTenant;

namespace TaskResourceBlueprints.Entities.Questions.Assignments
{
    public class ConditionResourceAssignment
    {
        public int Id { get; set; }

        public int ResourceId { get; set; }
        public ResourceDefinition Resource { get; set; } = null!;

        public int ConditionId { get; set; }
        public ConditionDefinition Condition { get; set; } = null!;
        public int? MenuId { get; set; }

        public decimal ChangeFactor1 { get; set; } = 1;
        public decimal ChangeFactor2 { get; set; } = 1;
        public decimal CapWaste { get; set; } = 1;
        public decimal? BaseCost { get; set; } = 0;
        public bool Uncontrollable { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public List<RoleDTO> CapacityRoles { get; set; } = [];
        /// <summary>
        /// Expressions used to compute the final cost or quantity.
        /// </summary>
        public List<string> Expressions { get; set; } = [];

        public List<NumericResourceAssignment> NumericAssignments { get; set; } = [];
        public List<OptionResourceAssignment> OptionAssignments { get; set; } = [];

    }
}
