using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Shared.Base.AppTenant
{
    public class RoleDTO
    {
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public decimal? Value { get; set; }
    }

    public class CapResourceBase
    {
        public string Name { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
        public ResourceTypesEnum ResType { get; set; } = ResourceTypesEnum.Materials;
        public List<int> Groups { get; set; } = [];
        public List<RoleDTO> Role { get; set; } = [];

        public decimal? GetRole(decimal q)
            => Role?.FirstOrDefault(x => x.Min <= q && x.Max >= q)?.Value;
    }
}