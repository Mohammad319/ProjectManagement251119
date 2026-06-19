using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.DTO.Calculation
{
    public class TaskPostDTO : TaskBase
    {
        public int Id { get; set; }
        /// <summary>
        /// Concurrency token (rowversion). Send this back on updates to detect stale edits.
        /// </summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
public int? ParentTaskId { get; set; }

        public int? StatusId { get; set; }

        public int? OpportunityId { get; set; }

        public bool OnlyCodeText { get; set; } = false;

        // -----------------------------
        // ✅ مصدر واحد للحقيقة: كل الحقول "المكررة" أصبحت Proxy على Metadata
        // هذا يمنع وجود قيمتين مختلفتين لنفس المعنى (Note/Unit/Code/Factors/...) بين الـ DTO والـ Metadata.
        // -----------------------------

        private TaskMetadata? data = new();

        public TaskMetadata Metadata
        {
            get
            {
                data ??= new TaskMetadata();
                return data;
            }
            set => data = CalculationItemMetadataMapper.CloneTaskMetadata(value);
        }

        public string Note
        {
            get => Metadata.Note;
            set => Metadata.Note = value ?? string.Empty;
        }

        public decimal? Quantity { get; set; }

        [MaxLength(25, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Unit { get; set; } = string.Empty;

        public decimal ChangeFactor1
        {
            get => Metadata.ChangeFactor1;
            set => Metadata.ChangeFactor1 = value;
        }

        public decimal ChangeFactor2
        {
            get => Metadata.ChangeFactor2;
            set => Metadata.ChangeFactor2 = value;
        }

        public decimal? Cap
        {
            get => Metadata.Cap;
            set => Metadata.Cap = value;
        }

        public bool IsActive
        {
            get => Metadata.IsActive;
            set => Metadata.IsActive = value;
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Code
        {
            get => Metadata.Code;
            set => Metadata.Code = value ?? string.Empty;
        }

        public TaskType Type
        {
            get => Metadata.Type;
            set => Metadata.Type = value;
        }

        public bool IsOH
        {
            get => Metadata.IsOH;
            set => Metadata.IsOH = value;
        }

        public decimal ActuallyQuantity
        {
            get => Metadata.ActuallyQuantity;
            set => Metadata.ActuallyQuantity = value;
        }

        public decimal WorkedQ
        {
            get => Metadata.WorkedQ;
            set => Metadata.WorkedQ = value;
        }

        public List<ResourcePostDTO> Resources { get; set; } = [];
        public List<TaskPostDTO> Tasks { get; set; } = [];
        [JsonIgnore] public bool Colspan { get; set; }
    }

    public class TaskStorageDTO : TaskBase
    {
        public int Id { get; set; }
        public int? TaskId { get; set; }

        private TaskMetadata? data = new();

        public TaskMetadata Data
        {
            get
            {
                data ??= new TaskMetadata();
                return data;
            }
            set { data = CalculationItemMetadataMapper.CloneTaskMetadata(value); }
        }

        public List<ResourceStorageListDTO> Resources { get; set; } = [];
        public List<TaskStorageDTO> Tasks { get; set; } = [];
        [JsonIgnore] public bool Colspan { get; set; }
    }

    public class TaskListDTO : TaskBase
    {
        private TaskMetadata? metadata = new();

        public TaskMetadata Metadata
        {
            get
            {
                metadata ??= new TaskMetadata();
                return metadata;
            }
            set => metadata = CalculationItemMetadataMapper.CloneTaskMetadata(value);
        }

        public int Id { get; set; }
        public int? TaskId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public decimal? Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;

        public int? StatusId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public string Opportunity { get; set; } = string.Empty;
        public int? OpportunityId { get; set; }

        // Production note – a standalone scalar field (not in Metadata), does not affect economy.
        public string? ProductionNote { get; set; }

        public List<ResourceListDTO> Resources { get; set; } = [];
    }
}
