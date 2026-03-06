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
            set => _metadata = NormalizeMetadata(value);
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
                Code = source.Code,
                StatusId = source.StatusId,
                Type = source.Type,
                Unit = source.Unit,
                Note = source.Note,
                IsActive = source.IsActive,
                IsOH = source.IsOH,
                Metadata = source.Metadata.Clone(),
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
            Note = NormalizeOptional(dto.Note);
            Unit = NormalizeOptional(dto.Unit);
            Code = NormalizeOptional(dto.Code);

            IsActive = dto.IsActive;
            Type = dto.Type;
            IsOH = dto.IsOH;

            Metadata = dto.Metadata?.Clone() ?? new TaskMetadata();

            // Keep duplicated fields in sync (scalar columns + JSON metadata)
            Metadata.Note = Note ?? string.Empty;
            Metadata.Unit = Unit ?? string.Empty;
            Metadata.Code = Code ?? string.Empty;
            Metadata.Type = Type;
            Metadata.IsActive = IsActive;
            Metadata.IsOH = IsOH;
            Metadata.Normalize();

            OpportunityId = dto.OpportunityId;
            StatusId = dto.StatusId;
            SetParentTask(dto.ParentTaskId);
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

        private static TaskMetadata NormalizeMetadata(TaskMetadata? metadata)
        {
            var clean = metadata?.Clone() ?? new TaskMetadata();
            clean.Note = NormalizeOptional(clean.Note) ?? string.Empty;
            clean.Unit = NormalizeOptional(clean.Unit) ?? string.Empty;
            clean.Code = NormalizeOptional(clean.Code) ?? string.Empty;
            clean.Responsible = NormalizeOptional(clean.Responsible) ?? string.Empty;
            clean.QuantityParam = NormalizeOptional(clean.QuantityParam) ?? string.Empty;
            clean.Normalize();
            return clean;
        }

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
