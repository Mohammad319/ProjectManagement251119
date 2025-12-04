using Domain.Entities.Base;
using Domain.Entities.Folder;
using ProjectManagement.Shared.Base.Calculation;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class TaskStatusEntity : TaskStatusBase, IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
        public bool IsVisible { get; set; } = true;
        [JsonIgnore] public ICollection<TaskEntity> Tasks { get; set; }
    }
}
