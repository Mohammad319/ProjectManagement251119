using Domain.Entities.Base;
using Domain.Entities.ResourceType;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    [Index(nameof(TenantId), nameof(TaskId))]
    public sealed class ResourceEntity : AuditableEntity<int>
    {
        private ResourceMetadata? _metadata;
        public ResourceMetadata Metadata
        {
            get => _metadata ??= new ResourceMetadata();
            set => ApplyMetadataSnapshot(value);
        }

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        [MaxLength(FieldLengths.Unit)]
        public string? Unit { get; set; }
        public ResourceTypesEnum ResType { get; set; }

        /// <summary>
        /// SortOrder of resource in UI display.
        /// </summary>
        public int SortOrder { get; set; }

        [MaxLength(FieldLengths.Comment)]
        public string? Note { get; set; }

        public int TaskId { get; set; }

        [JsonIgnore]
        public TaskEntity Task { get; set; } = null!;

        public int? OpportunityId { get; set; }

        [JsonIgnore]
        public OpportunityEntity? Opportunity { get; set; }

        public int? AccountId { get; set; }

        [JsonIgnore]
        public AccountEntity? Account { get; set; }

        public int? StatusId { get; set; }

        [JsonIgnore]
        public StatusResourcesEntity? Status { get; set; }

        public int? ResourceSortId { get; set; }

        [JsonIgnore]
        public ResourceSortEntity? ResourceSort { get; set; }

        public int? ResourceTypeId { get; set; }

        [JsonIgnore]
        public ResourceTypeEntity? ResourceType { get; set; }

        public int? PrimaryOfferId { get; set; }

        [JsonIgnore]
        public ICollection<OfferEntity> Offers { get; set; } = [];

        public static ResourceEntity Create(ResourcePostDTO dto, int sortOrder, int? parentTaskId = null)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var entity = new ResourceEntity();
            entity.Update(dto);

            if (parentTaskId is > 0)
                entity.SetTask(parentTaskId.Value);

            entity.SetSortOrder(sortOrder);
            return entity;
        }

        public static ResourceEntity CloneForTask(ResourceEntity source)
        {
            ArgumentNullException.ThrowIfNull(source);

            var clone = new ResourceEntity
            {
                Name = source.Name,
                ResType = source.ResType,
                IsActive = source.IsActive,
                Metadata = source.GetMetadataSnapshot(),
                AccountId = source.AccountId,
                StatusId = source.StatusId,
                ResourceSortId = source.ResourceSortId,
                ResourceTypeId = source.ResourceTypeId,
            };

            clone.SetSortOrder(source.SortOrder);
            return clone;
        }

        public void Update(ResourcePostDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            Name = NormalizeRequired(dto.Name, "Resource name is required.");
            ResType = dto.ResType;
            IsActive = dto.IsActive;

            Metadata = CalculationItemMetadataMapper.BuildResourceMetadata(
                dto.Data,
                dto.Note,
                dto.Unit);

            SetSortOrder(dto.SortOrder);

            OpportunityId = dto.OpportunityId;
            AccountId = dto.AccountId;
            StatusId = dto.StatusId;
            ResourceSortId = dto.ResourceSortId;
            ResourceTypeId = dto.ResourceTypeId;
            PrimaryOfferId = dto.OfferId;
        }

        public ResourceMetadata GetMetadataSnapshot()
            => CalculationItemMetadataMapper.BuildResourceMetadata(_metadata, Note, Unit);

        public void UpdateMetadata(Action<ResourceMetadata> update)
        {
            ArgumentNullException.ThrowIfNull(update);

            var snapshot = GetMetadataSnapshot();
            update(snapshot);
            Metadata = snapshot;
        }

        public void SetTask(int taskId)
        {
            if (taskId <= 0)
                throw new ValidationException("TaskId must be greater than zero.");

            TaskId = taskId;
        }

        public void SetSortOrder(int sortOrder)
        {
            ValidateSortOrder(sortOrder);
            SortOrder = sortOrder;
        }

        public void MoveToTask(int taskId, int sortOrder)
        {
            SetTask(taskId);
            SetSortOrder(sortOrder);
        }

        public void ClearCrossCalculationState(bool resetQuantityParam)
        {
            OpportunityId = null;
            Opportunity = null;
            PrimaryOfferId = null;
            Offers = [];

            if (resetQuantityParam && !string.IsNullOrWhiteSpace(Metadata.QuantityParam))
                UpdateMetadata(m => m.QuantityParam = PMValuesConst.FixedQ);
        }

        public void ResetIdentityForClone()
        {
            Id = 0;
            RowVersion = Array.Empty<byte>();
            TaskId = 0;
            Task = null!;
            Opportunity = null;
            Account = null;
            Status = null;
            ResourceSort = null;
            ResourceType = null;
            PrimaryOfferId = null;
            Offers = [];
        }

        private void ApplyMetadataSnapshot(ResourceMetadata? metadata)
        {
            var snapshot = NormalizeMetadata(metadata);
            _metadata = snapshot;
            Note = NormalizeOptional(snapshot.Note);
            Unit = NormalizeOptional(snapshot.Unit);
        }

        private static ResourceMetadata NormalizeMetadata(ResourceMetadata? metadata)
            => CalculationItemMetadataMapper.CloneResourceMetadata(metadata);

        private static string NormalizeRequired(string? value, string errorMessage)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                throw new ValidationException(errorMessage);
            return trimmed;
        }

        private static string? NormalizeOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static void ValidateSortOrder(double sortOrder)
        {
            if (double.IsNaN(sortOrder) || double.IsInfinity(sortOrder) || sortOrder < 0)
                throw new ValidationException("SortOrder must be a finite number greater than or equal to zero.");
        }
    }
}
