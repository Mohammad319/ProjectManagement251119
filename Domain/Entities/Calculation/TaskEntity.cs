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
        private TaskData? _metadata = new();
        public TaskData Metadata
        {
            get => _metadata ??= new TaskData();
            set => _metadata = value;
        }

        [Required(
            ErrorMessageResourceName = ErrorsMessages.FieldIsRequred,
            ErrorMessageResourceType = typeof(ResLocalize))]
        [MaxLength(
            80,
            ErrorMessageResourceName = ErrorsMessages.MaxLength,
            ErrorMessageResourceType = typeof(ResLocalize))]
        public string Name { get; set; } = string.Empty;

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
        public List<TaskEntity> Tasks { get; set; } = [];

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
