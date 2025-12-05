using Microsoft.EntityFrameworkCore;
using ProjectImportHub.Entities.Questions.Groups;
using ProjectImportHub.Entities.Tasks;
using ProjectImportHub.Infrastructure;

namespace ProjectImportHub.Services.TaskGroups;

public interface ITaskGroupsCommandService
{
    Task SaveGroupsAsync(TaskDefinition task, CancellationToken ct);
}
public sealed class TaskGroupsCommandService(IDbContextFactory<ProjectImportHubContext> _factory) : ITaskGroupsCommandService
{
    public async Task SaveGroupsAsync(TaskDefinition task, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        // Snapshot بدون تتبّع لتحديد ما سيُحذف
        var snap = await db.Tasks
            .AsNoTracking()
            .Include(t => t.QuestionGroups).ThenInclude(g => g.Options)
            .Include(t => t.ResourceSelectors).ThenInclude(g => g.Items)
            .Include(t => t.NumericQuestions)
            .FirstOrDefaultAsync(t => t.Id == task.Id, ct);

        if (snap is null)
            throw new InvalidOperationException($"Task #{task.Id} not found.");

        // === ChoiceGroups ===
        var incomingCgIds = (task.QuestionGroups ?? []).Select(x => x.Id).ToHashSet();
        var deleteCg = snap.QuestionGroups.Where(x => !incomingCgIds.Contains(x.Id)).ToList();
        if (deleteCg.Count > 0) db.RemoveRange(deleteCg);

        foreach (var g in task.QuestionGroups ?? Enumerable.Empty<QuestionGroupDefinition>())
        {
            g.TaskId = task.Id;
            db.Entry(g).State = (g.Id == 0) ? EntityState.Added : EntityState.Modified;

            var snapOpts = snap.QuestionGroups.FirstOrDefault(x => x.Id == g.Id)?.Options ?? [];
            var incomingOptIds = (g.Options ?? []).Select(o => o.Id).ToHashSet();
            var deleteOpts = snapOpts.Where(o => !incomingOptIds.Contains(o.Id)).ToList();
            if (deleteOpts.Count > 0) db.RemoveRange(deleteOpts);

            foreach (var o in g.Options ?? Enumerable.Empty<QuestionOptionDefinition>())
            {
                o.OptionGroupId = g.Id;
                db.Entry(o).State = (o.Id == 0) ? EntityState.Added : EntityState.Modified;
            }
        }

        // === ResourceSelectors ===
        var incomingRgIds = (task.ResourceSelectors ?? []).Select(x => x.Id).ToHashSet();
        var deleteRg = snap.ResourceSelectors.Where(x => !incomingRgIds.Contains(x.Id)).ToList();
        if (deleteRg.Count > 0) db.RemoveRange(deleteRg);

        foreach (var g in task.ResourceSelectors ?? Enumerable.Empty<ResourceSelectorDefinition>())
        {
            g.TaskId = task.Id;
            db.Entry(g).State = (g.Id == 0) ? EntityState.Added : EntityState.Modified;

            var snapItems = snap.ResourceSelectors.FirstOrDefault(x => x.Id == g.Id)?.Items ?? new List<ResourceChoiceOptionDefinition>();
            var incomingItemIds = (g.Items ?? []).Select(i => i.Id).ToHashSet();
            var deleteItems = snapItems.Where(i => !incomingItemIds.Contains(i.Id)).ToList();
            if (deleteItems.Count > 0) db.RemoveRange(deleteItems);

            foreach (var it in g.Items ?? Enumerable.Empty<ResourceChoiceOptionDefinition>())
            {
                it.ResourceChoiceGroupId = g.Id;
                db.Entry(it).State = (it.Id == 0) ? EntityState.Added : EntityState.Modified;
            }
        }

        // === NumericQuestions ===
        var incomingNgIds = (task.NumericQuestions ?? []).Select(x => x.Id).ToHashSet();
        var deleteNg = snap.NumericQuestions.Where(x => !incomingNgIds.Contains(x.Id)).ToList();
        if (deleteNg.Count > 0) db.RemoveRange(deleteNg);

        foreach (var g in task.NumericQuestions ?? Enumerable.Empty<NumericQuestionDefinition>())
        {
            g.TaskId = task.Id;
            db.Entry(g).State = (g.Id == 0) ? EntityState.Added : EntityState.Modified;
        }

        await db.SaveChangesAsync(ct);
    }
}
