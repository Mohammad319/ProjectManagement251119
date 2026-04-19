using Domain.Entities.Base;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class TaskStatusEntity : ColoredListEntity
    {
        [JsonIgnore]
        public ICollection<TaskEntity> Tasks { get; private set; } = [];

        public TaskStatusEntity() { }

        public TaskStatusEntity(string name, string color, int sortOrder, bool isVisible = true)
            : base(name, color, sortOrder, isVisible) { }
    }
}
