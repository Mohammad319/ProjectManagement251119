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
            private set => ApplyMetadataSnapshot(value);
        }

        [Required, MaxLength(FieldLengths.LongName)]
        public string Name { get; private set; } = string.Empty;
        public bool IsActive { get; private set; } = true;

        [MaxLength(FieldLengths.Unit)]
        public string? Unit { get; private set; }
        public ResourceTypesEnum ResType { get; private set; }

        public int SortOrder { get; private set; }

        [MaxLength(FieldLengths.Comment)]
        public string? Note { get; private set; }

        // Production note – a standalone comment from production/PL/AL/Viewer per calculation row.
        // Stored separately from the economic metadata; does not change quantity/price/cost/calculation.
        // May be edited even when the calculation is locked (permission is enforced in the backend).
        [MaxLength(FieldLengths.Comment)]
        public string? ProductionNote { get; private set; }

        // Reviewer comment – a standalone comment from the reviewer (Granskare) per calculation row.
        // Stored separately from the economic metadata; does not change quantity/price/cost/calculation.
        // May be edited even when the calculation is locked (permission is enforced in the backend).
        [MaxLength(FieldLengths.Comment)]
        public string? ReviewerComment { get; private set; }

        public decimal? Quantity { get; private set; }

        public IReadOnlyList<string> Notes => _metadata?.UpperNote ?? [];

        public int TaskId { get; private set; }

        [JsonIgnore]
        public TaskEntity Task { get; private set; } = null!;

        public int? OpportunityId { get; private set; }

        [JsonIgnore]
        public OpportunityEntity? Opportunity { get; private set; }

        public int? AccountId { get; private set; }

        [JsonIgnore]
        public AccountEntity? Account { get; private set; }

        public int? StatusId { get; private set; }

        [JsonIgnore]
        public StatusResourcesEntity? Status { get; private set; }

        public int? ResourceSortId { get; private set; }

        [JsonIgnore]
        public ResourceSortEntity? ResourceSort { get; private set; }

        public int? ResourceTypeId { get; private set; }

        [JsonIgnore]
        public ResourceTypeEntity? ResourceType { get; private set; }

        public int? PrimaryOfferId { get; private set; }

        [JsonIgnore]
        public ICollection<OfferEntity> Offers { get; private set; } = [];

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
                Unit = source.Unit,
                Quantity = source.Quantity,
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
            Unit = NormalizeOptional(dto.Unit);
            Quantity = NormalizeQuantity(dto.Quantity);

            Metadata = CalculationItemMetadataMapper.BuildResourceMetadata(
                dto.Data,
                dto.Note);

            SetSortOrder(dto.SortOrder);

            SetOpportunity(dto.OpportunityId);
            AccountId = dto.AccountId is > 0 ? dto.AccountId : null;
            StatusId = dto.StatusId is > 0 ? dto.StatusId : null;
            ResourceSortId = dto.ResourceSortId is > 0 ? dto.ResourceSortId : null;
            ResourceTypeId = dto.ResourceTypeId is > 0 ? dto.ResourceTypeId : null;
            SetPrimaryOffer(dto.OfferId);
        }

        public ResourceMetadata GetMetadataSnapshot()
        {
            return CalculationItemMetadataMapper.BuildResourceMetadata(_metadata, Note);
        }

        public void UpdateMetadata(Action<ResourceMetadata> update)
        {
            ArgumentNullException.ThrowIfNull(update);

            var snapshot = GetMetadataSnapshot();
            update(snapshot);
            Metadata = snapshot;
        }

        public void SetQuantity(decimal? value)
        {
            Quantity = NormalizeQuantity(value);
        }

        /// <summary>Sets the production note (does not affect economy/calculation). Returns true if the value changed.</summary>
        public bool SetProductionNote(string? value)
        {
            var normalized = NormalizeOptional(value);
            if (string.Equals(ProductionNote, normalized, StringComparison.Ordinal))
                return false;
            ProductionNote = normalized;
            return true;
        }

        /// <summary>Sets the reviewer comment (does not affect economy/calculation). Returns true if the value changed.</summary>
        public bool SetReviewerComment(string? value)
        {
            var normalized = NormalizeOptional(value);
            if (string.Equals(ReviewerComment, normalized, StringComparison.Ordinal))
                return false;
            ReviewerComment = normalized;
            return true;
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

        public void SetOpportunity(int? opportunityId)
        {
            OpportunityId = opportunityId is > 0 ? opportunityId : null;
        }

        public void MoveToTask(int taskId, int sortOrder)
        {
            SetTask(taskId);
            SetSortOrder(sortOrder);
        }

        public void SetPrimaryOffer(int? offerId)
        {
            PrimaryOfferId = offerId is > 0 ? offerId : null;
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

        private static decimal? NormalizeQuantity(decimal? value)
        {
            if (!value.HasValue)
                return null;

            var rounded = Math.Round(value.Value, 3, MidpointRounding.AwayFromZero);
            return rounded < 0m ? 0m : rounded;
        }

        private static void ValidateSortOrder(int sortOrder)
        {
            if (sortOrder < 0)
                throw new ValidationException("SortOrder cannot be negative.");
        }
    }
}
