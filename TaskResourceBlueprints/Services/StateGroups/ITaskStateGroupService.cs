using TaskResourceBlueprints.Entities.Lookups;

namespace TaskResourceBlueprints.Services.StateGroups;

public interface ITaskStateGroupService
{
    Task<List<TaskStateGroup>> GetGroupsWithStatesAsync(CancellationToken ct = default);
    Task<TaskStateGroup> AddGroupAsync(string name, int sortOrder, CancellationToken ct = default);
    Task<bool> UpdateGroupAsync(TaskStateGroup group, CancellationToken ct = default);
    Task<bool> DeleteGroupAsync(int id, CancellationToken ct = default);
    Task<TaskState> AddStateAsync(int groupId, string name, int sortOrder, CancellationToken ct = default);
    Task<bool> UpdateStateAsync(TaskState state, CancellationToken ct = default);
    Task<bool> DeleteStateAsync(int id, CancellationToken ct = default);
}
