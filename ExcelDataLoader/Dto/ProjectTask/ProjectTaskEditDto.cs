using ProjectImportHub.Entities;
using ProjectImportHub.Entities.Tasks;

namespace ProjectImportHub.Dto.ProjectTask
{
    public sealed class ProjectTaskEditDto
    {
        public int Id { get; set; }
        public TaskStatusEnum Status { get; set; } = TaskStatusEnum.ToPlan;
        public string? Responsible { get; set; }
        public string? AdminNote { get; set; }
        public int? ActionId { get; set; }
        public int? ActionTypeId { get; set; }
        public int? FallId { get; set; }
        public int? LocationId { get; set; }
        public string? Code { get; set; }
        public string DisplayName { get; set; } = string.Empty;

        public int? UnitGroupId { get; set; }     // früher TaskUnitGroupId
        public string? UnitCode { get; set; }     // früher Unit

        public double? Quantity { get; set; }

        public double ChangeFactor1 { get; set; } = 1;    // früher ChangeFactor1
        public double ChangeFactor2 { get; set; } = 1;  // früher ChangeFactor2
        public bool Uncontrollable { get; set; }

        public bool IsActive { get; set; } = true;
        public string? Note { get; set; }

        // Deutsch: drei Eingabefelder (Dicke/Breite/Länge) – bequemer als Index-Binding auf eine Liste
        public double? Thickness { get; set; } // WorkloadThresholds[0]
        public double? Width { get; set; }     // WorkloadThresholds[1]
        public double? Length { get; set; }    // WorkloadThresholds[2]

        public List<int> VisibleFolderIds { get; set; } = new();    // früher VisibleFolders

        // Hilfsanzeige (RowNotes…): füg’s später hinzu, wenn nötig
    }
}
