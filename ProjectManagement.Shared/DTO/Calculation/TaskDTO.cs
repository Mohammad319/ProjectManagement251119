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

        public int? TaskId { get; set; }
        TaskData data = new();
        public TaskData Data { get { data ??= new TaskData(); return data; } set { data = value; } }
        public List<ResourcePostDTO> Resources { get; set; }
        public List<TaskPostDTO> Tasks { get; set; }
        [JsonIgnore] public bool Colspan { get; set; }

        [Range(1, int.MaxValue, ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public int? StatusId { get; set; }
        public int? OpportunityId { get; set; }
        public bool OnlyCodeText = false;
    }
    public class TaskStorageDTO : TaskBase
    {
        public int Id { get; set; }
        public int? TaskId { get; set; }
        TaskData data = new();
        public TaskData Data { get { data ??= new TaskData(); return data; } set { data = value; } }
        public List<ResourceStorageListDTO> Resources { get; set; }
        public List<TaskStorageDTO> Tasks { get; set; }
        [JsonIgnore] public bool Colspan { get; set; }
    }

    public class TaskListDTO : TaskBase
    {
        public TaskData Data { get; set; } = new();
        public int Id { get; set; }
        public int? TaskId { get; set; }

        public int? StatusId { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
        public string Opportunity { get; set; }
        public int? OpportunityId { get; set; }
        public List<ResourceListDTO> Resources { get; set; }
    }
}
