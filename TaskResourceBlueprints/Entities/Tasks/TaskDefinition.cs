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
        UnderWorking = 3,
        Ready = 10,
        SuggestionOnly = 20,
        TrainingOnly = 30,
    }
    public class TaskDefinition : TaskLookupBase
    {
        private const int MaxNormalizedTextLength = 800;

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
        public string? ParentCode { get; set; }
        public string? ParentName { get; set; }
        public string? HierarchyPath { get; set; }
        public string NormalizedTextSv { get; set; } = string.Empty;
        public List<string> NameSynonyms { get; set; } = [];
        public List<string> UnitSynonyms { get; set; } = [];
        public int UsageCount { get; set; }

        public List<decimal> WorkloadThresholds { get; set; } = new() { 0m, 0m, 0m };

        public List<string> RowNotes { get; set; } = [];
        public List<int> VisibleFolderIds { get; set; } = [];

        public int? CapacityResourceId { get; set; }

        [NotMapped]
        public string? NewUnitCode { get; set; }

        public List<TaskConversionParameter> ConversionParameters { get; set; } = [];

        public byte[] RowVersion { get; set; } = [];
        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public List<TaskDefinitionStateLink> StateLinks { get; set; } = [];
        public List<TaskDefinitionResourceLink> ResourceLinks { get; set; } = [];

        public void RefreshNormalizedTextSv()
        {
            var parts = new List<string?>
            {
                SwedishTaskTextNormalizer.NormalizeTask(Name, Code, UnitCode, Quantity),
                ParentCode,
                ParentName,
                HierarchyPath,
            };
            parts.AddRange(NameSynonyms);
            parts.AddRange(UnitSynonyms);

            var normalized = SwedishTaskTextNormalizer.Normalize(string.Join(' ', parts.Where(x => !string.IsNullOrWhiteSpace(x))));
            NormalizedTextSv = normalized.Length <= MaxNormalizedTextLength
                ? normalized
                : normalized[..MaxNormalizedTextLength].Trim();
        }
    }
}
