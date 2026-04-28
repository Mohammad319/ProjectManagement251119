using TaskResourceBlueprints.Entities.Tasks;

namespace TaskResourceBlueprints.Entities.Lookups;

public class TaskStateGroup : TaskLookupBase
{
    public List<TaskState> States { get; set; } = [];
}
