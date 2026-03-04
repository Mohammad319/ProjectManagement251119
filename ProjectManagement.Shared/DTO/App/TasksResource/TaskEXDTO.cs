using System.Collections.Generic;

namespace ProjectManagement.Shared.DTO.App.TasksResource
{
    public class TaskEXBaseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Note { get; set; } = string.Empty;
        public List<string> UpperNote { get; set; } = [];
        public double? Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public double ChangeFactor1 { get; set; } = 1;
        public double ChangeFactor2 { get; set; } = 1;
        public bool Active { get; set; } = true;
        public string Code { get; set; } = string.Empty;
        public bool IsOH { get; set; }
        public int? ActionId { get; set; }
        public int? LocationId { get; set; }
        public int? ActionTypeId { get; set; }
        public int? FallId { get; set; }
    }
    public class TaskEXDTO : TaskEXBaseDTO
    {
        public string Action { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string Fall { get; set; } = string.Empty;
        public int? ResIdCap { get; set; }
        public List<int> ConditionsIDs { get; set; } = new();
    }
}
