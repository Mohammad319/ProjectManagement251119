using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Enums;
using System.Globalization;
using System.Text;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Entities.Lookups;
using TaskResourceBlueprints.Entities.Resources;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.Import;

public interface ITaskResourceCsvImportService
{
    Task<TaskResourceCsvImportResult> ImportAsync(
        Stream csvStream,
        string fileName,
        bool deleteExistingData = false,
        CancellationToken ct = default);
}

public sealed class TaskResourceCsvImportResult
{
    public int TotalRows { get; set; }
    public int TaskRows { get; set; }
    public int ResourceRows { get; set; }
    public int ParameterRows { get; set; }
    public int CreatedTasks { get; set; }
    public int UpdatedTasks { get; set; }
    public int CreatedResources { get; set; }
    public int UpdatedResources { get; set; }
    public int CreatedFolders { get; set; }
    public int CreatedAssignments { get; set; }
    public int UpdatedAssignments { get; set; }
    public int CreatedStateGroups { get; set; }
    public int CreatedStates { get; set; }
    public int CreatedStateLinks { get; set; }
    public int CreatedTaskConversionParams { get; set; }
    public int CreatedResourceParameters { get; set; }
    public int CreatedResourceAddOns { get; set; }
    public int CreatedResourceTimes { get; set; }
    public bool DeletedExistingData { get; set; }
    public int SkippedRows { get; set; }
    public List<TaskResourceCsvImportIssue> Issues { get; set; } = [];
}

public sealed record TaskResourceCsvImportIssue(int RowNumber, string Message);

