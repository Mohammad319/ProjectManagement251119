using Domain.Entities.Base;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.ValueObjects.Calculation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    [Index(nameof(TenantId), nameof(CalculationId))]
    public sealed class TaskEntity : IntBaseEntity
    {
        private TaskData? _metadata;
        public TaskData Metadata
        {
            get => _metadata ??= new TaskData();
            set => _metadata = value;
        }

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; set; } = string.Empty;

        public double? Quantity { get; set; }

        [MaxLength(FieldLengths.Comment)]
        public string? Note { get; set; }

        [MaxLength(FieldLengths.Unit)]
        public string? Unit { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(FieldLengths.Code)]
        public string? Code { get; set; }

        public TaskType Type { get; set; }

        public bool IsOH { get; set; }

        /// <summary>
        /// SortOrder of task in UI display.
        /// </summary>
        public double SortOrder { get; set; }

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

        // -----------------------
        // Opportunity relation
        // -----------------------

        public int? OpportunityId { get; set; }

        [JsonIgnore]
        public OpportunityEntity? Opportunity { get; set; }

        // -----------------------
        // Calculation relation
        // -----------------------

        public int CalculationId { get; set; }

        [JsonIgnore]
        public CalculationEntity Calculation { get; set; } = null!;

        // -----------------------
        // Status
        // -----------------------

        public int? StatusId { get; set; }

        [JsonIgnore]
        public TaskStatusEntity? Status { get; set; }

        // -----------------------
        // Task resources
        // -----------------------

        public ICollection<ResourceEntity> Resources { get; set; } = [];

        // -----------------------
        // Cost (Value Object)
        // -----------------------

        public CostValue Cost { get; private set; } = null!;

        // -----------------------
        // Methods
        // -----------------------

        private TaskEntity() { }

        public static TaskEntity Create(int calculationId, TaskPostDTO dto, double sortOrder, int? parentTaskId = null)
        {
            var entity = new TaskEntity
            {
                CalculationId = calculationId,
                SortOrder = sortOrder,
                ParentTaskId = parentTaskId
            };

            entity.Update(dto);

            return entity;
        }
        public static TaskEntity CloneForCalculation(TaskEntity t)
        {
            var clone = new TaskEntity
            {
                Name = t.Name,
                Code = t.Code,
                StatusId = t.StatusId,
                Type = t.Type,
                Quantity = t.Quantity,
                Unit = t.Unit,
                Note = t.Note,
                IsActive = t.IsActive,
                SortOrder = t.SortOrder,
                Metadata = t.Metadata.Clone()
            };

            foreach (var r in t.Resources)
                clone.Resources.Add(ResourceEntity.CloneForTask(r));

            foreach (var child in t.Tasks)
                clone.Tasks.Add(CloneForCalculation(child));

            return clone;
        }

        public void Update(TaskPostDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ValidationException("Task name is required.");

            Name = dto.Name;
            Quantity = dto.Quantity;
            Note = dto.Note;
            Unit = dto.Unit;
            IsActive = dto.IsActive;
            Code = dto.Code;
            Type = dto.Type;
            IsOH = dto.IsOH;

            Metadata = dto.Metadata;

            OpportunityId = dto.OpportunityId;
            StatusId = dto.StatusId;
            ParentTaskId = dto.ParentTaskId;

            // CostValue
            if (Cost is null)
            {
                Cost = new CostValue(
                    dto.Cost,
                    dto.BaseCost,
                    dto.ChangeFactor1,
                    dto.ChangeFactor2
                );
            }
            else
            {
                Cost.Set(
                    dto.Cost,
                    dto.BaseCost,
                    dto.ChangeFactor1,
                    dto.ChangeFactor2
                );
            }
        }
    }
}
