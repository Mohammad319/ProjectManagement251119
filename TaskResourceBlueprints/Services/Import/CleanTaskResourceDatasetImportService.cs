using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Helper.Text;
using System.Globalization;
using System.Text;
using System.Text.Json;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Entities.Resources;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Infrastructure;
using TaskResourceBlueprints.Infrastructure.ConfigurationConstants;

namespace TaskResourceBlueprints.Services.Import;

public interface ICleanTaskResourceDatasetImportService
{
    Task<CleanTaskResourceDatasetAnalysis> AnalyzeAsync(
        Stream fileStream,
        string fileName,
        CancellationToken ct = default);

    Task<CleanTaskResourceDatasetImportResult> ImportAsync(
        Stream fileStream,
        string fileName,
        bool updateExisting = false,
        CancellationToken ct = default);
}

public class CleanTaskResourceDatasetAnalysis
{
    public int TotalRows { get; set; }
    public int TaskRows { get; set; }
    public int ResourceRows { get; set; }
    public int UniqueTasks { get; set; }
    public int UniqueResources { get; set; }
    public int Links { get; set; }
    public int DuplicateLinks { get; set; }
    public int SkippedRows { get; set; }
    public List<CleanTaskResourceDatasetIssue> Issues { get; set; } = [];
}

public sealed class CleanTaskResourceDatasetImportResult : CleanTaskResourceDatasetAnalysis
{
    public int CreatedTasks { get; set; }
    public int UpdatedTasks { get; set; }
    public int ExistingTasks { get; set; }
    public int CreatedResources { get; set; }
    public int UpdatedResources { get; set; }
    public int ExistingResources { get; set; }
    public int CreatedFolders { get; set; }
    public int CreatedAssignments { get; set; }
    public int ExistingAssignments { get; set; }
}

public sealed record CleanTaskResourceDatasetIssue(int RowNumber, string Message);

public sealed record CleanTaskResourceTaskRow(
    int RowNumber,
    string Name,
    string? Code,
    string? ParentCode,
    decimal? Quantity,
    string? UnitCode,
    IReadOnlyList<string> NameSynonyms,
    IReadOnlyList<string> UnitSynonyms);

public sealed record CleanTaskResourceLinkRow(
    int RowNumber,
    CleanTaskResourceTaskRow Task,
    string ResourceName,
    ResourceTypesEnum ResourceType,
    string? ResourceFolder,
    decimal ResourceQuantity,
    string? ResourceUnit,
    decimal UnitCost,
    decimal QuantityFactor);

