using Microsoft.EntityFrameworkCore;
using ProjectImportHub.Entities.Questions.Assignments;
using ProjectImportHub.Entities.Questions.Conditions;
using ProjectImportHub.Entities.Questions.Groups;
using ProjectImportHub.Infrastructure;

namespace ProjectImportHub.Services.QuestionConditions;

public sealed class TaskConditionsService(IDbContextFactory<ProjectImportHubContext> _factory) : ITaskConditionsService
{
    // ===== تحميل الصفحة =====
    public async Task<List<ConditionDefinition>> GetConditionsAsync(int taskId, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Conditions
            .AsNoTracking().AsSplitQuery()
            .Where(c => c.TaskId == taskId)
            .Include(c => c.OptionRules)
            .Include(c => c.VariableRules)
            .Include(c => c.ResourceRules)
            .Include(c => c.NumericRules)
            .Include(c => c.Assignments).ThenInclude(a => a.Resource)
            .ToListAsync(ct);
    }

    public async Task<List<ResourceSelectorDefinition>> GetResourceGroupsAsync(int taskId, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.ResourceSelectors.AsNoTracking()
            .Where(g => g.TaskId == taskId)
            .Select(g => new ResourceSelectorDefinition
            {
                Id = g.Id,
                DisplayName = g.DisplayName,
                SortOrder = g.SortOrder,
                TaskId = g.TaskId,
                Items = g.Items.Select(o => new ResourceChoiceOptionDefinition
                {
                    Id = o.Id,
                    ResourceChoiceGroupId = o.ResourceChoiceGroupId,
                    ResourceId = o.ResourceId,
                    Resource = o.Resource
                }).ToList()
            })
            .ToListAsync(ct);
    }

    public async Task<List<QuestionGroupDefinition>> GetChoiceGroupsAsync(int taskId, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.QuestionGroups.AsNoTracking()
            .Where(g => g.TaskId == taskId)
            .Select(g => new QuestionGroupDefinition
            {
                Id = g.Id,
                DisplayName = g.DisplayName,
                SortOrder = g.SortOrder,
                TaskId = g.TaskId,
                Options = g.Options
                    .Select(o => new QuestionOptionDefinition
                    {
                        Id = o.Id,
                        DisplayName = o.DisplayName,
                        OptionGroupId = o.OptionGroupId
                    }).ToList()
            }).ToListAsync(ct);
    }

    public async Task<List<NumericQuestionDefinition>> GetNumericGroupsAsync(int taskId, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.NumericQuestions.AsNoTracking()
            .Where(ng => ng.TaskId == taskId)
            .Select(ng => new NumericQuestionDefinition
            {
                Id = ng.Id,
                DisplayName = ng.DisplayName,
                SortOrder = ng.SortOrder,
                TaskId = ng.TaskId,
                MinInputValue = ng.MinInputValue,
                MaxInputValue = ng.MaxInputValue
            }).ToListAsync(ct);
    }

    public async Task<Dictionary<int, List<OptionBindVM>>> GetOptionBindsByResourceAssignmentsAsync(int[] raIds, CancellationToken ct)
    {
        var result = raIds.Distinct().ToDictionary(id => id, _ => new List<OptionBindVM>());
        if (raIds.Length == 0) return result;

        await using var db = await _factory.CreateDbContextAsync(ct);
        var rows = await db.OptionResourceAssignments.AsNoTracking()
            .Where(b => raIds.Contains(b.ResourceAssignmentId))
            .Select(b => new
            {
                b.Id,
                b.ResourceAssignmentId,
                b.ChoiceOptionId,
                ChoiceGroupId = b.ChoiceOption.OptionGroupId,
                b.Formulas
            }).ToListAsync(ct);

        foreach (var b in rows)
            result[b.ResourceAssignmentId].Add(new OptionBindVM
            {
                Id = b.Id,
                ChoiceGroupId = b.ChoiceGroupId,
                ChoiceOptionId = b.ChoiceOptionId,
                Formulas = b.Formulas?.ToList() ?? []
            });

        return result;
    }

    public async Task<Dictionary<int, List<NumericBindVM>>> GetNumericBindsByResourceAssignmentsAsync(int[] raIds, CancellationToken ct)
    {
        var result = raIds.Distinct().ToDictionary(id => id, _ => new List<NumericBindVM>());
        if (raIds.Length == 0) return result;

        await using var db = await _factory.CreateDbContextAsync(ct);
        var rows = await db.NumericResourceAssignments.AsNoTracking()
            .Where(b => raIds.Contains(b.ResourceAssignmentId))
            .Select(b => new
            {
                b.Id,
                b.ResourceAssignmentId,
                b.NumericId,
                b.MinInputValue,
                b.MaxInputValue,
                b.Formulas
            }).ToListAsync(ct);

        foreach (var b in rows)
            result[b.ResourceAssignmentId].Add(new NumericBindVM
            {
                Id = b.Id,
                NumericId = b.NumericId,
                InputMinValue = b.MinInputValue,
                InputMaxValue = b.MaxInputValue,
                Formulas = b.Formulas?.ToList() ?? []
            });

        return result;
    }

