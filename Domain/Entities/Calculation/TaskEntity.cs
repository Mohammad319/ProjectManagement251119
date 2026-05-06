using Domain.Entities.Base;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Helper.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    [Index(nameof(TenantId), nameof(CalculationId))]
    public sealed class TaskEntity : AuditableEntity<int>
    {
        private TaskMetadata? _metadata;
        public TaskMetadata Metadata
        {
            get => _metadata ??= new TaskMetadata();
            private set => ApplyMetadataSnapshot(value);
        }

        [Required, MaxLength(FieldLengths.TaskName)]
        public string Name { get; private set; } = string.Empty;

        [MaxLength(FieldLengths.NormalizedText)]
        public string NormalizedTextSv { get; private set; } = string.Empty;

        public bool IsActive { get; private set; } = true;

        [MaxLength(FieldLengths.Unit)]
        public string? Unit { get; private set; }
        public TaskType Type { get; private set; }

        public int SortOrder { get; private set; }

        [MaxLength(FieldLengths.Comment)]
        public string? Note { get; private set; }

        public decimal? Quantity { get; private set; }

        [MaxLength(FieldLengths.Code)]
        public string? Code { get; private set; }

        public bool IsOH { get; private set; }

        public int? ParentTaskId { get; private set; }

        [JsonIgnore]
        [ForeignKey(nameof(ParentTaskId))]
        public TaskEntity? ParentTask { get; private set; }

        public ICollection<TaskEntity> Tasks { get; private set; } = [];

        public int? OpportunityId { get; private set; }

        [JsonIgnore]
        public OpportunityEntity? Opportunity { get; private set; }

        public int CalculationId { get; private set; }

        [JsonIgnore]
        public CalculationEntity Calculation { get; private set; } = null!;

        public int? StatusId { get; private set; }

        [JsonIgnore]
        public TaskStatusEntity? Status { get; private set; }

        public ICollection<ResourceEntity> Resources { get; private set; } = [];

        private TaskEntity() { }

        public static TaskEntity Create(int calculationId, TaskPostDTO dto, int sortOrder, int? parentTaskId = null)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            var entity = new TaskEntity();
            entity.SetCalculation(calculationId);
            entity.SetParentTask(parentTaskId);
            entity.SetSortOrder(sortOrder);
            entity.Update(dto);
            return entity;
        }

        public static TaskEntity CloneForCalculation(TaskEntity source)
        {
            ArgumentNullException.ThrowIfNull(source);

            var clone = new TaskEntity
            {
                Name = source.Name,
                NormalizedTextSv = source.NormalizedTextSv,
                StatusId = source.StatusId,
                Unit = source.Unit,
                Quantity = source.Quantity,
                Metadata = source.GetMetadataSnapshot(),
            };

            clone.SetSortOrder(source.SortOrder);

            foreach (var resource in source.Resources)
                clone.Resources.Add(ResourceEntity.CloneForTask(resource));

            foreach (var child in source.Tasks)
                clone.Tasks.Add(CloneForCalculation(child));

            return clone;
        }

        public void Update(TaskPostDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            Name = NormalizeRequired(dto.Name, "Task name is required.", FieldLengths.TaskName, nameof(dto.Name));
            Unit = NormalizeOptional(dto.Unit);
            Quantity = NormalizeQuantity(dto.Quantity);
            Metadata = CalculationItemMetadataMapper.BuildTaskMetadata(
                dto.Metadata,
                dto.Note,
                dto.Code,
                dto.IsActive,
                dto.Type,
                dto.IsOH);

            SetOpportunity(dto.OpportunityId);
            StatusId = dto.StatusId is > 0 ? dto.StatusId : null;
            SetParentTask(dto.ParentTaskId);
            RefreshNormalizedTextSv();
        }

        public TaskMetadata GetMetadataSnapshot()
        {
            return CalculationItemMetadataMapper.BuildTaskMetadata(_metadata, Note, Code, IsActive, Type, IsOH);
        }

        public void UpdateMetadata(Action<TaskMetadata> update)
        {
            ArgumentNullException.ThrowIfNull(update);

            var snapshot = GetMetadataSnapshot();
            update(snapshot);
            Metadata = snapshot;
        }

        public void SetIsOH(bool isOH) => UpdateMetadata(x => x.IsOH = isOH);

        public void SetQuantity(decimal? value)
        {
            Quantity = NormalizeQuantity(value);
            RefreshNormalizedTextSv();
        }

        public void SetSortOrder(int sortOrder)
        {
            ValidateSortOrder(sortOrder);
            SortOrder = sortOrder;
        }

        public void SetParentTask(int? parentTaskId)
        {
            ParentTaskId = parentTaskId is > 0 ? parentTaskId : null;
        }

        public void SetCalculation(int calculationId)
        {
            if (calculationId <= 0)
                throw new ValidationException("CalculationId must be greater than zero.");

            CalculationId = calculationId;
        }

        public void SetOpportunity(int? opportunityId)
        {
            OpportunityId = opportunityId is > 0 ? opportunityId : null;
        }

        public void RefreshNormalizedTextSv()
        {
            NormalizedTextSv = SwedishTaskTextNormalizer.NormalizeTask(Name, Code, Unit, Quantity);
        }

        public void ClearOpportunity()
        {
            OpportunityId = null;
            Opportunity = null;
        }

        public void ResetIdentityForClone()
        {
            Id = 0;
            RowVersion = Array.Empty<byte>();
            SetParentTask(null);
            ParentTask = null;
            Status = null;
            Opportunity = null;
            Calculation = null!;
        }

        public void SetChildTasks(ICollection<TaskEntity> children)
        {
            Tasks = children ?? [];
        }

        public void SetResources(ICollection<ResourceEntity> resources)
        {
            Resources = resources ?? [];
        }

        private void ApplyMetadataSnapshot(TaskMetadata? metadata)
        {
            var snapshot = NormalizeMetadata(metadata);
            _metadata = snapshot;
            Note = NormalizeOptional(snapshot.Note);
            Code = NormalizeOptional(snapshot.Code);
            IsActive = snapshot.IsActive;
            Type = snapshot.Type;
            IsOH = snapshot.IsOH;
        }

        private static TaskMetadata NormalizeMetadata(TaskMetadata? metadata)
            => CalculationItemMetadataMapper.CloneTaskMetadata(metadata);

        private static string NormalizeRequired(string? value, string errorMessage, int maxLength, string fieldName)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                throw new ValidationException(errorMessage);
            if (trimmed.Length > maxLength)
                throw new ValidationException($"{fieldName} cannot exceed {maxLength} characters.");
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
