using Domain.Entities.Base;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class TaskEntity : IntBaseEntity
    {
        private TaskData? _metadata;
        public TaskData Metadata
        {
            get => _metadata ??= new TaskData();
            set => _metadata = value;
        }

        [Required, MaxLength(FieldLengths.Name)]
        public required string Name { get; set; }
        [MaxLength(FieldLengths.Comment)]
        public string? Note { get; set; }
        public double? Quantity { get; set; }
        [MaxLength(FieldLengths.Unit)]
        public string? Unit { get; set; }
        public double ChangeFactor1 { get; set; } = 1;
        public double ChangeFactor2 { get; set; } = 1;

        [Range(-20, 20, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(ResLocalize))]
        public double? Cap { get; set; }
        public bool IsActive { get; set; } = true;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
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

        public List<ResourceEntity> Resources { get; set; } = [];
    }
}
