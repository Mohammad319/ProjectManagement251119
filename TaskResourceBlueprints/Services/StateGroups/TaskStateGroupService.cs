using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Entities.Lookups;
using TaskResourceBlueprints.Infrastructure;
using TaskResourceBlueprints.Services.Common;

namespace TaskResourceBlueprints.Services.StateGroups;

public sealed class TaskStateGroupService(IDbContextFactory<TaskResourceBlueprintsContext> factory) : ITaskStateGroupService
{
    public async Task<List<TaskStateGroup>> GetGroupsWithStatesAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.TaskStateGroups
            .AsNoTracking()
            .Include(g => g.States.OrderBy(s => s.SortOrder).ThenBy(s => s.Name))
            .OrderBy(g => g.SortOrder)
            .ThenBy(g => g.Name)
            .ToListAsync(ct);
    }

    public async Task<TaskStateGroup> AddGroupAsync(string name, int sortOrder, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var group = new TaskStateGroup { Name = name.Trim(), SortOrder = sortOrder, IsVisible = true };
        db.TaskStateGroups.Add(group);
        await db.SaveChangesAsync(ct);
        return group;
    }

    public async Task<bool> UpdateGroupAsync(TaskStateGroup group, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        db.TaskStateGroups.Update(group);
        return await db.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> DeleteGroupAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var group = await db.TaskStateGroups.FindAsync([id], ct);
        if (group is null) return false;
        db.TaskStateGroups.Remove(group);
        return await db.SaveChangesAsync(ct) > 0;
    }

    public async Task<TaskState> AddStateAsync(int groupId, string name, int sortOrder, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var state = new TaskState { TaskStateGroupId = groupId, Name = name.Trim(), SortOrder = sortOrder, IsVisible = true };
        db.TaskStates.Add(state);
        await db.SaveChangesAsync(ct);
        return state;
    }

    public async Task<bool> UpdateStateAsync(TaskState state, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        db.TaskStates.Update(state);
        return await db.SaveChangesAsync(ct) > 0;
    }

    public async Task<bool> DeleteStateAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var state = await db.TaskStates.FindAsync([id], ct);
        if (state is null) return false;
        db.TaskStates.Remove(state);
        return await db.SaveChangesAsync(ct) > 0;
    }
}