    // ===== شرط واحد =====
    public async Task<ConditionDefinition?> GetConditionAsync(int id, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Conditions
            .AsSplitQuery()
            .Include(c => c.OptionRules)
            .Include(c => c.VariableRules)
            .Include(c => c.ResourceRules)
            .Include(c => c.NumericRules)
            .Include(c => c.Assignments).ThenInclude(a => a.Resource)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<int> UpsertConditionAsync(ConditionDefinition editing, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        //// لا تمرر Resource مرفقة عند الإضافة/التحديث
        //foreach (var ra in editing.Assignments)
        //    ra.Resource = null;

        if (editing.Id == 0)
        {
            db.Conditions.Add(editing);
            await db.SaveChangesAsync(ct);
            return editing.Id;
        }

        var dbCond = await db.Conditions
            .Include(c => c.OptionRules)
            .Include(c => c.ResourceRules)
            .Include(c => c.VariableRules)
            .Include(c => c.NumericRules)
            .Include(c => c.Assignments)
            .FirstAsync(c => c.Id == editing.Id, ct);

        dbCond.OptionResourceLogic = editing.OptionResourceLogic;
        dbCond.OptionNumericLogic = editing.OptionNumericLogic;
        dbCond.NumericResourceLogic = editing.NumericResourceLogic;

        SyncCollection(db, dbCond.OptionRules, editing.OptionRules, (x, y) =>
        {
            x.QuestionGroupId = y.QuestionGroupId;
            x.OptionId = y.OptionId;
            x.GroupKey = y.GroupKey;
        });

        SyncCollection(db, dbCond.ResourceRules, editing.ResourceRules, (x, y) =>
        {
            x.ResourceOptionGroupId = y.ResourceOptionGroupId;
            x.ResourceOptionItemId = y.ResourceOptionItemId;
            x.SetKey = y.SetKey;
        });

        SyncCollection(db, dbCond.NumericRules, editing.NumericRules, (x, y) =>
        {
            x.NumericInputId = y.NumericInputId;
            x.MinAllowedValue = y.MinAllowedValue;
            x.MaxAllowedValue = y.MaxAllowedValue;
            x.SetKey = y.SetKey;
        });

        SyncCollection(db, dbCond.VariableRules, editing.VariableRules, (x, y) =>
        {
            x.VariableName = y.VariableName;
            x.MinAllowedValue = y.MinAllowedValue;
            x.MaxAllowedValue = y.MaxAllowedValue;
            x.SetKey = y.SetKey;
        });

        // اختياري: مزامنة تعيين الموارد التابعة للشرط
        SyncCollection(db, dbCond.Assignments, editing.Assignments, (x, y) =>
        {
            x.ResourceId = y.ResourceId;
            x.ChangeFactor1 = y.ChangeFactor1;
            x.ChangeFactor2 = y.ChangeFactor2;
            x.CapWaste = y.CapWaste;
            x.BaseCost = y.BaseCost;
            x.IsActive = y.IsActive;
            x.MenuId = y.MenuId;
        });

        await db.SaveChangesAsync(ct);
        return dbCond.Id;
    }

    public async Task DeleteConditionAsync(int id, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var e = await db.Conditions.FindAsync([id], ct);
        if (e is null) return;
        db.Remove(e);
        await db.SaveChangesAsync(ct);
    }

    // ===== ربطات Option/Numeric =====
    public async Task<int> UpsertOptionBindingAsync(int raId, OptionBindVM vm, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        if (vm.Id == 0)
        {
            var entity = new OptionResourceAssignment
            {
                ChoiceOptionId = vm.ChoiceOptionId,
                ResourceAssignmentId = raId,
                Formulas = vm.Formulas.Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
            };
            db.OptionResourceAssignments.Add(entity);
            await db.SaveChangesAsync(ct);
            return entity.Id;
        }
        else
        {
            var entity = await db.OptionResourceAssignments.FirstAsync(x => x.Id == vm.Id, ct);
            entity.ChoiceOptionId = vm.ChoiceOptionId;
            entity.Formulas = [.. vm.Formulas.Where(s => !string.IsNullOrWhiteSpace(s))];
            await db.SaveChangesAsync(ct);
            return entity.Id;
        }
    }

    public async Task<int> UpsertNumericBindingAsync(int raId, NumericBindVM vm, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        if (vm.Id == 0)
        {
            var entity = new NumericResourceAssignment
            {
                NumericId = vm.NumericId,
                ResourceAssignmentId = raId,
                MinInputValue = vm.InputMinValue,
                MaxInputValue = vm.InputMaxValue,
                Formulas = vm.Formulas.Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
            };
            db.NumericResourceAssignments.Add(entity);
            await db.SaveChangesAsync(ct);
            return entity.Id;
        }
        else
        {
            var entity = await db.NumericResourceAssignments.FirstAsync(x => x.Id == vm.Id, ct);
            entity.NumericId = vm.NumericId;
            entity.MinInputValue = vm.InputMinValue;
            entity.MaxInputValue = vm.InputMaxValue;
            entity.Formulas = [.. vm.Formulas.Where(s => !string.IsNullOrWhiteSpace(s))];
            await db.SaveChangesAsync(ct);
            return entity.Id;
        }
    }

    // === Helper عام لمزامنة التجميعات (نفس منطقك) ===
    private static void SyncCollection<T>(
        DbContext db,
        ICollection<T> dbItems,
        ICollection<T> uiItems,
        Action<T, T> mapProps) where T : class
    {
        uiItems ??= Array.Empty<T>().ToList();
        int GetId(T e) => (int)e!.GetType().GetProperty("Id")!.GetValue(e)!;

        var dbById = dbItems.ToDictionary(GetId);
        var uiById = uiItems.Where(i => GetId(i) != 0).ToDictionary(GetId);

        foreach (var dbItem in dbItems.ToList())
        {
            var id = GetId(dbItem);
            if (id != 0 && !uiById.ContainsKey(id))
                db.Remove(dbItem);
        }

        foreach (var (id, uiItem) in uiById)
        {
            var dbItem = dbById[id];
            mapProps(dbItem, uiItem);
        }

        foreach (var newItem in uiItems.Where(i => GetId(i) == 0))
            dbItems.Add(newItem);
    }
}
