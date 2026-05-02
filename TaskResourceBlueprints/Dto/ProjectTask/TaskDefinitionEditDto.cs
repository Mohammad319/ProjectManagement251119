using ProjectManagement.Shared.Base.Calculation;
using TaskResourceBlueprints.Entities.Tasks;

namespace TaskResourceBlueprints.Dto.ProjectTask
{
    public sealed class TaskDefinitionEditDto
    {
        public int Id { get; set; }
        public TaskStatusEnum Status { get; set; } = TaskStatusEnum.ToPlan;
        public string? Responsible { get; set; }
        public string? AdminNote { get; set; }
        public string? Code { get; set; }
        public string DisplayName { get; set; } = string.Empty;

        public string? UnitCode { get; set; }

        public decimal? Quantity { get; set; }
        public decimal? PriceProduction { get; set; }

        public decimal ChangeFactor1 { get; set; } = 1m;
        public decimal ChangeFactor2 { get; set; } = 1m;
        public bool Uncontrollable { get; set; }

        public bool IsActive { get; set; } = true;
        public string? Note { get; set; }

        public decimal? Thickness { get; set; }
        public decimal? Width { get; set; }
        public decimal? Length { get; set; }

        public List<int> VisibleFolderIds { get; set; } = new();
        public List<TaskConversionParameter> ConversionParameters { get; set; } = [];
        public List<int> SelectedStateIds { get; set; } = [];
    }
}