public sealed class TaskResourceCsvImportService(IDbContextFactory<TaskResourceBlueprintsContext> dbContextFactory)
    : ITaskResourceCsvImportService
{
    private const char Delimiter = ';';
    private const int FallbackCodeIndex = 4;
    private const int FallbackNameIndex = 5;
    private const int FallbackQuantityIndex = 6;
    private const int FallbackUnitIndex = 7;

    public async Task<TaskResourceCsvImportResult> ImportAsync(
        Stream csvStream,
        string fileName,
        bool deleteExistingData = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(csvStream);

        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        var headerLine = await reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(headerLine))
            throw new InvalidOperationException("CSV file is empty.");

        var header = ParseCsvLine(headerLine);
        var map = BuildColumnMap(header);
        var result = new TaskResourceCsvImportResult
        {
            DeletedExistingData = deleteExistingData
        };

        if (deleteExistingData)
            await DeleteExistingImportDataAsync(db, ct);

        var taskByExternalId = new Dictionary<string, TaskDefinition>(StringComparer.OrdinalIgnoreCase);
        var taskByCode = new Dictionary<string, TaskDefinition>(StringComparer.OrdinalIgnoreCase);
        var taskByImportKey = new Dictionary<string, TaskDefinition>(StringComparer.OrdinalIgnoreCase);
        var linksByRowId = new Dictionary<string, TaskDefinitionResourceLink>(StringComparer.OrdinalIgnoreCase);
        var foldersByPath = await LoadFoldersAsync(db, ct);
        var resourcesByKey = await LoadResourcesAsync(db, ct);
        var existingLinkKeys = await LoadExistingLinkKeysAsync(db, ct);
        var stateGroupsByName = await LoadStateGroupsAsync(db, ct);
        var statesByKey = await LoadStatesAsync(db, ct);
        var nextTaskSortOrder = await db.Tasks.Select(x => (int?)x.SortOrder).MaxAsync(ct) ?? 0;
        var nextResourceSortOrder = await db.Resources.Select(x => (int?)x.SortOrder).MaxAsync(ct) ?? 0;
        var nextFolderSortOrderByParent = await db.ResourceCategories
            .GroupBy(x => x.ParentCategoryId)
            .Select(x => new { ParentId = x.Key, MaxSortOrder = x.Max(f => f.SortOrder) })
            .ToDictionaryAsync(x => ParentSortKey(x.ParentId), x => x.MaxSortOrder, ct);

        await LoadTaskDictionariesAsync(db, taskByCode, taskByImportKey, ct);

        var rowNumber = 1;
        TaskDefinition? currentTask = null;
        TaskDefinition? currentCodeContextTask = null;
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            ct.ThrowIfCancellationRequested();

            rowNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            result.TotalRows++;
            var row = ParseCsvLine(line);
            var rowType = Get(row, map, "Radtyp").Trim();

            try
            {
                if (rowType.Equals("T", StringComparison.OrdinalIgnoreCase))
                {
                    result.TaskRows++;
                    currentTask = await UpsertTaskAsync(
                        db, row, map, fileName, rowNumber, result,
                        taskByExternalId, taskByCode, taskByImportKey,
                        stateGroupsByName, statesByKey,
                        ++nextTaskSortOrder, currentCodeContextTask, ct);

                    if (!string.IsNullOrWhiteSpace(currentTask.Code))
                        currentCodeContextTask = currentTask;
                }
                else if (rowType.Equals("R", StringComparison.OrdinalIgnoreCase))
                {
                    result.ResourceRows++;
                    var (created, rowId, link) = await UpsertResourceAssignmentAsync(
                        db, row, map, rowNumber, result,
                        taskByExternalId, taskByCode, currentTask,
                        foldersByPath, resourcesByKey, existingLinkKeys,
                        nextFolderSortOrderByParent, ++nextResourceSortOrder,
                        fileName, ct);

                    if (!created)
                        nextResourceSortOrder--;

                    if (!string.IsNullOrWhiteSpace(rowId))
                        linksByRowId[rowId] = link;
                }
                else if (rowType.Equals("P", StringComparison.OrdinalIgnoreCase))
                {
                    result.ParameterRows++;
                    await ApplyParameterRowAsync(
                        db, row, map, rowNumber, result,
                        taskByExternalId, currentTask, linksByRowId, ct);
                }
                else
                {
                    result.SkippedRows++;
                    result.Issues.Add(new(rowNumber, $"Unknown Radtyp '{rowType}'."));
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                result.SkippedRows++;
                result.Issues.Add(new(rowNumber, ex.Message));
            }
        }

        return result;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Loaders
    // ──────────────────────────────────────────────────────────────────────

    private static async Task LoadTaskDictionariesAsync(
        TaskResourceBlueprintsContext db,
        Dictionary<string, TaskDefinition> taskByCode,
        Dictionary<string, TaskDefinition> taskByImportKey,
        CancellationToken ct)
    {
        var tasks = await db.Tasks
            .Include(t => t.StateLinks)
                .ThenInclude(l => l.State)
                    .ThenInclude(s => s!.Group)
            .ToListAsync(ct);

        foreach (var task in tasks)
        {
            if (!string.IsNullOrWhiteSpace(task.Code))
                taskByCode[task.Code.Trim()] = task;

            var statePairs = task.StateLinks
                .Where(l => l.State?.Group is not null)
                .Select(l => (Property: l.State!.Group!.Name, Value: l.State.Name));

            var importKey = TaskImportKey(task.Code, task.Name, statePairs);
            if (!string.IsNullOrWhiteSpace(importKey) && !taskByImportKey.ContainsKey(importKey))
                taskByImportKey[importKey] = task;
        }
    }

    private static async Task<Dictionary<string, ResourceCategory>> LoadFoldersAsync(
        TaskResourceBlueprintsContext db, CancellationToken ct)
    {
        var folders = await db.ResourceCategories.ToListAsync(ct);
        var byId = folders.ToDictionary(x => x.Id);
        var byPath = new Dictionary<string, ResourceCategory>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in folders)
            byPath[BuildFolderPath(folder, byId)] = folder;
        return byPath;
    }

    private static async Task<HashSet<string>> LoadExistingLinkKeysAsync(
        TaskResourceBlueprintsContext db, CancellationToken ct)
    {
        var keys = await db.TaskDefinitionResourceLinks.AsNoTracking()
            .Select(l => AssignmentKey(l.TaskDefinitionId, l.ResourceDefinitionId))
            .ToListAsync(ct);
        return [.. keys];
    }

    private static async Task<Dictionary<string, ResourceDefinition>> LoadResourcesAsync(
        TaskResourceBlueprintsContext db, CancellationToken ct)
    {
        var resources = await db.Resources.ToListAsync(ct);
        return resources
            .GroupBy(x => ResourceKey(x.Name, x.FolderId, x.ResType), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<Dictionary<string, TaskStateGroup>> LoadStateGroupsAsync(
        TaskResourceBlueprintsContext db, CancellationToken ct)
    {
        var groups = await db.TaskStateGroups.ToListAsync(ct);
        return groups
            .GroupBy(x => NormalizeToken(x.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<Dictionary<string, TaskState>> LoadStatesAsync(
        TaskResourceBlueprintsContext db, CancellationToken ct)
    {
        var states = await db.TaskStates.ToListAsync(ct);
        return states
            .GroupBy(x => StateKey(x.TaskStateGroupId, x.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Delete
    // ──────────────────────────────────────────────────────────────────────

    private static async Task DeleteExistingImportDataAsync(TaskResourceBlueprintsContext db, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        await db.ResourceTenantLinks.ExecuteDeleteAsync(ct);
        await db.TaskDefinitionStateLinks.ExecuteDeleteAsync(ct);
        await db.TaskDefinitionResourceLinks.ExecuteDeleteAsync(ct);
        await db.Tasks.ExecuteDeleteAsync(ct);
        await db.Resources.ExecuteDeleteAsync(ct);
        await db.ResourceCategories.ExecuteUpdateAsync(
            setters => setters.SetProperty(x => x.ParentCategoryId, (int?)null), ct);
        await db.ResourceCategories.ExecuteDeleteAsync(ct);
        await db.TaskStates.ExecuteDeleteAsync(ct);
        await db.TaskStateGroups.ExecuteDeleteAsync(ct);

        await transaction.CommitAsync(ct);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Upsert task
    // ──────────────────────────────────────────────────────────────────────

    private static async Task<TaskDefinition> UpsertTaskAsync(
        TaskResourceBlueprintsContext db,
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> map,
        string fileName,
        int rowNumber,
        TaskResourceCsvImportResult result,
        Dictionary<string, TaskDefinition> taskByExternalId,
        Dictionary<string, TaskDefinition> taskByCode,
        Dictionary<string, TaskDefinition> taskByImportKey,
        Dictionary<string, TaskStateGroup> stateGroupsByName,
        Dictionary<string, TaskState> statesByKey,
        int sortOrder,
        TaskDefinition? currentCodeContextTask,
        CancellationToken ct)
    {
        var externalId = Get(row, map, "TaskId").Trim();
        var code = Get(row, map, "Code", fallbackIndex: FallbackCodeIndex).Trim();
        var name = Get(row, map, "Name", fallbackIndex: FallbackNameIndex).Trim();
        var parentTaskId = Get(row, map, "ParentTaskId").Trim();
        var parentCode = EmptyToNull(Get(row, map, "ParentCode"));
        if (string.IsNullOrWhiteSpace(code) &&
            string.IsNullOrWhiteSpace(parentCode) &&
            !string.IsNullOrWhiteSpace(currentCodeContextTask?.Code))
        {
            parentCode = currentCodeContextTask.Code;
        }

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Task name is required.");

        var statePairs = new[]
        {
            (Property: Get(row, map, "Property1").Trim(), Value: Get(row, map, "Value1").Trim()),
            (Property: Get(row, map, "Property2").Trim(), Value: Get(row, map, "Value2").Trim()),
        };
        var importKey = TaskImportKey(code, name, statePairs);

        TaskDefinition? task = null;
        if (!string.IsNullOrWhiteSpace(externalId))
            taskByExternalId.TryGetValue(externalId, out task);
        if (!string.IsNullOrWhiteSpace(importKey))
            task ??= taskByImportKey.TryGetValue(importKey, out var byImportKey) ? byImportKey : null;

        if (task is null)
        {
            task = new TaskDefinition
            {
                SortOrder = sortOrder,
                Status = TaskStatusEnum.Ready,
                IsActive = true,
                IsVisible = true,
            };
            db.Tasks.Add(task);
            result.CreatedTasks++;
        }
        else
        {
            result.UpdatedTasks++;
        }

        task.Code = string.IsNullOrWhiteSpace(code) ? task.Code : code;
        task.Name = name;
        ApplyTaskHierarchy(task, parentTaskId, parentCode, taskByExternalId, taskByCode);
        task.Quantity = ParseNullableDecimal(Get(row, map, "Quantity", fallbackIndex: FallbackQuantityIndex));
        task.UnitCode = EmptyToNull(Get(row, map, "Unit", fallbackIndex: FallbackUnitIndex));
        var nameSynonyms = ReadSynonyms(row, map, "TaskNameSynonym1", "TaskNameSynonym2");
        if (nameSynonyms.Count > 0)
            task.NameSynonyms = nameSynonyms;

        var unitSynonyms = ReadSynonyms(row, map, "TaskUnitSynonym1", "TaskUnitSynonym2");
        if (unitSynonyms.Count > 0)
            task.UnitSynonyms = unitSynonyms;
        task.FieldNotes = EmptyToNull(Get(row, map, "CalculationMethod"));
        task.AdminNote = BuildAdminNote(fileName, account: null, category: Get(row, map, "Category"), rowNumber);
        task.ChangeFactor1 = 1m;
        task.ChangeFactor2 = 1m;
        task.ConversionParameters = [];
        task.RefreshNormalizedTextSv();

        if (!string.IsNullOrWhiteSpace(externalId))
            taskByExternalId[externalId] = task;
        if (!string.IsNullOrWhiteSpace(importKey))
            taskByImportKey[importKey] = task;
        if (!string.IsNullOrWhiteSpace(task.Code))
            taskByCode[task.Code.Trim()] = task;

        await db.SaveChangesAsync(ct);

        // ── State group links ──────────────────────────────────────────────
        // Load current state links for this task (keyed by groupId for fast lookup)
        var currentLinks = await db.TaskDefinitionStateLinks
            .Where(l => l.TaskDefinitionId == task.Id)
            .Include(l => l.State)
            .ToListAsync(ct);
        var linkByGroupId = currentLinks
            .Where(l => l.State is not null)
            .ToDictionary(l => l.State!.TaskStateGroupId);

        foreach (var (property, value) in statePairs)
        {
            if (string.IsNullOrWhiteSpace(property) || string.IsNullOrWhiteSpace(value))
                continue;

            var group = await EnsureStateGroupAsync(db, property, stateGroupsByName, result, ct);
            var state = await EnsureStateAsync(db, value, group, statesByKey, result, ct);

            if (linkByGroupId.TryGetValue(group.Id, out var existingLink))
            {
                // Update to new state if different
                if (existingLink.TaskStateId != state.Id)
                    existingLink.TaskStateId = state.Id;
            }
            else
            {
                db.TaskDefinitionStateLinks.Add(new TaskDefinitionStateLink
                {
                    TaskDefinitionId = task.Id,
                    TaskStateId = state.Id
                });
                result.CreatedStateLinks++;
            }
        }

        await db.SaveChangesAsync(ct);
        return task;
    }

    private static void ApplyTaskHierarchy(
        TaskDefinition task,
        string parentTaskId,
        string? parentCode,
        IReadOnlyDictionary<string, TaskDefinition> taskByExternalId,
        IReadOnlyDictionary<string, TaskDefinition> taskByCode)
    {
        var parent = ResolveParentTask(parentTaskId, parentCode, task.Code, taskByExternalId, taskByCode);
        if (parent is null)
        {
            task.ParentCode = parentCode;
            task.ParentName = null;
            task.HierarchyPath = BuildHierarchyPath(null, task);
            return;
        }

        task.ParentCode = string.IsNullOrWhiteSpace(parent.Code) ? parentCode : parent.Code.Trim();
        task.ParentName = parent.Name;
        task.HierarchyPath = BuildHierarchyPath(parent, task);
    }

    private static TaskDefinition? ResolveParentTask(
        string parentTaskId,
        string? parentCode,
        string? taskCode,
        IReadOnlyDictionary<string, TaskDefinition> taskByExternalId,
        IReadOnlyDictionary<string, TaskDefinition> taskByCode)
    {
        if (!string.IsNullOrWhiteSpace(parentTaskId) &&
            taskByExternalId.TryGetValue(parentTaskId.Trim(), out var byId))
            return byId;

        if (!string.IsNullOrWhiteSpace(parentCode) &&
            taskByCode.TryGetValue(parentCode.Trim(), out var byCode))
            return byCode;

        var inferredParentCode = InferParentCode(taskCode, taskByCode.Keys);
        return !string.IsNullOrWhiteSpace(inferredParentCode) &&
               taskByCode.TryGetValue(inferredParentCode, out var inferredParent)
            ? inferredParent
            : null;
    }

    private static string? InferParentCode(string? code, IEnumerable<string> knownCodes)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var trimmedCode = code.Trim();
        return knownCodes
            .Where(candidate => IsLikelyParentCode(trimmedCode, candidate))
            .OrderByDescending(candidate => candidate.Length)
            .FirstOrDefault();
    }

    private static bool IsLikelyParentCode(string code, string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        var parent = candidate.Trim();
        if (parent.Length >= code.Length ||
            !code.StartsWith(parent, StringComparison.OrdinalIgnoreCase))
            return false;

        var next = code[parent.Length];
        return next == '.' || char.IsLetterOrDigit(next);
    }

    private static string BuildHierarchyPath(TaskDefinition? parent, TaskDefinition task)
    {
        var current = FormatHierarchyPart(task.Code, task.Name);
        return string.IsNullOrWhiteSpace(parent?.HierarchyPath)
            ? current
            : $"{parent.HierarchyPath} > {current}";
    }

    private static string FormatHierarchyPart(string? code, string name)
        => string.IsNullOrWhiteSpace(code) ? name.Trim() : $"{code.Trim()} {name.Trim()}";

    // ──────────────────────────────────────────────────────────────────────
    // Upsert resource assignment
    // ──────────────────────────────────────────────────────────────────────

    private static async Task<(bool Created, string RowId, TaskDefinitionResourceLink Link)> UpsertResourceAssignmentAsync(
        TaskResourceBlueprintsContext db,
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> map,
        int rowNumber,
        TaskResourceCsvImportResult result,
        Dictionary<string, TaskDefinition> taskByExternalId,
        Dictionary<string, TaskDefinition> taskByCode,
        TaskDefinition? currentTask,
        Dictionary<string, ResourceCategory> foldersByPath,
        Dictionary<string, ResourceDefinition> resourcesByKey,
        HashSet<string> existingLinkKeys,
        Dictionary<int, int> nextFolderSortOrderByParent,
        int resourceSortOrder,
        string fileName,
        CancellationToken ct)
    {
        var parentTaskId = Get(row, map, "ParentTaskId").Trim();
        var parentCode = Get(row, map, "ParentCode").Trim();
        var name = Get(row, map, "Name", fallbackIndex: FallbackNameIndex).Trim();

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Resource name is required.");

        var task = ResolveTask(taskByExternalId, taskByCode, currentTask, parentTaskId, parentCode);
        if (task is null)
            throw new InvalidOperationException($"Parent task was not found. ParentTaskId='{parentTaskId}', ParentCode='{parentCode}'.");

        var folder = await EnsureFolderPathAsync(
            db, Get(row, map, "ResourceFolder"), foldersByPath, nextFolderSortOrderByParent, result, ct);

        var typeText = Get(row, map, "ResourceType");
        var resourceType = ParseResourceType(typeText, rowNumber, result);
        var resourceKey = ResourceKey(name, folder?.Id, resourceType);
        var createdResource = false;

        if (!resourcesByKey.TryGetValue(resourceKey, out var resource))
        {
            resource = new ResourceDefinition
            {
                Name = name,
                Folder = folder,
                FolderId = folder?.Id,
                ResType = resourceType,
                IsActive = true,
                IsVisible = true,
                SortOrder = resourceSortOrder,
                Data = new ResourceMetadata()
            };
            db.Resources.Add(resource);
            await db.SaveChangesAsync(ct);
            resourcesByKey[resourceKey] = resource;
            result.CreatedResources++;
            createdResource = true;
        }
        else
        {
            result.UpdatedResources++;
        }

        var capWaste = ParseDecimalOrDefault(Get(row, map, "CapWaste"), 0m);
        var factor = ParseDecimalOrDefault(Get(row, map, "QuantityFactor"), 1m);
        var unitCost = ParseNullableDecimal(Get(row, map, "UnitCost")) ?? 0m;
        var capWasteType = Get(row, map, "CapWasteType");

        resource.Name = name;
        resource.Folder = folder;
        resource.FolderId = folder?.Id;
        resource.ResType = resourceType;
        resource.IsActive = true;
        resource.IsVisible = true;
        resource.AdminNote = BuildAdminNote(fileName, account: Get(row, map, "Account"), category: Get(row, map, "Category"), rowNumber);
        resource.Data ??= new ResourceMetadata();
        resource.Unit = Get(row, map, "Unit", fallbackIndex: FallbackUnitIndex).Trim();
        resource.Quantity = null;
        resource.Data.ChangeFactor1 = factor;
        resource.Data.ChangeFactor2 = 1m;
        resource.Data.CapWaste = capWaste;
        resource.Data.Cost = unitCost;
        resource.Data.Note = Get(row, map, "CalculationMethod").Trim();
        ApplyCapWaste(resource.Data, capWaste, capWasteType);
        resource.Data.Normalize();

        if (folder is not null && !task.VisibleFolderIds.Contains(folder.Id))
            task.VisibleFolderIds.Add(folder.Id);

        await db.SaveChangesAsync(ct);

        TaskDefinitionResourceLink resourceLink;
        var linkKey = AssignmentKey(task.Id, resource.Id);
        if (!existingLinkKeys.Contains(linkKey))
        {
            var quantity = ParseDecimalOrDefault(Get(row, map, "Quantity", fallbackIndex: FallbackQuantityIndex), 1m);
            resourceLink = new TaskDefinitionResourceLink
            {
                TaskDefinitionId = task.Id,
                ResourceDefinitionId = resource.Id,
                Quantity = quantity > 0 ? quantity : 1m,
                Parameters = [],
                AddOns = [],
                Times = [],
            };
            db.TaskDefinitionResourceLinks.Add(resourceLink);
            await db.SaveChangesAsync(ct);
            existingLinkKeys.Add(linkKey);
            result.CreatedAssignments++;
        }
        else
        {
            resourceLink = await db.TaskDefinitionResourceLinks
                .FirstAsync(l => l.TaskDefinitionId == task.Id && l.ResourceDefinitionId == resource.Id, ct);
            resourceLink.Parameters = [];
            resourceLink.AddOns = [];
            resourceLink.Times = [];
            await db.SaveChangesAsync(ct);
            result.UpdatedAssignments++;
        }

        var resourceRowId = Get(row, map, "TaskId").Trim();
        return (createdResource, resourceRowId, resourceLink);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Parameter row handler
    // ──────────────────────────────────────────────────────────────────────

    private static async Task ApplyParameterRowAsync(
        TaskResourceBlueprintsContext db,
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> map,
        int rowNumber,
        TaskResourceCsvImportResult result,
        Dictionary<string, TaskDefinition> taskByExternalId,
        TaskDefinition? currentTask,
        Dictionary<string, TaskDefinitionResourceLink> linksByRowId,
        CancellationToken ct)
    {
        var paramType = Get(row, map, "Parametertyp").Trim();
        var name = Get(row, map, "Name", fallbackIndex: FallbackNameIndex).Trim();
        var unit = Get(row, map, "Unit", fallbackIndex: FallbackUnitIndex).Trim();
        var valueStr = Get(row, map, "Parametervärde").Trim();
        var value = ParseNullableDecimal(valueStr) ?? 0m;

        if (paramType.StartsWith("Typ 0", StringComparison.OrdinalIgnoreCase))
        {
            var parentTaskId = Get(row, map, "ParentTaskId").Trim();
            TaskDefinition? task = null;
            if (!string.IsNullOrWhiteSpace(parentTaskId))
                taskByExternalId.TryGetValue(parentTaskId, out task);
            task ??= currentTask;

            if (task is null)
            {
                result.Issues.Add(new(rowNumber, $"Typ 0 parameter '{name}': parent task not found."));
                return;
            }

            task.ConversionParameters.Add(new TaskConversionParameter
            {
                Name = name,
                Unit = unit,
                Value = value,
            });

            var product = 1m;
            foreach (var p in task.ConversionParameters)
                product *= p.Value;
            task.ChangeFactor2 = Math.Round(product, 4, MidpointRounding.AwayFromZero);

            await db.SaveChangesAsync(ct);
            result.CreatedTaskConversionParams++;
            return;
        }

        var resursradId = Get(row, map, "ResursradId").Trim();
        if (string.IsNullOrWhiteSpace(resursradId) || !linksByRowId.TryGetValue(resursradId, out var link))
        {
            result.Issues.Add(new(rowNumber, $"Parameter row '{name}': resource row '{resursradId}' not found."));
            return;
        }

        if (paramType.StartsWith("Typ 1", StringComparison.OrdinalIgnoreCase))
        {
            link.Parameters.Add(new ResourceParameter
            {
                Name = name,
                Unit = unit,
                Value = value,
            });
            await db.SaveChangesAsync(ct);
            result.CreatedResourceParameters++;
        }
        else if (paramType.StartsWith("Typ 2", StringComparison.OrdinalIgnoreCase))
        {
            link.AddOns.Add(new ResourceAddon
            {
                Name = name,
                Unit = unit,
                Cost = value,
                Factor = 1m,
                Type = QuantityResourceAddon.Multiplication,
            });
            await db.SaveChangesAsync(ct);
            result.CreatedResourceAddOns++;
        }
        else if (paramType.StartsWith("Typ 3", StringComparison.OrdinalIgnoreCase))
        {
            var quantity = ParseNullableDecimal(Get(row, map, "Quantity", fallbackIndex: FallbackQuantityIndex)) ?? 0m;
            link.Times.Add(new ResourceTime
            {
                Name = name,
                Unit = unit,
                Cost = value,
            }.SetResolvedQuantity(quantity));
            await db.SaveChangesAsync(ct);
            result.CreatedResourceTimes++;
        }
        else
        {
            result.Issues.Add(new(rowNumber, $"Unknown Parametertyp '{paramType}'."));
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // State group helpers
    // ──────────────────────────────────────────────────────────────────────

    private static async Task<TaskStateGroup> EnsureStateGroupAsync(
        TaskResourceBlueprintsContext db,
        string name,
        Dictionary<string, TaskStateGroup> stateGroupsByName,
        TaskResourceCsvImportResult result,
        CancellationToken ct)
    {
        var key = NormalizeToken(name);
        if (stateGroupsByName.TryGetValue(key, out var group))
            return group;

        var nextSort = stateGroupsByName.Count + 1;
        group = new TaskStateGroup { Name = name.Trim(), SortOrder = nextSort, IsVisible = true };
        db.TaskStateGroups.Add(group);
        await db.SaveChangesAsync(ct);
        stateGroupsByName[key] = group;
        result.CreatedStateGroups++;
        return group;
    }

    private static async Task<TaskState> EnsureStateAsync(
        TaskResourceBlueprintsContext db,
        string name,
        TaskStateGroup group,
        Dictionary<string, TaskState> statesByKey,
        TaskResourceCsvImportResult result,
        CancellationToken ct)
    {
        var key = StateKey(group.Id, name);
        if (statesByKey.TryGetValue(key, out var state))
            return state;

        var nextSort = statesByKey.Values.Count(s => s.TaskStateGroupId == group.Id) + 1;
        state = new TaskState
        {
            Name = name.Trim(),
            TaskStateGroupId = group.Id,
            SortOrder = nextSort,
            IsVisible = true
        };
        db.TaskStates.Add(state);
        await db.SaveChangesAsync(ct);
        statesByKey[key] = state;
        result.CreatedStates++;
        return state;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Folder helpers
    // ──────────────────────────────────────────────────────────────────────

    private static TaskDefinition? ResolveTask(
        IReadOnlyDictionary<string, TaskDefinition> taskByExternalId,
        IReadOnlyDictionary<string, TaskDefinition> taskByCode,
        TaskDefinition? currentTask,
        string parentTaskId,
        string parentCode)
    {
        if (!string.IsNullOrWhiteSpace(parentTaskId) && taskByExternalId.TryGetValue(parentTaskId, out var byId))
            return byId;

        if (currentTask is not null &&
            (string.IsNullOrWhiteSpace(parentCode) ||
             string.Equals(currentTask.Code?.Trim(), parentCode.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return currentTask;
        }

        if (!string.IsNullOrWhiteSpace(parentCode) && taskByCode.TryGetValue(parentCode, out var byCode))
            return byCode;
        return null;
    }

    private static async Task<ResourceCategory?> EnsureFolderPathAsync(
        TaskResourceBlueprintsContext db,
        string path,
        Dictionary<string, ResourceCategory> foldersByPath,
        Dictionary<int, int> nextFolderSortOrderByParent,
        TaskResourceCsvImportResult result,
        CancellationToken ct)
    {
        var parts = path
            .Split('>', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (parts.Count == 0)
            return null;

        ResourceCategory? parent = null;
        var currentPath = string.Empty;

        foreach (var part in parts)
        {
            currentPath = string.IsNullOrWhiteSpace(currentPath) ? part : $"{currentPath} > {part}";
            if (foldersByPath.TryGetValue(currentPath, out var existing))
            {
                parent = existing;
                continue;
            }

            var parentId = parent?.Id;
            var parentSortKey = ParentSortKey(parentId);
            var nextSortOrder = nextFolderSortOrderByParent.TryGetValue(parentSortKey, out var maxSortOrder)
                ? maxSortOrder + 1
                : 1;
            nextFolderSortOrderByParent[parentSortKey] = nextSortOrder;

            var folder = new ResourceCategory
            {
                DisplayName = part,
                ParentCategory = parent,
                ParentCategoryId = parent?.Id > 0 ? parent.Id : null,
                SortOrder = nextSortOrder,
                IsVisible = true
            };

            db.ResourceCategories.Add(folder);
            await db.SaveChangesAsync(ct);
            foldersByPath[currentPath] = folder;
            parent = folder;
            result.CreatedFolders++;
        }

        return parent;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Parsing helpers
    // ──────────────────────────────────────────────────────────────────────

    private static void ApplyCapWaste(ResourceMetadata metadata, decimal value, string capWasteType)
    {
        var normalizedType = NormalizeToken(capWasteType);
        if (normalizedType.Contains("spill", StringComparison.OrdinalIgnoreCase) ||
            normalizedType.Contains("waste", StringComparison.OrdinalIgnoreCase) ||
            normalizedType.Contains("%", StringComparison.OrdinalIgnoreCase))
        {
            metadata.Waste = value;
            metadata.Cap = 0m;
            return;
        }
        metadata.Cap = value;
        metadata.Waste = 0m;
    }

    private static ResourceTypesEnum ParseResourceType(string value, int rowNumber, TaskResourceCsvImportResult result)
    {
        var token = NormalizeToken(value);
        if (TryParseResourceTypeName(value, out var enumType))
            return enumType;

        if (token.Contains("material")) return ResourceTypesEnum.Materials;
        if (token.Contains("maskin") || token.Contains("machine") || token.Contains("equipment")) return ResourceTypesEnum.MachinesAndEquipments;
        if (token.Contains("arbet") || token.Contains("worker") || token.Contains("personal") || token.Contains("montor")) return ResourceTypesEnum.Worker;
        if (token is "ue" || token.Contains("underentrepren") || token.Contains("subcontract")) return ResourceTypesEnum.Subcontractors;
        if (token.Contains("manager") || token.Contains("ledning")) return ResourceTypesEnum.Managers;
        if (token.Contains("design") || token.Contains("projektering")) return ResourceTypesEnum.Design;
        if (token.Contains("risk")) return ResourceTypesEnum.Risk;
        if (token.Contains("overhead")) return ResourceTypesEnum.ProjectOverheadCosts;

        result.Issues.Add(new(rowNumber, $"Unknown resource type '{value}', imported as Adjustment."));
        return ResourceTypesEnum.Adjustment;
    }

    private static bool TryParseResourceTypeName(string value, out ResourceTypesEnum type)
    {
        if (Enum.TryParse(value.Trim(), ignoreCase: true, out type))
            return true;

        var token = NormalizeHeader(value ?? string.Empty);
        foreach (var candidate in Enum.GetValues<ResourceTypesEnum>())
        {
            if (NormalizeHeader(candidate.ToString()).Equals(token, StringComparison.OrdinalIgnoreCase))
            {
                type = candidate;
                return true;
            }
        }

        type = default;
        return false;
    }

    private static IReadOnlyDictionary<string, int> BuildColumnMap(IReadOnlyList<string> header)
    {
        var aliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Radtyp"]          = ["Radtyp", "RowType"],
            ["TaskId"]          = ["TaskId"],
            ["ParentTaskId"]    = ["ParentTaskId"],
            ["ParentCode"]      = ["ParentCode"],
            ["Code"]            = ["Code", "Kod"],
            ["Name"]            = ["Name", "Namn"],
            ["TaskNameSynonym1"] = ["TaskNameSynonym1", "NameSynonym1", "NamnSynonym1", "Task synonym 1"],
            ["TaskNameSynonym2"] = ["TaskNameSynonym2", "NameSynonym2", "NamnSynonym2", "Task synonym 2"],
            ["Quantity"]        = ["Mängd", "Mangd", "Quantity"],
            ["Unit"]            = ["Enhet", "Unit"],
            ["TaskUnitSynonym1"] = ["TaskUnitSynonym1", "UnitSynonym1", "EnhetSynonym1", "Unit synonym 1"],
            ["TaskUnitSynonym2"] = ["TaskUnitSynonym2", "UnitSynonym2", "EnhetSynonym2", "Unit synonym 2"],
            ["Property1"]       = ["Egenskap 1", "Egenskap1", "Property1"],
            ["Value1"]          = ["Värde 1", "Varde 1", "Value1"],
            ["Property2"]       = ["Egenskap 2", "Egenskap2", "Property2"],
            ["Value2"]          = ["Värde 2", "Varde 2", "Value2"],
            ["Account"]         = ["Konto", "Account"],
            ["ResourceType"]    = ["Resurs typ", "Resurstyp", "Resource type", "ResourceType"],
            ["ResourceFolder"]  = ["Resursmapp", "Resource folder", "ResourceFolder"],
            ["CapWaste"]        = ["Kapacitet/Spill", "Capacity/Waste"],
            ["CapWasteType"]    = ["Kapacitet/Spill typ", "Capacity/Waste type"],
            ["QuantityFactor"]  = ["Mängdpåverkande faktor", "Mangdpaverkande faktor", "Quantity factor"],
            ["UnitCost"]        = ["Enhetskostnad", "Unit cost"],
            ["Category"]        = ["Kategori", "Category"],
            ["CalculationMethod"] = ["Beräkningssätt", "Berakningssatt", "Calculation method"],
            ["ResursradId"]     = ["ResursradId", "ResourceRowId"],
            ["Parametertyp"]    = ["Parametertyp", "ParameterType"],
            ["Parametervärde"]  = ["Parametervärde", "Parametervarde", "ParameterValue"],
        };

        var normalizedHeader = header
            .Select((value, index) => new { Key = NormalizeHeader(value), Index = index })
            .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First().Index, StringComparer.OrdinalIgnoreCase);

        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, names) in aliases)
        {
            foreach (var name in names)
            {
                if (normalizedHeader.TryGetValue(NormalizeHeader(name), out var index))
                {
                    map[key] = index;
                    break;
                }
            }
        }

        return map;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (ch == Delimiter && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        values.Add(current.ToString());
        return values;
    }

    private static string Get(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> map, string key, int? fallbackIndex = null)
    {
        if (map.TryGetValue(key, out var index) && index >= 0 && index < row.Count)
            return row[index].Trim();
        if (fallbackIndex is int fallback && fallback >= 0 && fallback < row.Count)
            return row[fallback].Trim();
        return string.Empty;
    }

    private static List<string> ReadSynonyms(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> map, params string[] keys)
        => keys
            .Select(key => Get(row, map, key).Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static decimal? ParseNullableDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalizedValue = value.Trim()
            .Replace("\u00a0", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        if (decimal.TryParse(normalizedValue, NumberStyles.Number, CultureInfo.GetCultureInfo("sv-SE"), out var sv))
            return sv;

        var normalized = normalizedValue.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariant)
            ? invariant
            : null;
    }

    private static decimal ParseDecimalOrDefault(string value, decimal defaultValue)
        => ParseNullableDecimal(value) ?? defaultValue;

    private static string? EmptyToNull(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string BuildAdminNote(string? fileName, string? account, string? category, int rowNumber)
    {
        var parts = new List<string> { $"Imported from CSV row {rowNumber}." };
        if (!string.IsNullOrWhiteSpace(fileName)) parts.Add($"File: {fileName.Trim()}.");
        if (!string.IsNullOrWhiteSpace(account))  parts.Add($"Account: {account.Trim()}.");
        if (!string.IsNullOrWhiteSpace(category)) parts.Add($"Category: {category.Trim()}.");
        return string.Join(" ", parts);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Key helpers
    // ──────────────────────────────────────────────────────────────────────

    private static string ResourceKey(string name, int? folderId, ResourceTypesEnum type)
        => $"{NormalizeToken(name)}|{folderId?.ToString(CultureInfo.InvariantCulture) ?? ""}|{(int)type}";

    private static string TaskImportKey(
        string? code,
        string name,
        IEnumerable<(string Property, string Value)> statePairs)
    {
        var identity = !string.IsNullOrWhiteSpace(code)
            ? $"code:{NormalizeToken(code)}"
            : $"name:{NormalizeToken(name)}";

        var stateKeyParts = statePairs
            .Where(x => !string.IsNullOrWhiteSpace(x.Property) && !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"{NormalizeToken(x.Property)}={NormalizeToken(x.Value)}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        return stateKeyParts.Count == 0
            ? identity
            : $"{identity}|states:{string.Join("|", stateKeyParts)}";
    }

    private static string AssignmentKey(int taskId, int resourceId)
        => $"{taskId}:{resourceId}";

    private static string StateKey(int groupId, string stateName)
        => $"{groupId}|{NormalizeToken(stateName)}";

    private static string BuildFolderPath(ResourceCategory folder, IReadOnlyDictionary<int, ResourceCategory> byId)
    {
        var names = new Stack<string>();
        var current = folder;
        while (current is not null)
        {
            names.Push(current.DisplayName.Trim());
            current = current.ParentCategoryId.HasValue && byId.TryGetValue(current.ParentCategoryId.Value, out var parent)
                ? parent
                : null;
        }
        return string.Join(" > ", names);
    }

    private static int ParentSortKey(int? parentId) => parentId ?? 0;

    private static string NormalizeHeader(string value)
        => NormalizeToken(value).Replace(" ", string.Empty, StringComparison.Ordinal);

    private static string NormalizeToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
