using System.Collections.Generic;

namespace ProjectManagement.Shared.Base.ProjectAppStorage
{
    public class TaskAppBase
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsVisible { get; set; } = true;
    }

    public class ProjectTaskBase : TaskAppBase
    {
        public string Note { get; set; } = string.Empty;
        public List<string> HeaderNotes { get; set; } = [];
        public decimal? Quantity { get; set; }
        public string UnitCode { get; set; } = string.Empty;
        public decimal ChangeFactor1 { get; set; } = 1m;
        public decimal ChangeFactor2 { get; set; } = 1m;
        public bool IsActive { get; set; } = true;
        public string Code { get; set; } = string.Empty;
        public List<string> Formulas { get; set; } = [];
        public List<decimal> WorkloadThresholds { get; set; } = [0m, 0m, 0m];

        public int? ActionId { get; set; }
        public int? LocationId { get; set; }
        public int? ActionTypeId { get; set; }
        public int? FallId { get; set; }
        public int? UnitGroupId { get; set; }

        public List<int> VisibleFolderIds { get; set; } = [];

        public int? CapacityResourceId { get; set; }
    }
}
