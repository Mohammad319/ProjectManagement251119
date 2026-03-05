using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.DTO.Calculation
{
    public class TaskPostDTO : TaskBase
    {
        public int Id { get; set; }

        public int? ParentTaskId { get; set; }

        [Range(1, int.MaxValue, ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
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
            get { data ??= new TaskMetadata(); return data; }
            set { data = value ?? new TaskMetadata(); }
        }

        public string Note
        {
            get => Metadata.Note;
            set => Metadata.Note = value ?? string.Empty;
        }

        public decimal? Quantity
        {
            get => Metadata.Quantity;
            set => Metadata.Quantity = value;
        }

        [MaxLength(25, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Unit
        {
            get => Metadata.Unit;
            set => Metadata.Unit = value ?? string.Empty;
        }

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

        [Range(-20, 20, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
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
            get { data ??= new TaskMetadata(); return data; }
            set { data = value ?? new TaskMetadata(); }
        }

        public List<ResourceStorageListDTO> Resources { get; set; } = [];
        public List<TaskStorageDTO> Tasks { get; set; } = [];
        [JsonIgnore] public bool Colspan { get; set; }
    }

    public class TaskListDTO : TaskBase
    {
        public TaskMetadata Metadata { get; set; } = new();
        public int Id { get; set; }
        public int? TaskId { get; set; }

        public int? StatusId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public string Opportunity { get; set; } = string.Empty;
        public int? OpportunityId { get; set; }

        public List<ResourceListDTO> Resources { get; set; } = [];
    }
}
