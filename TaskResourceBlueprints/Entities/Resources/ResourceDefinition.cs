using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using TaskResourceBlueprints.Entities.Resources;

namespace TaskResourceBlueprints.Entities
{
    public class ResourceDefinition
    {
        public int Id { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        public ResourceCategory? Folder { get; set; }
        public int? FolderId { get; set; }
        public string? AdminNote { get; set; }
        public List<RoleDTO> CostRoles { get; set; } = [];

        public ResourceTypesEnum ResType { get; set; }
        public string Name { get; set; } = null!;
        [Range(0, double.MaxValue)] public int SortOrder { get; set; }
        public string? Unit { get; set; }
        public decimal? Quantity { get; set; }
        private ResourceMetadata _data = new();
        public ResourceMetadata Data
        {
            get => _data ??= new ResourceMetadata();
            set => _data = value;
        }

        public List<ResourceTenantLinkEntity> TenantLinks { get; set; } = [];
    }
}
