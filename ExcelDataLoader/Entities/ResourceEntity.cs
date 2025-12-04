using ProjectImportHub.Entities.Assignments;
using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.App.Dataloader;
using ProjectManagement.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectImportHub.Entities
{
    public class ResourceEntity
    {
        public CalcResCost? CalcResCost { get; set; }

        public int Id { get; set; }
        public bool Active { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        public ResourceFolderEntity? Folder { get; set; }
        public int? FolderId { get; set; }
        public string? AdminNote { get; set; }
        public List<RoleDTO>? CostRole { get; set; } = [];


        public ResourceTypesEnum ResType { get; set; }
        public string Name { get; set; } = null!;
        [Range(0, double.MaxValue)] public double SortOrder { get; set; }
        private ResourceData _data = new();
        public ResourceData Data
        {
            get => _data ??= new ResourceData();
            set => _data = value;
        }

        public List<ResourcePropertyBindEntity> PropertiesBind { get; set; } = [];
        public List<ResourceTenantLinkEntity> ResourcesTenant { get; set; } = [];
    }
}
