using System.Collections.Generic;

namespace ProjectManagement.Shared.Base.ProjectAppStorage
{
    public class TaskAppBase
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public int SortOrder { get; set; }
        public bool IsVisible { get; set; } = true;
    }

    public class ProjectTaskBase : TaskAppBase
    {
        public string Note { get; set; }
        public List<string> HeaderNotes { get; set; } = [];
        public double? Quantity { get; set; }
        public string UnitCode { get; set; }
        public double ChangeFactor1 { get; set; } = 1;
        public double ChangeFactor2 { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public string Code { get; set; }
        public List<string> Formulas { get; set; } = [];
        public List<double> WorkloadThresholds { get; set; } = [0, 0, 0];

        public int? ActionId { get; set; }
        public int? LocationId { get; set; }
        public int? ActionTypeId { get; set; }
        public int? FallId { get; set; }
        public int? UnitGroupId { get; set; }

        public List<int> VisibleFolderIds { get; set; } = [];

        public int? CapacityResourceId { get; set; }
    }
}
