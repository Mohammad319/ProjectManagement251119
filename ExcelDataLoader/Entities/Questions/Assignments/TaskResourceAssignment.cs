using ProjectImportHub.Entities.Tasks;
using ProjectManagement.Shared.Base.AppTenant;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectImportHub.Entities.Questions.Assignments
{
    public class TaskResourceAssignment
    {
        public int Id { get; set; }
        public bool IsActive { get; set; } = true;
        public double ChangeFactor1 { get; set; } = 1;
        public double ChangeFactor2 { get; set; } = 1;
        public double CapWaste { get; set; } = 1;
        public double? BaseCost { get; set; } = 0;
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