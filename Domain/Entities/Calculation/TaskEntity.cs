using Domain.Entities.Base;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
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
            set => ApplyMetadataSnapshot(value);
        }

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        [MaxLength(FieldLengths.Unit)]
        public string? Unit { get; set; }
        public TaskType Type { get; set; }

        /// <summary>
        /// SortOrder of task in UI display.
        /// </summary>
        public int SortOrder { get; set; }

        [MaxLength(FieldLengths.Comment)]
        public string? Note { get; set; }

        [MaxLength(FieldLengths.Code)]
        public string? Code { get; set; }

        public bool IsOH { get; set; }

        /// <summary>
        /// Parent task reference for hierarchical structure (optional).
        /// </summary>
        public int? ParentTaskId { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(ParentTaskId))]
        public TaskEntity? ParentTask { get; set; }

        /// <summary>
        /// Child tasks (subtasks).
        /// </summary>
        public ICollection<TaskEntity> Tasks { get; set; } = [];

        public int? OpportunityId { get; set; }

        [JsonIgnore]
        public OpportunityEntity? Opportunity { get; set; }

        public int CalculationId { get; set; }

        [JsonIgnore]
        public CalculationEntity Calculation { get; set; } = null!;

        public int? StatusId { get; set; }

        [JsonIgnore]
        public TaskStatusEntity? Status { get; set; }

        public ICollection<ResourceEntity> Resources { get; set; } = [];

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
                StatusId = source.StatusId,
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

            Name = NormalizeRequired(dto.Name, "Task name is required.");
            Metadata = CalculationItemMetadataMapper.BuildTaskMetadata(
                dto.Metadata,
                dto.Note,
                dto.Unit,
                dto.Code,
                dto.IsActive,
                dto.Type,
                dto.IsOH);

            OpportunityId = dto.OpportunityId;
            StatusId = dto.StatusId;
            SetParentTask(dto.ParentTaskId);
        }

        public TaskMetadata GetMetadataSnapshot()
            => CalculationItemMetadataMapper.BuildTaskMetadata(_metadata, Note, Unit, Code, IsActive, Type, IsOH);

        public void UpdateMetadata(Action<TaskMetadata> update)
        {
            ArgumentNullException.ThrowIfNull(update);

            var snapshot = GetMetadataSnapshot();
            update(snapshot);
            Metadata = snapshot;
        }

        public void SetIsOH(bool isOH) => UpdateMetadata(x => x.IsOH = isOH);

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
        }

        private void ApplyMetadataSnapshot(TaskMetadata? metadata)
        {
            var snapshot = NormalizeMetadata(metadata);
            _metadata = snapshot;
            Note = NormalizeOptional(snapshot.Note);
            Unit = NormalizeOptional(snapshot.Unit);
            Code = NormalizeOptional(snapshot.Code);
            IsActive = snapshot.IsActive;
            Type = snapshot.Type;
            IsOH = snapshot.IsOH;
        }

        private static TaskMetadata NormalizeMetadata(TaskMetadata? metadata)
            => CalculationItemMetadataMapper.CloneTaskMetadata(metadata);

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
