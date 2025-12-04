using ProjectImportHub.Entities.Questions.Assignments;
using ProjectImportHub.Entities.Questions.Conditions;
using ProjectImportHub.Entities.Questions.Groups;
using ProjectManagement.Shared.Helper.ProjectAppStorage;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectImportHub.Entities
{
    public class TaskAppBase1
    {
        public int Id { get; set; }
        public string? DisplayName { get; set; }
        public int SortOrder { get; set; }
        public bool IsVisible { get; set; } = true;
    }
    public class ActionEntity : TaskAppBase1 { }
    public class LocationEntity : TaskAppBase1 { }
    public class ActionTypeEntity : TaskAppBase1 { }
    public class FallEntity : TaskAppBase1 { }
    public enum TaskStatusEnum
    {
        ToPlan = 1,
        ToDo = 1,
        UnderWorking = 2,
        Ready = 10,
    }   
    public class ProjectTaskEntity : TaskAppBase1
    {
        public TaskStatusEnum Status { get; set; }
        public string? Responsible { get; set; }
        public string? AdminNote { get; set; }

        public string? Note { get; set; }
        public List<string> HeaderNotes { get; set; } = [];
        public double? Quantity { get; set; }
        public string? UnitCode { get; set; }
        public double ChangeFactor1 { get; set; } = 1;
        public double ChangeFactor2 { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public bool Uncontrollable { get; set; }

        public string? Code { get; set; }
        public List<double> WorkloadThresholds { get; set; } = [0, 0, 0];

        public int? ActionId { get; set; }
        public int? LocationId { get; set; }
        public int? ActionTypeId { get; set; }
        public int? FallId { get; set; }
        public int? UnitGroupId { get; set; }
        public List<string>? UpperNote { get; set; } = [];

        public List<int> VisibleFolderIds { get; set; } = [];

        public int? CapacityResourceId { get; set; }
        [NotMapped] public Dictionary<ParamName, double> ParameterValues { get; } = [];
        [NotMapped] public string? NewUnitCode { get; set; }
        [NotMapped] public bool ShowAllFolders { get; set; }

        public ActionEntity? Action { get; set; }
        public LocationEntity? Location { get; set; }
        public ActionTypeEntity? ActionType { get; set; }
        public FallEntity? Fall { get; set; }
        public UnitGroupEntity? UnitGroup { get; set; }
        public List<TaskResourceAssignmentEntity> TaskResourceAssignments { get; set; } = [];
        public List<OptionGroupEntity> OptionGroups { get; set; } = [];
        public List<ResourceOptionGroupEntity> ResourceOptionGroups { get; set; } = [];
        public List<NumericInputEntity> NumericInputs { get; set; } = [];
        public List<TaskConditionEntity> Conditions { get; set; } = [];
    }
}
