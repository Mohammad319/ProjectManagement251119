using ProjectManagement.Shared.Base.AppTenant;
using System.Collections.Generic;

namespace ProjectManagement.Shared.DTO.App.Dataloader
{
    public class TaskEXMDWithResIdsto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public List<int> ResourceIDs { get; set; } = [];
    }
    public class TaskWithResourcesMDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Note { get; set; }
        public double? Quantity { get; set; }
        public string Unit { get; set; }
        public double ChangeFactor1 { get; set; } = 1;
        public double ChangeFactor2 { get; set; } = 1;
        public string Code { get; set; }

        public List<ResourceEXDto> Resources { get; set; } = [];
        public int? ResIdCap { get; set; }

        public List<ConditionDto> ConditionEffect { get; set; } = [];
        public List<RoleDTO> CapRole { get; set; } = [];
    }
}
