using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.ResourceType;
using ProjectManagement.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.ResourceType
{
    [Index(nameof(TenantId), nameof(AccountId))]
    public sealed class ResourceTypeEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        public int SortOrder { get; private set; }
        public bool IsVisible { get; private set; } = true;
        public ResourceTypesEnum Kind { get; private set; }

        public int? AccountId { get; private set; }

        [JsonIgnore]
        public AccountEntity? Account { get; private set; }

        private ResourceTypeData? _metadata;
        public ResourceTypeData Metadata
        {
            get => _metadata ??= new();
            private set => _metadata = value;
        }

        [JsonIgnore]
        public ICollection<ResourceSortEntity> ResourcesSort { get; private set; } = [];

        [JsonIgnore]
        public ICollection<Domain.Entities.Calculation.ResourceEntity> Resources { get; private set; } = [];

        private ResourceTypeEntity() { } // EF

        public static ResourceTypeEntity Create(PostResourceTypeDTO dto, int sortOrder)
        {
            var e = new ResourceTypeEntity();
            e.Update(dto);
            e.SortOrder = sortOrder;
            return e;
        }

        public void Update(PostResourceTypeDTO dto)
        {
            Name = dto.Name;
            IsVisible = dto.IsVisible;
            Kind = dto.Type;
            AccountId = dto.AccountId;

            // dto -> metadata (بدون AutoMapper)
            Metadata = new ResourceTypeData();
            dto.CopyPropertiesTo(Metadata);
        }

        public void UpdateOrder(int sortOrder) => SortOrder = sortOrder;
    }
}
