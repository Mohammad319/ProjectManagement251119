using ProjectManagement.Shared.Base.AppTenant;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectImportHub.Entities.Questions.Assignments
{
    public class TaskResourceAssignmentEntity
    {
        public int Id { get; set; }
        public bool IsActive { get; set; } = true;
        public double ChangeFactor1 { get; set; } = 1;
        public double ChangeFactor2 { get; set; } = 1;
        public double CapWaste { get; set; } = 1;
        public double? BaseCost { get; set; } = 0;
        public bool Uncontrollable { get; set; } = false;
        public List<RoleDTO>? CapRole { get; set; } = [];
        public List<string> Formulas { get; set; } = [];


        public int? MenuId { get; set; }
        [ForeignKey(nameof(Task))]
        public int TaskId { get; set; }
        public ProjectTaskEntity? Task { get; set; }

        public int ResourceId { get; set; }
        public ResourceEntity? Resource { get; set; }

    }
}