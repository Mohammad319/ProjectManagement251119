using TaskResourceBlueprints.Entities.Tasks;
using ProjectManagement.Shared.Base.AppTenant;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskResourceBlueprints.Entities.Questions.Assignments
{
    public class TaskResourceAssignment
    {
        public int Id { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal ChangeFactor1 { get; set; } = 1;
        public decimal ChangeFactor2 { get; set; } = 1;
        public decimal CapWaste { get; set; } = 1;
        public decimal? BaseCost { get; set; } = 0;
        public bool Uncontrollable { get; set; } = false;
        public List<RoleDTO> CapacityRoles { get; set; } = [];
        public List<string> Expressions { get; set; } = [];


        public int? MenuId { get; set; }
        [ForeignKey(nameof(Task))]
        public int TaskId { get; set; }
        public TaskDefinition? Task { get; set; }

        public int ResourceId { get; set; }
        public ResourceDefinition? Resource { get; set; }

    }
}