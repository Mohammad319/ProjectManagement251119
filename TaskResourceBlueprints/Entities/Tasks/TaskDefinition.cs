using ProjectManagement.Shared.Base.Calculation;
using TaskResourceBlueprints.Entities.Lookups;
using ProjectManagement.Shared.Helper.ProjectAppStorage;
using ProjectManagement.Shared.Helper.Text;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskResourceBlueprints.Entities.Tasks
{
    public class TaskLookupBase
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsVisible { get; set; } = true;
    }

    public enum TaskStatusEnum
    {
        ToPlan = 1,
        ToDo = 2,
        UnderWorking = 3,
        Ready = 10,
    }
    public class TaskDefinition : TaskLookupBase
    {
        public TaskStatusEnum Status { get; set; }

        public string? Responsible { get; set; }
        public string? AdminNote { get; set; }
        public string? FieldNotes { get; set; }

        public decimal? Quantity { get; set; }
        public decimal? PriceProduction { get; set; }
        public string? UnitCode { get; set; }

        public decimal ChangeFactor1 { get; set; } = 1m;
        public decimal ChangeFactor2 { get; set; } = 1m;

        public bool IsActive { get; set; } = true;
        public bool Uncontrollable { get; set; }

        public string? Code { get; set; }
        public string NormalizedTextSv { get; set; } = string.Empty;
        public int UsageCount { get; set; }

        public List<decimal> WorkloadThresholds { get; set; } = new() { 0m, 0m, 0m };

        public List<string> RowNotes { get; set; } = [];
        public List<int> VisibleFolderIds { get; set; } = [];

        public int? CapacityResourceId { get; set; }

        [NotMapped]
        public string? NewUnitCode { get; set; }

        public List<TaskConversionParameter> ConversionParameters { get; set; } = [];

        public List<TaskDefinitionStateLink> StateLinks { get; set; } = [];
        public List<TaskDefinitionResourceLink> ResourceLinks { get; set; } = [];

        public void RefreshNormalizedTextSv()
        {
            NormalizedTextSv = SwedishTaskTextNormalizer.NormalizeTask(Name, Code, UnitCode, Quantity);
        }
    }
}
