using Domain.Entities.Base;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.ResourceType;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.ResourceType
{
    public sealed class ResourceTypeEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.Name)]
        public  string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public bool IsVisible { get; set; } = true;
        public ResourceTypesEnum Kind { get; set; }

        public int? AccountId { get; set; }
        [JsonIgnore]
        public AccountEntity? Account { get; set; }

        private ResourceTypeData? _metadata;
        public ResourceTypeData Metadata
        {
            get => _metadata ??= new();
            set => _metadata = value;
        }

        [JsonIgnore]
        public ICollection<ResourceSortEntity> ResourcesSort { get; set; } = [];

        [JsonIgnore]
        public ICollection<ResourceEntity> Resources { get; set; } = [];
    }


    public class ResourceSortEntity : AuditableEntity<int>
    {

        private ResourceTypeData? _metadata;
        public ResourceTypeData Metadata
        {
            get => _metadata ??= new ResourceTypeData();
            set => _metadata = value;
        }

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; set; } = string.Empty;

        public bool IsVisible { get; set; } = true;

        public int SortOrder { get; set; }

        public int ResourceTypeId { get; set; }

        [JsonIgnore]
        public ResourceTypeEntity ResourceType { get; set; } = null!;

        public int? AccountId { get; set; }
        public AccountEntity? Account { get; set; }

        [JsonIgnore]
        public ICollection<ResourceEntity> Resources { get; set; } = [];
    }

}
