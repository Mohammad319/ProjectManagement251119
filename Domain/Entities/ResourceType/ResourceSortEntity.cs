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

        private ResourceSortEntity() { }

        public static ResourceSortEntity Create(PostResourceSortDTO dto, int resourceTypeId, int sortOrder)
        {
            var entity = new ResourceSortEntity();
            entity.SetResourceType(resourceTypeId);
            entity.SetSortOrder(sortOrder);
            entity.Update(dto);
            return entity;
        }

        public void Update(PostResourceSortDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            Name = NormalizeName(dto.Name);
            IsVisible = dto.IsVisible;
            AccountId = NormalizeOptionalPositive(dto.AccountId, nameof(dto.AccountId));

            var metadata = new ResourceTypeData();
            dto.CopyPropertiesTo(metadata);
            metadata.Unit = NormalizeOptional(metadata.Unit) ?? string.Empty;
            metadata.Normalize();
            Metadata = metadata;
        }

        public void UpdateOrder(int sortOrder) => SetSortOrder(sortOrder);

        private void SetResourceType(int resourceTypeId)
        {
            if (resourceTypeId <= 0)
                throw new ValidationException("ResourceTypeId is required.");
            ResourceTypeId = resourceTypeId;
        }

        private void SetSortOrder(int sortOrder)
        {
            if (sortOrder < 0)
                throw new ValidationException("SortOrder cannot be negative.");
            SortOrder = sortOrder;
        }

        private static string NormalizeName(string? value)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                throw new ValidationException("Resource sort name is required.");
            return trimmed;
        }

        private static int? NormalizeOptionalPositive(int? value, string fieldName)
        {
            if (!value.HasValue || value.Value <= 0)
                return null;
            return value.Value;
        }

        private static string? NormalizeOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
