using TaskResourceBlueprints.Entities.Tasks;

namespace TaskResourceBlueprints.Entities.Lookups;

public class TaskState : TaskLookupBase
{
    public int TaskStateGroupId { get; set; }
    public TaskStateGroup? Group { get; set; }
    public List<TaskDefinitionStateLink> TaskLinks { get; set; } = [];
}
