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
            private set => _metadata = ResourceTypeMetadataMapper.Build(value);
        }

        [JsonIgnore]
        public ICollection<ResourceSortEntity> ResourcesSort { get; private set; } = [];

        [JsonIgnore]
        public ICollection<Domain.Entities.Calculation.ResourceEntity> Resources { get; private set; } = [];

        private ResourceTypeEntity() { }

        public static ResourceTypeEntity Create(PostResourceTypeDTO dto, int sortOrder)
        {
            var entity = new ResourceTypeEntity();
            entity.SetSortOrder(sortOrder);
            entity.Update(dto);
            return entity;
        }

        public void Update(PostResourceTypeDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            Name = NormalizeName(dto.Name);
            IsVisible = dto.IsVisible;
            Kind = dto.Type;
            AccountId = NormalizeOptionalPositive(dto.AccountId);
            Metadata = dto.Data;
        }

        public ResourceTypeData GetMetadataSnapshot()
            => ResourceTypeMetadataMapper.Build(_metadata);

        public void UpdateMetadata(Action<ResourceTypeData> update)
        {
            ArgumentNullException.ThrowIfNull(update);

            var snapshot = GetMetadataSnapshot();
            update(snapshot);
            Metadata = snapshot;
        }

        public void UpdateOrder(int sortOrder) => SetSortOrder(sortOrder);

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
                throw new ValidationException("Resource type name is required.");
            return trimmed;
        }

        private static int? NormalizeOptionalPositive(int? value)
        {
            if (!value.HasValue || value.Value <= 0)
                return null;
            return value.Value;
        }

    }
}
