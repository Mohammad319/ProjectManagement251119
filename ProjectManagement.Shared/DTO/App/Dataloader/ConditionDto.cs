using ProjectManagement.Shared.Enums;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.DTO.App.Dataloader
{
    public class Equation
    {
        public string TargetVarible { get; set; } = null!;
        public string Formula { get; set; } = null!;
        public List<ResourceTypesEnum> Types { get; set; } = [];
    }
    public class ConditionElementDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public List<Equation> Equations { get; set; } = [];

        [JsonIgnore] public bool IsSelected = false;
    }
    public class ConditionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsMultiSelect { get; set; } = false;
        public string Notes { get; set; } = string.Empty;
        public List<ExternalVariable> ExternalVariables { get; set; } = [];
        public List<ConditionElementDto> Items { get; set; } = [];
    }
}
