using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.ResourceType;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.ResourceType
{
    [Index(nameof(TenantId), nameof(ResourceTypeId))]
    public sealed class ResourceSortEntity : AuditableEntity<int>
    {
        private ResourceTypeData? _metadata;
        public ResourceTypeData Metadata
        {
            get => _metadata ??= new ResourceTypeData();
            private set => _metadata = value;
        }

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        public bool IsVisible { get; private set; } = true;
        public int SortOrder { get; private set; }

        public int ResourceTypeId { get; private set; }

        [JsonIgnore]
        public ResourceTypeEntity ResourceType { get; private set; } = null!;

        public int? AccountId { get; private set; }

        [JsonIgnore]
        public AccountEntity? Account { get; private set; }

        [JsonIgnore]
        public ICollection<Domain.Entities.Calculation.ResourceEntity> Resources { get; private set; } = [];

        private ResourceSortEntity() { } // EF

        public static ResourceSortEntity Create(PostResourceSortDTO dto, int resourceTypeId, int sortOrder)
        {
            var e = new ResourceSortEntity
            {
                ResourceTypeId = resourceTypeId,
                SortOrder = sortOrder
            };
            e.Update(dto);
            return e;
        }

        public void Update(PostResourceSortDTO dto)
        {
            Name = dto.Name;
            IsVisible = dto.IsVisible;
            AccountId = dto.AccountId;

            Metadata = new ResourceTypeData();
            dto.CopyPropertiesTo(Metadata);
        }

        public void UpdateOrder(int sortOrder) => SortOrder = sortOrder;
    }
}
