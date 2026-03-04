using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using System;
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

        public string Note { get; set; } = string.Empty;
        public decimal? Quantity { get; set; }
        [MaxLength(25, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Unit { get; set; } = string.Empty;
        public decimal ChangeFactor1 { get; set; } = 1;
        public decimal ChangeFactor2 { get; set; } = 1;
        [Range(-20, 20, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public decimal? Cap { get; set; }
        public bool IsActive { get; set; } = true;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Code { get; set; } = string.Empty;
        public TaskType Type { get; set; }
        public bool IsOH { get; set; }

        TaskMetadata data = new();
        public TaskMetadata Metadata { get { data ??= new TaskMetadata(); return data; } set { data = value; } }
        public List<ResourcePostDTO> Resources { get; set; } = [];
        public List<TaskPostDTO> Tasks { get; set; } = [];
        [JsonIgnore] public bool Colspan { get; set; }

        public decimal ActuallyQuantity { get; set; } = 0;
        public double WorkedQ { get; set; } = 0;

    }
    public class TaskStorageDTO : TaskBase
    {
        public int Id { get; set; }
        public int? TaskId { get; set; }
        TaskMetadata data = new();
        public TaskMetadata Data { get { data ??= new TaskMetadata(); return data; } set { data = value; } }
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
