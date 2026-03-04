using ProjectManagement.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManagement.Shared.Base.AppTenant
{
    public class RoleDTO
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public double? Value { get; set; }
    }
    public class CapResourceBase
    {
        public string Name { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
        public ResourceTypesEnum ResType { get; set; } = ResourceTypesEnum.Materials;
        public List<int> Groups { get; set; } = [];
        public List<RoleDTO> Role { get; set; } = [];
        public double? GetRole(double q)
        {
            return Role?.FirstOrDefault(x => x.Min <= q && x.Max >= q)?.Value;
        }
    }
}
