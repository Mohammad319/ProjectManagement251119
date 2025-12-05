using TaskResourceBlueprints.Entities.Resources;
using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.App.Dataloader;
using ProjectManagement.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskResourceBlueprints.Entities
{
    public class ResourceDefinition
    {
        public CalcResCost CalcResCost { get; set; } = default!;

        public int Id { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        public ResourceCategory? Folder { get; set; }
        public int? FolderId { get; set; }
        public string? AdminNote { get; set; }
        public List<RoleDTO> CostRoles { get; set; } = [];

        public ResourceTypesEnum ResType { get; set; }
        public string Name { get; set; } = null!;
        [Range(0, double.MaxValue)] public double SortOrder { get; set; }
        private ResourceData _data = new();
        public ResourceData Data
        {
            get => _data ??= new ResourceData();
            set => _data = value;
        }

        public List<ResourceAttributeValue> AttributeValues { get; set; } = [];
        public List<ResourceTenantLinkEntity> TenantLinks { get; set; } = [];
    }
}
