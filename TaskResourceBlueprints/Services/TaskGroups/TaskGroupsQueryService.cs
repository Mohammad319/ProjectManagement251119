using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Entities.Questions.Conditions;
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

        var dbTask = await db.Tasks
            .Include(t => t.QuestionGroups).ThenInclude(g => g.Options)
            .Include(t => t.ResourceSelectors).ThenInclude(g => g.Items)
            .Include(t => t.NumericQuestions)
            .FirstOrDefaultAsync(t => t.Id == task.Id, ct);

        if (dbTask is null)
            throw new InvalidOperationException($"Task #{task.Id} not found.");

        task.QuestionGroups ??= [];
        task.ResourceSelectors ??= [];
        task.NumericQuestions ??= [];

        NormalizeSortOrders(task);

        await RemoveStaleConditionReferencesAsync(db, dbTask, task, ct);

        SyncQuestionGroups(dbTask, task);
        SyncResourceSelectors(dbTask, task);
        SyncNumericQuestions(dbTask, task);

        await db.SaveChangesAsync(ct);
    }

    public async Task<TaskDefinition?> GetTaskWithConditionsAsync(int id, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var task = await db.Tasks
            .AsNoTracking()
            .Include(t => t.QuestionGroups).ThenInclude(g => g.Options)
            .Include(t => t.ResourceSelectors).ThenInclude(g => g.Items).ThenInclude(i => i.Resource)
            .Include(t => t.NumericQuestions)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (task is null)
            return null;

        task.QuestionGroups = task.QuestionGroups
            .OrderBy(g => g.SortOrder)
            .ThenBy(g => g.Id)
            .ToList();

        foreach (var group in task.QuestionGroups)
        {
            group.Options = group.Options
                .OrderBy(o => o.SortOrder)
                .ThenBy(o => o.Id)
                .ToList();
        }

        task.ResourceSelectors = task.ResourceSelectors
            .OrderBy(g => g.SortOrder)
            .ThenBy(g => g.Id)
            .ToList();

        foreach (var selector in task.ResourceSelectors)
        {
            selector.Items = selector.Items
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Id)
                .ToList();
        }

        task.NumericQuestions = task.NumericQuestions
            .OrderBy(g => g.SortOrder)
            .ThenBy(g => g.Id)
            .ToList();

        return task;
    }

    private static void NormalizeSortOrders(TaskDefinition task)
    {
        Normalize(task.QuestionGroups, x => x.Item.SortOrder = x.Index);
        Normalize(task.ResourceSelectors, x => x.Item.SortOrder = x.Index);
        Normalize(task.NumericQuestions, x => x.Item.SortOrder = x.Index);

        foreach (var group in task.QuestionGroups)
        {
            Normalize(group.Options, x => x.Item.SortOrder = x.Index);
        }

        foreach (var selector in task.ResourceSelectors)
        {
            Normalize(selector.Items, x => x.Item.SortOrder = x.Index);
        }
    }

    private static void Normalize<T>(IList<T> items, Action<(T Item, int Index)> apply)
    {
        for (var i = 0; i < items.Count; i++)
        {
            apply((items[i], i));
        }
    }

    private static void SyncQuestionGroups(TaskDefinition dbTask, TaskDefinition incomingTask)
    {
        var incomingById = incomingTask.QuestionGroups
            .Where(x => x.Id != 0)
            .ToDictionary(x => x.Id);

        foreach (var existing in dbTask.QuestionGroups.ToList())
        {
            if (!incomingById.ContainsKey(existing.Id))
            {
                dbTask.QuestionGroups.Remove(existing);
            }
        }

        foreach (var incoming in incomingTask.QuestionGroups.OrderBy(x => x.SortOrder).ThenBy(x => x.Id))
        {
            if (incoming.Id == 0)
            {
                dbTask.QuestionGroups.Add(new QuestionGroupDefinition
                {
                    DisplayName = incoming.DisplayName,
                    SortOrder = incoming.SortOrder,
                    SelectionMode = incoming.SelectionMode,
                    SectionKey = incoming.SectionKey,
                    Options = incoming.Options
                        .OrderBy(x => x.SortOrder)
                        .ThenBy(x => x.Id)
                        .Select(o => new QuestionOptionDefinition
                        {
                            DisplayName = o.DisplayName,
                            SortOrder = o.SortOrder,
                            RevealedSectionKeys = o.RevealedSectionKeys?.ToList() ?? []
                        }).ToList()
                });

                continue;
            }

            var target = dbTask.QuestionGroups.First(x => x.Id == incoming.Id);
            target.DisplayName = incoming.DisplayName;
            target.SortOrder = incoming.SortOrder;
            target.SelectionMode = incoming.SelectionMode;
            target.SectionKey = incoming.SectionKey;

            SyncQuestionOptions(target, incoming);
        }
    }

    private static void SyncQuestionOptions(QuestionGroupDefinition targetGroup, QuestionGroupDefinition incomingGroup)
    {
        var incomingById = incomingGroup.Options
            .Where(x => x.Id != 0)
            .ToDictionary(x => x.Id);

        foreach (var existing in targetGroup.Options.ToList())
        {
            if (!incomingById.ContainsKey(existing.Id))
            {
                targetGroup.Options.Remove(existing);
            }
        }

        foreach (var incoming in incomingGroup.Options.OrderBy(x => x.SortOrder).ThenBy(x => x.Id))
        {
            if (incoming.Id == 0)
            {
                targetGroup.Options.Add(new QuestionOptionDefinition
                {
                    DisplayName = incoming.DisplayName,
                    SortOrder = incoming.SortOrder,
                    RevealedSectionKeys = incoming.RevealedSectionKeys?.ToList() ?? []
                });

                continue;
            }

            var target = targetGroup.Options.First(x => x.Id == incoming.Id);
            target.DisplayName = incoming.DisplayName;
            target.SortOrder = incoming.SortOrder;
            target.RevealedSectionKeys = incoming.RevealedSectionKeys?.ToList() ?? [];
        }
    }

    private static void SyncResourceSelectors(TaskDefinition dbTask, TaskDefinition incomingTask)
    {
        var incomingById = incomingTask.ResourceSelectors
            .Where(x => x.Id != 0)
            .ToDictionary(x => x.Id);

        foreach (var existing in dbTask.ResourceSelectors.ToList())
        {
            if (!incomingById.ContainsKey(existing.Id))
            {
                dbTask.ResourceSelectors.Remove(existing);
            }
        }

        foreach (var incoming in incomingTask.ResourceSelectors.OrderBy(x => x.SortOrder).ThenBy(x => x.Id))
        {
            if (incoming.Id == 0)
            {
                dbTask.ResourceSelectors.Add(new ResourceSelectorDefinition
                {
                    DisplayName = incoming.DisplayName,
                    SortOrder = incoming.SortOrder,
                    SectionKey = incoming.SectionKey,
                    Items = incoming.Items
                        .OrderBy(x => x.SortOrder)
                        .ThenBy(x => x.Id)
                        .Select(i => new ResourceOptionItem
                        {
                            SortOrder = i.SortOrder,
                            ResourceId = i.ResourceId
                        }).ToList()
                });

                continue;
            }

            var target = dbTask.ResourceSelectors.First(x => x.Id == incoming.Id);
            target.DisplayName = incoming.DisplayName;
            target.SortOrder = incoming.SortOrder;
            target.SectionKey = incoming.SectionKey;

            SyncSelectorItems(target, incoming);
        }
    }

    private static void SyncSelectorItems(ResourceSelectorDefinition targetSelector, ResourceSelectorDefinition incomingSelector)
    {
        var incomingById = incomingSelector.Items
            .Where(x => x.Id != 0)
            .ToDictionary(x => x.Id);

        foreach (var existing in targetSelector.Items.ToList())
        {
            if (!incomingById.ContainsKey(existing.Id))
            {
                targetSelector.Items.Remove(existing);
            }
        }

        foreach (var incoming in incomingSelector.Items.OrderBy(x => x.SortOrder).ThenBy(x => x.Id))
        {
            if (incoming.Id == 0)
            {
                targetSelector.Items.Add(new ResourceOptionItem
                {
                    SortOrder = incoming.SortOrder,
                    ResourceId = incoming.ResourceId
                });

                continue;
            }

            var target = targetSelector.Items.First(x => x.Id == incoming.Id);
            target.SortOrder = incoming.SortOrder;
            target.ResourceId = incoming.ResourceId;
        }
    }

    private static void SyncNumericQuestions(TaskDefinition dbTask, TaskDefinition incomingTask)
    {
        var incomingById = incomingTask.NumericQuestions
            .Where(x => x.Id != 0)
            .ToDictionary(x => x.Id);

        foreach (var existing in dbTask.NumericQuestions.ToList())
        {
            if (!incomingById.ContainsKey(existing.Id))
            {
                dbTask.NumericQuestions.Remove(existing);
            }
        }

        foreach (var incoming in incomingTask.NumericQuestions.OrderBy(x => x.SortOrder).ThenBy(x => x.Id))
        {
            if (incoming.Id == 0)
            {
                dbTask.NumericQuestions.Add(new NumericQuestionDefinition
                {
                    DisplayName = incoming.DisplayName,
                    SortOrder = incoming.SortOrder,
                    MinInputValue = incoming.MinInputValue,
                    MaxInputValue = incoming.MaxInputValue,
                    SectionKey = incoming.SectionKey
                });

                continue;
            }

            var target = dbTask.NumericQuestions.First(x => x.Id == incoming.Id);
            target.DisplayName = incoming.DisplayName;
            target.SortOrder = incoming.SortOrder;
            target.MinInputValue = incoming.MinInputValue;
            target.MaxInputValue = incoming.MaxInputValue;
            target.SectionKey = incoming.SectionKey;
        }
    }

    private static async Task RemoveStaleConditionReferencesAsync(
        TaskResourceBlueprintsContext db,
        TaskDefinition existingTask,
        TaskDefinition incomingTask,
        CancellationToken ct)
    {
        var removedQuestionGroupIds = existingTask.QuestionGroups
            .Where(x => incomingTask.QuestionGroups.All(y => y.Id != x.Id))
            .Select(x => x.Id)
            .ToHashSet();

        var removedOptionIds = existingTask.QuestionGroups
            .SelectMany(x => x.Options)
            .Where(x => incomingTask.QuestionGroups.SelectMany(y => y.Options).All(y => y.Id != x.Id))
            .Select(x => x.Id)
            .ToHashSet();

        var removedSelectorIds = existingTask.ResourceSelectors
            .Where(x => incomingTask.ResourceSelectors.All(y => y.Id != x.Id))
            .Select(x => x.Id)
            .ToHashSet();

        var removedSelectorItemIds = existingTask.ResourceSelectors
            .SelectMany(x => x.Items)
            .Where(x => incomingTask.ResourceSelectors.SelectMany(y => y.Items).All(y => y.Id != x.Id))
            .Select(x => x.Id)
            .ToHashSet();

        var removedNumericIds = existingTask.NumericQuestions
            .Where(x => incomingTask.NumericQuestions.All(y => y.Id != x.Id))
            .Select(x => x.Id)
            .ToHashSet();

        if (removedOptionIds.Count > 0)
        {
            var optionBindingRows = await db.OptionResourceAssignments
                .Where(x => removedOptionIds.Contains(x.OptionId))
                .ToListAsync(ct);

            if (optionBindingRows.Count > 0)
                db.OptionResourceAssignments.RemoveRange(optionBindingRows);
        }

        if (removedNumericIds.Count > 0)
        {
            var numericBindingRows = await db.NumericResourceAssignments
                .Where(x => removedNumericIds.Contains(x.NumericId))
                .ToListAsync(ct);

            if (numericBindingRows.Count > 0)
                db.NumericResourceAssignments.RemoveRange(numericBindingRows);
        }

        if (removedQuestionGroupIds.Count > 0 || removedOptionIds.Count > 0)
        {
            var optionRules = await db.OptionRequirements
                .Where(x => removedQuestionGroupIds.Contains(x.QuestionGroupId) || removedOptionIds.Contains(x.OptionId))
                .ToListAsync(ct);

            if (optionRules.Count > 0)
            {
                await ClearDefaultOptionRulesAsync(db, optionRules.Select(x => x.Id).ToList(), ct);
                db.OptionRequirements.RemoveRange(optionRules);
            }
        }

        if (removedSelectorIds.Count > 0 || removedSelectorItemIds.Count > 0)
        {
            var resourceRules = await db.ResourceRequirements
                .Where(x => removedSelectorIds.Contains(x.SelectorId) || removedSelectorItemIds.Contains(x.SelectorItemId))
                .ToListAsync(ct);

            if (resourceRules.Count > 0)
            {
                await ClearDefaultResourceRulesAsync(db, resourceRules.Select(x => x.Id).ToList(), ct);
                db.ResourceRequirements.RemoveRange(resourceRules);
            }
        }

        if (removedNumericIds.Count > 0)
        {
            var numericRules = await db.NumericRequirements
                .Where(x => removedNumericIds.Contains(x.NumericQuestionId))
                .ToListAsync(ct);

            if (numericRules.Count > 0)
                db.NumericRequirements.RemoveRange(numericRules);
        }
    }

    private static async Task ClearDefaultOptionRulesAsync(
        TaskResourceBlueprintsContext db,
        IReadOnlyCollection<int> removedRuleIds,
        CancellationToken ct)
    {
        var affected = await db.Conditions
            .Where(c => c.DefaultOptionRuleId != null && removedRuleIds.Contains(c.DefaultOptionRuleId.Value))
            .ToListAsync(ct);

        foreach (var condition in affected)
        {
            condition.DefaultOptionRuleId = null;
        }
    }

    private static async Task ClearDefaultResourceRulesAsync(
        TaskResourceBlueprintsContext db,
        IReadOnlyCollection<int> removedRuleIds,
        CancellationToken ct)
    {
        var affected = await db.Conditions
            .Where(c => c.DefaultResourceRuleId != null && removedRuleIds.Contains(c.DefaultResourceRuleId.Value))
            .ToListAsync(ct);

        foreach (var condition in affected)
        {
            condition.DefaultResourceRuleId = null;
        }
    }
}
