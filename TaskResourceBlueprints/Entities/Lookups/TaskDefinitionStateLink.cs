using TaskResourceBlueprints.Entities.Tasks;

namespace TaskResourceBlueprints.Entities.Lookups;

public class TaskDefinitionStateLink
{
    public int Id { get; set; }
    public int TaskDefinitionId { get; set; }
    public int TaskStateId { get; set; }
    public TaskDefinition? Task { get; set; }
    public TaskState? State { get; set; }
}
