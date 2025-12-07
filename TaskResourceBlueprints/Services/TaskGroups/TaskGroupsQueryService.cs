using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Entities.Questions.Groups;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.TaskGroups;

public interface ITaskGroupsQueryService
{
    Task SaveTaskConditionsAsync(TaskDefinition task, CancellationToken ct);
    Task<TaskDefinition?> GetTaskWithConditionsAsync(int taskId, CancellationToken ct);
}

public sealed class TaskGroupsQueryService(IDbContextFactory<TaskResourceBlueprintsContext> factory) : ITaskGroupsQueryService
{
    public async Task SaveTaskConditionsAsync(TaskDefinition task, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        // Snapshot بدون تتبّع لتحديد ما سيُحذف
        var snapshot = await db.Tasks
            .AsNoTracking()
            .Include(t => t.QuestionGroups).ThenInclude(g => g.Options)
            .Include(t => t.ResourceSelectors).ThenInclude(g => g.Items)
            .Include(t => t.NumericQuestions)
            .FirstOrDefaultAsync(t => t.Id == task.Id, ct);

        if (snapshot is null)
            throw new InvalidOperationException($"Task #{task.Id} not found.");

        var incomingQuestionGroups = task.QuestionGroups ?? new List<QuestionGroupDefinition>();
        var incomingResourceSelectors = task.ResourceSelectors ?? new List<ResourceSelectorDefinition>();
        var incomingNumericQuestions = task.NumericQuestions ?? new List<NumericQuestionDefinition>();

        // نحضّر قواميس للوصول السريع بدل FirstOrDefault المتكرر
        var snapshotQuestionGroupsById = snapshot.QuestionGroups
            .ToDictionary(x => x.Id);

        var snapshotResourceSelectorsById = snapshot.ResourceSelectors
            .ToDictionary(x => x.Id);

        // === ChoiceGroups ===
        var incomingQuestionGroupIds = incomingQuestionGroups
            .Select(x => x.Id)
            .ToHashSet();

        var questionGroupsToDelete = snapshot.QuestionGroups
            .Where(x => !incomingQuestionGroupIds.Contains(x.Id))
            .ToList();

        if (questionGroupsToDelete.Count > 0)
            db.RemoveRange(questionGroupsToDelete);

        foreach (var group in incomingQuestionGroups)
        {
            group.TaskId = task.Id;
            db.Entry(group).State = group.Id == 0
                ? EntityState.Added
                : EntityState.Modified;

            var snapshotOptions = snapshotQuestionGroupsById.TryGetValue(group.Id, out var existingGroup)
                ? (existingGroup.Options ?? new List<QuestionOptionDefinition>())
                : new List<QuestionOptionDefinition>();

            var incomingOptions = group.Options ?? new List<QuestionOptionDefinition>();
            var incomingOptionIds = incomingOptions.Select(o => o.Id).ToHashSet();

            var optionsToDelete = snapshotOptions
                .Where(o => !incomingOptionIds.Contains(o.Id))
                .ToList();

            if (optionsToDelete.Count > 0)
                db.RemoveRange(optionsToDelete);

            foreach (var option in incomingOptions)
            {
                option.QuestionGroupId = group.Id;
                db.Entry(option).State = option.Id == 0
                    ? EntityState.Added
                    : EntityState.Modified;
            }
        }

        // === ResourceSelectors ===
        var incomingSelectorIds = incomingResourceSelectors
            .Select(x => x.Id)
            .ToHashSet();

        var selectorsToDelete = snapshot.ResourceSelectors
            .Where(x => !incomingSelectorIds.Contains(x.Id))
            .ToList();

        if (selectorsToDelete.Count > 0)
            db.RemoveRange(selectorsToDelete);

        foreach (var selector in incomingResourceSelectors)
        {
            selector.TaskId = task.Id;
            db.Entry(selector).State = selector.Id == 0
                ? EntityState.Added
                : EntityState.Modified;

            var snapshotItems = snapshotResourceSelectorsById.TryGetValue(selector.Id, out var existingSelector)
                ? (existingSelector.Items ?? new List<ResourceOptionItem>())
                : new List<ResourceOptionItem>();

            var incomingItems = selector.Items ?? new List<ResourceOptionItem>();
            var incomingItemIds = incomingItems.Select(i => i.Id).ToHashSet();

            var itemsToDelete = snapshotItems
                .Where(i => !incomingItemIds.Contains(i.Id))
                .ToList();

            if (itemsToDelete.Count > 0)
                db.RemoveRange(itemsToDelete);

            foreach (var item in incomingItems)
            {
                item.SelectorId = selector.Id;
                db.Entry(item).State = item.Id == 0
                    ? EntityState.Added
                    : EntityState.Modified;
            }
        }

        // === NumericQuestions ===
        var incomingNumericQuestionIds = incomingNumericQuestions
            .Select(x => x.Id)
            .ToHashSet();

        var numericQuestionsToDelete = snapshot.NumericQuestions
            .Where(x => !incomingNumericQuestionIds.Contains(x.Id))
            .ToList();

        if (numericQuestionsToDelete.Count > 0)
            db.RemoveRange(numericQuestionsToDelete);

        foreach (var numericQuestion in incomingNumericQuestions)
        {
            numericQuestion.TaskId = task.Id;
            db.Entry(numericQuestion).State = numericQuestion.Id == 0
                ? EntityState.Added
                : EntityState.Modified;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<TaskDefinition?> GetTaskWithConditionsAsync(int id, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db.Tasks
            .AsNoTracking()
            .Include(t => t.QuestionGroups).ThenInclude(g => g.Options)
            .Include(t => t.ResourceSelectors).ThenInclude(g => g.Items).ThenInclude(i => i.Resource)
            .Include(t => t.NumericQuestions)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }
}
