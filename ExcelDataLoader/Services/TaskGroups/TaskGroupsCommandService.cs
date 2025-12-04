using Microsoft.EntityFrameworkCore;
using ProjectImportHub.Entities.Questions.Groups;
using ProjectImportHub.Infrastructure;

namespace ProjectImportHub.Services.TaskGroups;

public interface ITaskGroupsCommandService
{
    Task SaveGroupsAsync(Entities.ProjectTaskEntity task, CancellationToken ct);
}
public sealed class TaskGroupsCommandService(IDbContextFactory<ProjectImportHubContext> _factory) : ITaskGroupsCommandService
{
    public async Task SaveGroupsAsync(Entities.ProjectTaskEntity task, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        // Snapshot بدون تتبّع لتحديد ما سيُحذف
        var snap = await db.Tasks
            .AsNoTracking()
            .Include(t => t.OptionGroups).ThenInclude(g => g.Options)
            .Include(t => t.ResourceOptionGroups).ThenInclude(g => g.Items)
            .Include(t => t.NumericInputs)
            .FirstOrDefaultAsync(t => t.Id == task.Id, ct);

        if (snap is null)
            throw new InvalidOperationException($"Task #{task.Id} not found.");

        // === ChoiceGroups ===
        var incomingCgIds = (task.OptionGroups ?? []).Select(x => x.Id).ToHashSet();
        var deleteCg = snap.OptionGroups.Where(x => !incomingCgIds.Contains(x.Id)).ToList();
        if (deleteCg.Count > 0) db.RemoveRange(deleteCg);

        foreach (var g in task.OptionGroups ?? Enumerable.Empty<OptionGroupEntity>())
        {
            g.TaskId = task.Id;
            db.Entry(g).State = (g.Id == 0) ? EntityState.Added : EntityState.Modified;

            var snapOpts = snap.OptionGroups.FirstOrDefault(x => x.Id == g.Id)?.Options ?? [];
            var incomingOptIds = (g.Options ?? []).Select(o => o.Id).ToHashSet();
            var deleteOpts = snapOpts.Where(o => !incomingOptIds.Contains(o.Id)).ToList();
            if (deleteOpts.Count > 0) db.RemoveRange(deleteOpts);

            foreach (var o in g.Options ?? Enumerable.Empty<OptionItemEntity>())
            {
                o.OptionGroupId = g.Id;
                db.Entry(o).State = (o.Id == 0) ? EntityState.Added : EntityState.Modified;
            }
        }

        // === ResourceOptionGroups ===
        var incomingRgIds = (task.ResourceOptionGroups ?? []).Select(x => x.Id).ToHashSet();
        var deleteRg = snap.ResourceOptionGroups.Where(x => !incomingRgIds.Contains(x.Id)).ToList();
        if (deleteRg.Count > 0) db.RemoveRange(deleteRg);

        foreach (var g in task.ResourceOptionGroups ?? Enumerable.Empty<ResourceOptionGroupEntity>())
        {
            g.TaskId = task.Id;
            db.Entry(g).State = (g.Id == 0) ? EntityState.Added : EntityState.Modified;

            var snapItems = snap.ResourceOptionGroups.FirstOrDefault(x => x.Id == g.Id)?.Items ?? new List<ResourceOptionItemEntity>();
            var incomingItemIds = (g.Items ?? []).Select(i => i.Id).ToHashSet();
            var deleteItems = snapItems.Where(i => !incomingItemIds.Contains(i.Id)).ToList();
            if (deleteItems.Count > 0) db.RemoveRange(deleteItems);

            foreach (var it in g.Items ?? Enumerable.Empty<ResourceOptionItemEntity>())
            {
                it.ResourceChoiceGroupId = g.Id;
                db.Entry(it).State = (it.Id == 0) ? EntityState.Added : EntityState.Modified;
            }
        }

        // === NumericInputs ===
        var incomingNgIds = (task.NumericInputs ?? []).Select(x => x.Id).ToHashSet();
        var deleteNg = snap.NumericInputs.Where(x => !incomingNgIds.Contains(x.Id)).ToList();
        if (deleteNg.Count > 0) db.RemoveRange(deleteNg);

        foreach (var g in task.NumericInputs ?? Enumerable.Empty<NumericInputEntity>())
        {
            g.TaskId = task.Id;
            db.Entry(g).State = (g.Id == 0) ? EntityState.Added : EntityState.Modified;
        }

        await db.SaveChangesAsync(ct);
    }
}
