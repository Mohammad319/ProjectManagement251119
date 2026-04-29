using ProjectManagement.Shared.Base.Calculation;
using TaskResourceBlueprints.Entities;

namespace TaskResourceBlueprints.Entities.Tasks;

public class TaskDefinitionResourceLink
{
    public int Id { get; set; }
    public int TaskDefinitionId { get; set; }
    public int ResourceDefinitionId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public bool IsFixed { get; set; }
    public List<ResourceParameter> Parameters { get; set; } = [];
    public List<ResourceAddon> AddOns { get; set; } = [];
    public List<ResourceTime> Times { get; set; } = [];
    public TaskDefinition? Task { get; set; }
    public ResourceDefinition? Resource { get; set; }
}