public sealed class CleanTaskResourceDatasetImportService(IDbContextFactory<TaskResourceBlueprintsContext> dbContextFactory)
    : ICleanTaskResourceDatasetImportService
{
    public async Task<CleanTaskResourceDatasetAnalysis> AnalyzeAsync(
        Stream fileStream,
        string fileName,
        CancellationToken ct = default)
    {
        var (analysis, _, _) = await CleanTaskResourceDatasetReader.ReadAsync(fileStream, fileName, ct);
        return analysis;
    }

    public async Task<CleanTaskResourceDatasetImportResult> ImportAsync(
        Stream fileStream,
        string fileName,
        bool updateExisting = false,
        CancellationToken ct = default)
    {
        var (analysis, tasks, links) = await CleanTaskResourceDatasetReader.ReadAsync(fileStream, fileName, ct);
        var result = CopyAnalysis(analysis);

        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var existingTasks = await db.Tasks.ToListAsync(ct);
        var existingResources = await db.Resources.ToListAsync(ct);
        var folders = await db.ResourceCategories.ToListAsync(ct);
        var existingLinks = await db.TaskDefinitionResourceLinks.ToListAsync(ct);
        var foldersByPath = BuildFolderDictionary(folders);
        var taskByCode = existingTasks
            .Where(x => !string.IsNullOrWhiteSpace(x.Code))
            .GroupBy(x => x.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var taskByName = existingTasks
            .GroupBy(x => NormalizeNameKey(x.Name))
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var resourceByKey = existingResources
            .ToDictionary(x => ResourceKey(x.Name, x.FolderId, x.ResType), StringComparer.OrdinalIgnoreCase);
        var linkKeys = existingLinks
            .Select(x => LinkKey(x.TaskDefinitionId, x.ResourceDefinitionId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var nextTaskSortOrder = existingTasks.Select(x => (int?)x.SortOrder).Max() ?? 0;
        var nextResourceSortOrder = existingResources.Select(x => (int?)x.SortOrder).Max() ?? 0;
        var nextFolderSortOrder = folders
            .GroupBy(x => x.ParentCategoryId)
            .ToDictionary(x => ParentKey(x.Key), x => x.Select(folder => folder.SortOrder).DefaultIfEmpty(0).Max());

        var importedTasks = new Dictionary<string, TaskDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var taskRow in tasks)
        {
            var task = ResolveTask(taskRow, taskByCode, taskByName, importedTasks);
            if (task is null)
            {
                task = new TaskDefinition
                {
                    SortOrder = ++nextTaskSortOrder,
                    Status = TaskStatusEnum.Ready,
                    IsActive = true,
                    IsVisible = true,
                };
                ApplyTask(task, taskRow, fileName, taskByCode);
                db.Tasks.Add(task);
                result.CreatedTasks++;
                await db.SaveChangesAsync(ct);
            }
            else if (updateExisting)
            {
                ApplyTask(task, taskRow, fileName, taskByCode);
                result.UpdatedTasks++;
                await db.SaveChangesAsync(ct);
            }
            else
            {
                result.ExistingTasks++;
            }

            TrackTask(task, taskByCode, taskByName, importedTasks);
        }

        foreach (var linkRow in links)
        {
            ct.ThrowIfCancellationRequested();

            var task = ResolveTask(linkRow.Task, taskByCode, taskByName, importedTasks);
            if (task is null)
            {
                result.SkippedRows++;
                result.Issues.Add(new(linkRow.RowNumber, "Parent task was not found during import."));
                continue;
            }

            var folder = await EnsureFolderAsync(db, linkRow.ResourceFolder, foldersByPath, nextFolderSortOrder, result, ct);
            var resourceKey = ResourceKey(linkRow.ResourceName, folder?.Id, linkRow.ResourceType);
            if (!resourceByKey.TryGetValue(resourceKey, out var resource))
            {
                resource = new ResourceDefinition
                {
                    Name = Limit(linkRow.ResourceName, Lengths.DisplayName),
                    Folder = folder,
                    FolderId = folder?.Id,
                    ResType = linkRow.ResourceType,
                    SortOrder = ++nextResourceSortOrder,
                    IsActive = true,
                    IsVisible = true,
                    Data = new ResourceMetadata()
                };
                ApplyResource(resource, linkRow, fileName);
                db.Resources.Add(resource);
                await db.SaveChangesAsync(ct);
                resourceByKey[resourceKey] = resource;
                result.CreatedResources++;
            }
            else if (updateExisting)
            {
                ApplyResource(resource, linkRow, fileName);
                result.UpdatedResources++;
                await db.SaveChangesAsync(ct);
            }
            else
            {
                result.ExistingResources++;
            }

            var linkKey = LinkKey(task.Id, resource.Id);
            if (!linkKeys.Add(linkKey))
            {
                result.ExistingAssignments++;
                continue;
            }

            db.TaskDefinitionResourceLinks.Add(new TaskDefinitionResourceLink
            {
                TaskDefinitionId = task.Id,
                ResourceDefinitionId = resource.Id,
                Quantity = linkRow.ResourceQuantity > 0 ? linkRow.ResourceQuantity : 1m,
                Parameters = [],
                AddOns = [],
                Times = [],
            });
            result.CreatedAssignments++;
            await db.SaveChangesAsync(ct);
        }

        return result;
    }

    private static CleanTaskResourceDatasetImportResult CopyAnalysis(CleanTaskResourceDatasetAnalysis analysis)
        => new()
        {
            TotalRows = analysis.TotalRows,
            TaskRows = analysis.TaskRows,
            ResourceRows = analysis.ResourceRows,
            UniqueTasks = analysis.UniqueTasks,
            UniqueResources = analysis.UniqueResources,
            Links = analysis.Links,
            DuplicateLinks = analysis.DuplicateLinks,
            SkippedRows = analysis.SkippedRows,
            Issues = [.. analysis.Issues],
        };

    private static TaskDefinition? ResolveTask(
        CleanTaskResourceTaskRow row,
        IReadOnlyDictionary<string, TaskDefinition> taskByCode,
        IReadOnlyDictionary<string, TaskDefinition> taskByName,
        IReadOnlyDictionary<string, TaskDefinition> importedTasks)
    {
        var importKey = TaskImportKey(row);
        if (importedTasks.TryGetValue(importKey, out var imported))
            return imported;

        if (!string.IsNullOrWhiteSpace(row.Code) && taskByCode.TryGetValue(row.Code.Trim(), out var byCode))
            return byCode;

        return taskByName.TryGetValue(NormalizeNameKey(row.Name), out var byName) ? byName : null;
    }

    private static void TrackTask(
        TaskDefinition task,
        IDictionary<string, TaskDefinition> taskByCode,
        IDictionary<string, TaskDefinition> taskByName,
        IDictionary<string, TaskDefinition> importedTasks)
    {
        if (!string.IsNullOrWhiteSpace(task.Code))
            taskByCode[task.Code.Trim()] = task;
        taskByName[NormalizeNameKey(task.Name)] = task;
        importedTasks[TaskImportKey(task.Name, task.Code)] = task;
    }

    private static void ApplyTask(
        TaskDefinition task,
        CleanTaskResourceTaskRow row,
        string fileName,
        IReadOnlyDictionary<string, TaskDefinition> taskByCode)
    {
        task.Name = Limit(row.Name, Lengths.DisplayName);
        task.Code = string.IsNullOrWhiteSpace(row.Code) ? task.Code : row.Code.Trim();
        TaskHierarchyContextBuilder.Apply(task, row.ParentCode, taskByCode);
        task.Quantity = row.Quantity ?? task.Quantity;
        task.UnitCode = string.IsNullOrWhiteSpace(row.UnitCode) ? task.UnitCode : row.UnitCode.Trim();
        var nameSynonyms = CleanSynonyms(row.NameSynonyms);
        if (nameSynonyms.Count > 0)
            task.NameSynonyms = nameSynonyms;

        var unitSynonyms = CleanSynonyms(row.UnitSynonyms);
        if (unitSynonyms.Count > 0)
            task.UnitSynonyms = unitSynonyms;
        task.AdminNote = $"Imported from clean task-resource dataset '{fileName}' row {row.RowNumber}.";
        task.ChangeFactor1 = 1m;
        task.ChangeFactor2 = 1m;
        task.RefreshNormalizedTextSv();
    }

    private static void ApplyResource(ResourceDefinition resource, CleanTaskResourceLinkRow row, string fileName)
    {
        resource.Name = Limit(row.ResourceName, Lengths.DisplayName);
        resource.Unit = row.ResourceUnit?.Trim();
        resource.Quantity = null;
        resource.AdminNote = $"Imported from clean task-resource dataset '{fileName}' row {row.RowNumber}.";
        resource.Data ??= new ResourceMetadata();
        resource.Data.Cost = row.UnitCost;
        resource.Data.ChangeFactor1 = row.QuantityFactor <= 0 ? 1m : row.QuantityFactor;
        resource.Data.ChangeFactor2 = 1m;
        resource.Data.Normalize();
    }

    private static async Task<ResourceCategory?> EnsureFolderAsync(
        TaskResourceBlueprintsContext db,
        string? folderPath,
        Dictionary<string, ResourceCategory> foldersByPath,
        Dictionary<string, int> nextSortOrder,
        CleanTaskResourceDatasetImportResult result,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return null;

        ResourceCategory? parent = null;
        var path = string.Empty;
        var parts = folderPath
            .Split(['/', '\\', '>'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x));

        foreach (var part in parts)
        {
            path = string.IsNullOrWhiteSpace(path) ? part : $"{path}/{part}";
            if (foldersByPath.TryGetValue(path, out var existing))
            {
                parent = existing;
                continue;
            }

            var parentKey = ParentKey(parent?.Id);
            var sortOrder = nextSortOrder.GetValueOrDefault(parentKey) + 1;
            nextSortOrder[parentKey] = sortOrder;
            var folder = new ResourceCategory
            {
                DisplayName = Limit(part, Lengths.DisplayName),
                ParentCategory = parent,
                ParentCategoryId = parent?.Id,
                SortOrder = sortOrder,
                IsVisible = true
            };
            db.ResourceCategories.Add(folder);
            await db.SaveChangesAsync(ct);
            foldersByPath[path] = folder;
            parent = folder;
            result.CreatedFolders++;
        }

        return parent;
    }

    private static Dictionary<string, ResourceCategory> BuildFolderDictionary(IReadOnlyList<ResourceCategory> folders)
    {
        var byId = folders.ToDictionary(x => x.Id);
        var result = new Dictionary<string, ResourceCategory>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in folders)
            result[BuildFolderPath(folder, byId)] = folder;
        return result;
    }

    private static string BuildFolderPath(ResourceCategory folder, IReadOnlyDictionary<int, ResourceCategory> byId)
    {
        var names = new Stack<string>();
        var current = folder;
        while (current is not null)
        {
            names.Push(current.DisplayName);
            current = current.ParentCategoryId.HasValue && byId.TryGetValue(current.ParentCategoryId.Value, out var parent)
                ? parent
                : null;
        }

        return string.Join('/', names);
    }

    private static string TaskImportKey(CleanTaskResourceTaskRow row)
        => TaskImportKey(row.Name, row.Code);

    private static string TaskImportKey(string name, string? code)
        => !string.IsNullOrWhiteSpace(code) ? $"code:{code.Trim()}" : $"name:{NormalizeNameKey(name)}";

    private static string NormalizeNameKey(string name)
        => SwedishTaskTextNormalizer.Normalize(name);

    private static string ResourceKey(string name, int? folderId, ResourceTypesEnum type)
        => $"{type}|{folderId?.ToString(CultureInfo.InvariantCulture) ?? "root"}|{SwedishTaskTextNormalizer.Normalize(name)}";

    private static string LinkKey(int taskId, int resourceId)
        => $"{taskId}:{resourceId}";

    private static string ParentKey(int? parentId)
        => parentId?.ToString(CultureInfo.InvariantCulture) ?? "root";

    private static string Limit(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength].Trim();

    private static List<string> CleanSynonyms(IEnumerable<string> values)
        => values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}

public static class CleanTaskResourceDatasetReader
{
    public static async Task<(
        CleanTaskResourceDatasetAnalysis Analysis,
        IReadOnlyList<CleanTaskResourceTaskRow> Tasks,
        IReadOnlyList<CleanTaskResourceLinkRow> Links)> ReadAsync(
        Stream fileStream,
        string fileName,
        CancellationToken ct = default)
    {
        if (IsJsonFile(fileName))
            return await ReadJsonAsync(fileStream, ct);

        var table = IsExcelFile(fileName)
            ? ReadExcelRows(fileStream)
            : await ReadTextRowsAsync(fileStream, ct);
        return AnalyzeRows(table);
    }

    private static async Task<(
        CleanTaskResourceDatasetAnalysis Analysis,
        IReadOnlyList<CleanTaskResourceTaskRow> Tasks,
        IReadOnlyList<CleanTaskResourceLinkRow> Links)> ReadJsonAsync(Stream fileStream, CancellationToken ct)
    {
        using var document = await JsonDocument.ParseAsync(fileStream, cancellationToken: ct);
        var root = document.RootElement;
        if (root.ValueKind is JsonValueKind.Object && TryGetProperty(root, out var tasksElement, "tasks", "items", "data"))
            root = tasksElement;

        if (root.ValueKind is not JsonValueKind.Array)
            throw new InvalidOperationException("Clean task-resource JSON must be an array, or an object with tasks/items/data array.");

        var analysis = new CleanTaskResourceDatasetAnalysis();
        var tasks = new Dictionary<string, CleanTaskResourceTaskRow>(StringComparer.OrdinalIgnoreCase);
        var links = new List<CleanTaskResourceLinkRow>();
        var seenLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var uniqueResources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rowNumber = 0;

        foreach (var taskElement in root.EnumerateArray())
        {
            ct.ThrowIfCancellationRequested();
            rowNumber++;
            analysis.TotalRows++;

            if (taskElement.ValueKind is not JsonValueKind.Object)
            {
                analysis.SkippedRows++;
                analysis.Issues.Add(new(rowNumber, "JSON task item must be an object."));
                continue;
            }

            var taskName = GetString(taskElement, "taskName", "name", "namn", "task");
            if (string.IsNullOrWhiteSpace(taskName))
            {
                analysis.SkippedRows++;
                analysis.Issues.Add(new(rowNumber, "JSON task is missing taskName/name."));
                continue;
            }

            var task = new CleanTaskResourceTaskRow(
                rowNumber,
                taskName,
                GetString(taskElement, "taskCode", "code", "kod"),
                GetString(taskElement, "parentCode", "parentTaskCode", "parent"),
                GetDecimal(taskElement, "taskQuantity", "quantity", "mangd"),
                GetString(taskElement, "taskUnit", "unit", "enhet"),
                GetStringList(taskElement, "taskNameSynonyms", "nameSynonyms", "taskSynonyms")
                    .Concat(ReadJsonSynonymFields(taskElement, "taskNameSynonym1", "nameSynonym1", "taskNameSynonym2", "nameSynonym2"))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                GetStringList(taskElement, "taskUnitSynonyms", "unitSynonyms")
                    .Concat(ReadJsonSynonymFields(taskElement, "taskUnitSynonym1", "unitSynonym1", "taskUnitSynonym2", "unitSynonym2"))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList());
            tasks.TryAdd(TaskKey(task), task);
            analysis.TaskRows++;

            if (!TryGetProperty(taskElement, out var resourcesElement, "resources", "resurser", "items"))
            {
                continue;
            }

            if (resourcesElement.ValueKind is not JsonValueKind.Array)
            {
                analysis.SkippedRows++;
                analysis.Issues.Add(new(rowNumber, "JSON resources property must be an array."));
                continue;
            }

            foreach (var resourceElement in resourcesElement.EnumerateArray())
            {
                rowNumber++;
                analysis.TotalRows++;
                if (resourceElement.ValueKind is not JsonValueKind.Object)
                {
                    analysis.SkippedRows++;
                    analysis.Issues.Add(new(rowNumber, "JSON resource item must be an object."));
                    continue;
                }

                var resourceName = GetString(resourceElement, "resourceName", "name", "resurs", "resource");
                if (string.IsNullOrWhiteSpace(resourceName))
                {
                    analysis.SkippedRows++;
                    analysis.Issues.Add(new(rowNumber, "JSON resource is missing resourceName/name."));
                    continue;
                }

                var resourceType = ParseResourceType(GetString(resourceElement, "resourceType", "type", "resurstyp") ?? string.Empty);
                var resourceFolder = GetString(resourceElement, "resourceFolder", "folder", "category", "mapp");
                var linkKey = $"{TaskKey(task)}|{resourceType}|{resourceFolder}|{SwedishTaskTextNormalizer.Normalize(resourceName)}";

                if (!seenLinks.Add(linkKey))
                {
                    analysis.DuplicateLinks++;
                    analysis.Issues.Add(new(rowNumber, "Duplicate JSON task-resource link."));
                    continue;
                }

                uniqueResources.Add($"{resourceType}|{resourceFolder}|{SwedishTaskTextNormalizer.Normalize(resourceName)}");
                links.Add(new CleanTaskResourceLinkRow(
                    rowNumber,
                    task,
                    resourceName,
                    resourceType,
                    resourceFolder,
                    GetDecimal(resourceElement, "resourceQuantity", "quantity", "qty") ?? 1m,
                    GetString(resourceElement, "resourceUnit", "unit", "enhet"),
                    GetDecimal(resourceElement, "unitCost", "cost", "price", "pris") ?? 0m,
                    GetDecimal(resourceElement, "quantityFactor", "factor", "faktor") ?? 1m));
                analysis.ResourceRows++;
                analysis.Links++;
            }
        }

        analysis.UniqueTasks = tasks.Count;
        analysis.UniqueResources = uniqueResources.Count;
        return (analysis, tasks.Values.ToList(), links);
    }

    private static (
        CleanTaskResourceDatasetAnalysis Analysis,
        IReadOnlyList<CleanTaskResourceTaskRow> Tasks,
        IReadOnlyList<CleanTaskResourceLinkRow> Links) AnalyzeRows(IReadOnlyList<IReadOnlyList<string>> table)
    {
        var analysis = new CleanTaskResourceDatasetAnalysis();
        if (table.Count == 0)
            return (analysis, [], []);

        var map = BuildColumnMap(table[0]);
        var firstDataRowIndex = map.HasAnyHeader ? 1 : 0;
        var tasks = new Dictionary<string, CleanTaskResourceTaskRow>(StringComparer.OrdinalIgnoreCase);
        var links = new List<CleanTaskResourceLinkRow>();
        var seenLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var uniqueResources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CleanTaskResourceTaskRow? currentTask = null;

        for (var i = firstDataRowIndex; i < table.Count; i++)
        {
            var row = table[i];
            var rowNumber = i + 1;
            if (row.All(string.IsNullOrWhiteSpace))
                continue;

            analysis.TotalRows++;
            var rowType = Get(row, map.RowTypeIndex).Trim().ToUpperInvariant();
            var isExplicitTaskRow = rowType is "T" or "TASK";
            var isExplicitResourceRow = rowType is "R" or "RESOURCE";
            var sharedName = Get(row, map.SharedNameIndex).Trim();
            var taskName = FirstNonEmpty(Get(row, map.TaskNameIndex), isExplicitResourceRow ? string.Empty : sharedName).Trim();
            var taskCode = EmptyToNull(Get(row, map.TaskCodeIndex));
            var parentCode = EmptyToNull(Get(row, map.ParentCodeIndex));
            var resourceName = FirstNonEmpty(Get(row, map.ResourceNameIndex), isExplicitResourceRow ? sharedName : string.Empty).Trim();
            var isTaskRow = isExplicitTaskRow ||
                (!isExplicitResourceRow && (!string.IsNullOrWhiteSpace(taskName) || !string.IsNullOrWhiteSpace(taskCode)));
            var isResourceRow = isExplicitResourceRow ||
                (!string.IsNullOrWhiteSpace(resourceName) && map.ResourceNameIndex >= 0);

            if (isTaskRow)
            {
                if (string.IsNullOrWhiteSpace(taskName))
                {
                    analysis.SkippedRows++;
                    analysis.Issues.Add(new(rowNumber, "Task row is missing TaskName/Namn."));
                    continue;
                }

                currentTask = new CleanTaskResourceTaskRow(
                    rowNumber,
                    taskName,
                    taskCode,
                    parentCode,
                    ParseNullableDecimal(Get(row, map.TaskQuantityIndex)),
                    EmptyToNull(Get(row, map.TaskUnitIndex)),
                    ReadSynonyms(row, map.TaskNameSynonym1Index, map.TaskNameSynonym2Index),
                    ReadSynonyms(row, map.TaskUnitSynonym1Index, map.TaskUnitSynonym2Index));
                tasks.TryAdd(TaskKey(currentTask), currentTask);
                analysis.TaskRows++;
            }

            if (!isResourceRow)
                continue;

            var parentTask = ResolveParentTask(row, map, currentTask, tasks);
            if (parentTask is null)
            {
                analysis.SkippedRows++;
                analysis.Issues.Add(new(rowNumber, "Resource row appears before any task row."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                analysis.SkippedRows++;
                analysis.Issues.Add(new(rowNumber, "Resource row is missing ResourceName/Resurs."));
                continue;
            }

            var resourceType = ParseResourceType(Get(row, map.ResourceTypeIndex));
            var resourceFolder = EmptyToNull(Get(row, map.ResourceFolderIndex));
            var resourceQuantity = ParseNullableDecimal(FirstNonEmpty(Get(row, map.ResourceQuantityIndex), isExplicitResourceRow ? Get(row, map.TaskQuantityIndex) : string.Empty)) ?? 1m;
            var resourceUnit = EmptyToNull(FirstNonEmpty(Get(row, map.ResourceUnitIndex), isExplicitResourceRow ? Get(row, map.TaskUnitIndex) : string.Empty));
            var unitCost = ParseNullableDecimal(Get(row, map.UnitCostIndex)) ?? 0m;
            var quantityFactor = ParseNullableDecimal(Get(row, map.QuantityFactorIndex)) ?? 1m;
            var linkKey = $"{TaskKey(parentTask)}|{resourceType}|{resourceFolder}|{SwedishTaskTextNormalizer.Normalize(resourceName)}";

            if (!seenLinks.Add(linkKey))
            {
                analysis.DuplicateLinks++;
                analysis.Issues.Add(new(rowNumber, "Duplicate task-resource link in file."));
                continue;
            }

            uniqueResources.Add($"{resourceType}|{resourceFolder}|{SwedishTaskTextNormalizer.Normalize(resourceName)}");
            links.Add(new CleanTaskResourceLinkRow(
                rowNumber,
                parentTask,
                resourceName,
                resourceType,
                resourceFolder,
                resourceQuantity,
                resourceUnit,
                unitCost,
                quantityFactor));
            analysis.ResourceRows++;
            analysis.Links++;
        }

        analysis.UniqueTasks = tasks.Count;
        analysis.UniqueResources = uniqueResources.Count;
        return (analysis, tasks.Values.ToList(), links);
    }

    private static CleanTaskResourceColumnMap BuildColumnMap(IReadOnlyList<string> header)
    {
        var normalized = header.Select(NormalizeHeader).ToList();
        var map = new CleanTaskResourceColumnMap(
            RowTypeIndex: Find(normalized, "rowtype", "radtyp", "type"),
            SharedNameIndex: Find(normalized, "name", "title"),
            ParentCodeIndex: Find(normalized, "parentcode", "parenttaskcode", "parenttask", "parent"),
            TaskCodeIndex: Find(normalized, "taskcode", "kod", "code", "taskid", "uppgiftskod"),
            TaskNameIndex: Find(normalized, "taskname", "task", "namn", "uppgift", "aktivitet", "arbetsmoment"),
            TaskQuantityIndex: Find(normalized, "taskquantity", "taskqty", "taskmangd", "mangd", "quantity"),
            TaskUnitIndex: Find(normalized, "taskunit", "taskenhet", "enhet", "unit", "mestlampligenhet"),
            TaskNameSynonym1Index: Find(normalized, "tasknamesynonym1", "tasknamesynonyms", "namesynonym1", "namesynonyms", "namnsynonym1", "tasksynonym1", "tagsynonyms"),
            TaskNameSynonym2Index: Find(normalized, "tasknamesynonym2", "namesynonym2", "namnsynonym2", "tasksynonym2"),
            TaskUnitSynonym1Index: Find(normalized, "taskunitsynonym1", "taskunitsynonyms", "unitsynonym1", "unitsynonyms", "enhetsynonym1"),
            TaskUnitSynonym2Index: Find(normalized, "taskunitsynonym2", "unitsynonym2", "enhetsynonym2"),
            ResourceNameIndex: Find(normalized, "resourcename", "resource", "resurs", "resursnamn", "material", "resourceitem"),
            ResourceTypeIndex: Find(normalized, "resourcetype", "resurstyp", "restyp", "typ"),
            ResourceFolderIndex: Find(normalized, "resourcefolder", "resursmapp", "folder", "mapp", "category", "kategori"),
            ResourceQuantityIndex: Find(normalized, "resourcequantity", "resourceqty", "resursmangd", "resquantity", "resqty"),
            ResourceUnitIndex: Find(normalized, "resourceunit", "resursenhet", "resunit", "resursunit"),
            UnitCostIndex: Find(normalized, "unitcost", "kostnad", "pris", "enhetspris", "price"),
            QuantityFactorIndex: Find(normalized, "quantityfactor", "factor", "faktor"));

        if (!map.HasAnyHeader)
            return map with
            {
                TaskCodeIndex = 0,
                SharedNameIndex = -1,
                ParentCodeIndex = -1,
                TaskNameIndex = 1,
                TaskQuantityIndex = 2,
                TaskUnitIndex = 3,
                ResourceNameIndex = 4,
                ResourceTypeIndex = 5,
                ResourceFolderIndex = 6,
                ResourceQuantityIndex = 7,
                ResourceUnitIndex = 8,
                UnitCostIndex = 9,
                QuantityFactorIndex = 10,
            };

        return map;
    }

    private static ResourceTypesEnum ParseResourceType(string value)
    {
        var token = NormalizeHeader(value);
        if (TryParseResourceTypeName(value, out var enumType))
            return enumType;

        if (token.Contains("material")) return ResourceTypesEnum.Materials;
        if (token.Contains("maskin") || token.Contains("machine") || token.Contains("equipment")) return ResourceTypesEnum.MachinesAndEquipments;
        if (token.Contains("arbet") || token.Contains("worker") || token.Contains("labor") || token.Contains("personal") || token.Contains("montor")) return ResourceTypesEnum.Worker;
        if (token is "ue" || token.Contains("underentrepren") || token.Contains("subcontract")) return ResourceTypesEnum.Subcontractors;
        if (token.Contains("manager") || token.Contains("ledning")) return ResourceTypesEnum.Managers;
        if (token.Contains("design") || token.Contains("projektering")) return ResourceTypesEnum.Design;
        if (token.Contains("risk")) return ResourceTypesEnum.Risk;
        if (token.Contains("overhead")) return ResourceTypesEnum.ProjectOverheadCosts;
        return ResourceTypesEnum.Adjustment;
    }

    private static bool TryParseResourceTypeName(string value, out ResourceTypesEnum type)
    {
        if (Enum.TryParse(value.Trim(), ignoreCase: true, out type))
            return true;

        var token = NormalizeHeader(value);
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

    private static int Find(IReadOnlyList<string> header, params string[] names)
    {
        for (var i = 0; i < header.Count; i++)
        {
            if (names.Contains(header[i], StringComparer.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static CleanTaskResourceTaskRow? ResolveParentTask(
        IReadOnlyList<string> row,
        CleanTaskResourceColumnMap map,
        CleanTaskResourceTaskRow? currentTask,
        IReadOnlyDictionary<string, CleanTaskResourceTaskRow> tasks)
    {
        var parentCode = EmptyToNull(Get(row, map.ParentCodeIndex));
        if (!string.IsNullOrWhiteSpace(parentCode) &&
            tasks.TryGetValue($"code:{parentCode.Trim()}", out var parentByCode))
        {
            return parentByCode;
        }

        return currentTask;
    }

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static string TaskKey(CleanTaskResourceTaskRow task)
        => !string.IsNullOrWhiteSpace(task.Code)
            ? $"code:{task.Code.Trim()}"
            : $"name:{SwedishTaskTextNormalizer.Normalize(task.Name)}";

    private static async Task<IReadOnlyList<IReadOnlyList<string>>> ReadTextRowsAsync(Stream fileStream, CancellationToken ct)
    {
        using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var lines = new List<string>();
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line))
                lines.Add(line);
        }

        var delimiter = DetectDelimiter(lines);
        return lines.Select(line => ParseDelimitedLine(line, delimiter)).ToList();
    }

    private static IReadOnlyList<IReadOnlyList<string>> ReadExcelRows(Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.First();
        var range = worksheet.RangeUsed();
        if (range is null)
            return [];

        var rows = new List<IReadOnlyList<string>>();
        foreach (var row in range.RowsUsed())
        {
            rows.Add(row.Cells(1, range.ColumnCount()).Select(cell => cell.GetFormattedString().Trim()).ToList());
        }

        return rows;
    }

    private static char DetectDelimiter(IReadOnlyList<string> lines)
    {
        var sample = lines.Take(10).ToList();
        var candidates = new[] { ';', '\t', ',' };
        return candidates
            .Select(delimiter => new { Delimiter = delimiter, Score = sample.Sum(line => ParseDelimitedLine(line, delimiter).Count) })
            .OrderByDescending(x => x.Score)
            .First().Delimiter;
    }

    private static IReadOnlyList<string> ParseDelimitedLine(string line, char delimiter)
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
            }
            else if (ch == delimiter && !inQuotes)
            {
                values.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        values.Add(current.ToString().Trim());
        return values;
    }

    private static bool IsExcelFile(string fileName)
        => fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
           fileName.EndsWith(".xlsm", StringComparison.OrdinalIgnoreCase);

    private static bool IsJsonFile(string fileName)
        => fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeHeader(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(ch))
                builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string Get(IReadOnlyList<string> row, int index)
        => index >= 0 && index < row.Count ? row[index] : string.Empty;

    private static IReadOnlyList<string> ReadSynonyms(IReadOnlyList<string> row, params int[] indexes)
        => indexes
            .Select(index => Get(row, index).Trim())
            .SelectMany(SplitSynonymCell)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static IEnumerable<string> SplitSynonymCell(string value)
        => value
            .Split([';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .DefaultIfEmpty(value.Trim());

    private static string? EmptyToNull(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal? ParseNullableDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().Replace(" ", string.Empty).Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static bool TryGetProperty(JsonElement element, out JsonElement value, params string[] names)
    {
        foreach (var property in element.EnumerateObject())
        {
            var normalizedName = NormalizeHeader(property.Name);
            if (names.Any(name => string.Equals(normalizedName, NormalizeHeader(name), StringComparison.OrdinalIgnoreCase)))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? GetString(JsonElement element, params string[] names)
    {
        if (!TryGetProperty(element, out var value, names))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static IReadOnlyList<string> GetStringList(JsonElement element, params string[] names)
    {
        if (!TryGetProperty(element, out var value, names))
            return [];

        if (value.ValueKind == JsonValueKind.Array)
        {
            return value
                .EnumerateArray()
                .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var singleValue = value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : value.GetRawText();

        return string.IsNullOrWhiteSpace(singleValue) ? [] : [singleValue.Trim()];
    }

    private static IReadOnlyList<string> ReadJsonSynonymFields(JsonElement element, params string[] names)
        => names
            .Select(name => GetString(element, name))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static decimal? GetDecimal(JsonElement element, params string[] names)
    {
        if (!TryGetProperty(element, out var value, names))
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var parsed))
            return parsed;

        return value.ValueKind == JsonValueKind.String
            ? ParseNullableDecimal(value.GetString() ?? string.Empty)
            : null;
    }

    private sealed record CleanTaskResourceColumnMap(
        int RowTypeIndex,
        int SharedNameIndex,
        int ParentCodeIndex,
        int TaskCodeIndex,
        int TaskNameIndex,
        int TaskQuantityIndex,
        int TaskUnitIndex,
        int TaskNameSynonym1Index,
        int TaskNameSynonym2Index,
        int TaskUnitSynonym1Index,
        int TaskUnitSynonym2Index,
        int ResourceNameIndex,
        int ResourceTypeIndex,
        int ResourceFolderIndex,
        int ResourceQuantityIndex,
        int ResourceUnitIndex,
        int UnitCostIndex,
        int QuantityFactorIndex)
    {
        public bool HasAnyHeader =>
            RowTypeIndex >= 0 ||
            SharedNameIndex >= 0 ||
            TaskCodeIndex >= 0 ||
            TaskNameIndex >= 0 ||
            ResourceNameIndex >= 0 ||
            ResourceTypeIndex >= 0;
    }
}
