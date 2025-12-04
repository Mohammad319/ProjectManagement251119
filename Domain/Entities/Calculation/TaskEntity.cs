using Domain.Entities.Base;
using Domain.Entities.Folder;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class TaskEntity : TaskBase, IDataKeyFilterReadOnly
    {
        [JsonIgnore] public int TenantId { get; set; }

        TaskData data = new();
        public TaskData Data { get { data ??= new TaskData(); return data; } set { data = value; } }
        [Key] public int Id { get; set; }
        public int? TaskId { get; set; }
        [ForeignKey(nameof(TaskId))]
        [JsonIgnore] public TaskEntity Task { get; set; }
        public List<TaskEntity> Tasks { get; set; }
        [JsonIgnore] public int? OpportunityId { get; set; }
        [JsonIgnore] public OpportunityEntity Opportunity { get; set; }
        [JsonIgnore]public CalculationEntity Calculation { get; set; }
        [JsonIgnore] public int CalculationId { get; set; }
        public int? StatusId { get; set; }
        [JsonIgnore] public TaskStatusEntity Status { get; set; }
        public List<ResourceEntity> Resources { get; set; }
    }
}
