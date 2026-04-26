using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.App.Dataloader;
using ProjectManagement.Shared.Enums;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Entities.Questions.Assignments;
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
    public int CreatedTasks { get; set; }
    public int UpdatedTasks { get; set; }
    public int CreatedResources { get; set; }
    public int UpdatedResources { get; set; }
    public int CreatedFolders { get; set; }
    public int CreatedAssignments { get; set; }
    public int UpdatedAssignments { get; set; }
    public bool DeletedExistingData { get; set; }
    public int SkippedRows { get; set; }
    public List<TaskResourceCsvImportIssue> Issues { get; set; } = [];
}

public sealed record TaskResourceCsvImportIssue(int RowNumber, string Message);

public sealed class TaskResourceCsvImportService(IDbContextFactory<TaskResourceBlueprintsContext> dbContextFactory)
    : ITaskResourceCsvImportService
{
    private const char Delimiter = ';';

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
        var foldersByPath = await LoadFoldersAsync(db, ct);
        var resourcesByKey = await LoadResourcesAsync(db, ct);
        var assignmentsByKey = await LoadAssignmentsAsync(db, ct);
        var nextTaskSortOrder = await db.Tasks.Select(x => (int?)x.SortOrder).MaxAsync(ct) ?? 0;
        var nextResourceSortOrder = await db.Resources.Select(x => (int?)x.SortOrder).MaxAsync(ct) ?? 0;
        var nextFolderSortOrderByParent = await db.ResourceCategories
            .GroupBy(x => x.ParentCategoryId)
            .Select(x => new { ParentId = x.Key, MaxSortOrder = x.Max(f => f.SortOrder) })
            .ToDictionaryAsync(x => ParentSortKey(x.ParentId), x => x.MaxSortOrder, ct);

        await LoadTaskDictionariesAsync(db, taskByExternalId, taskByCode, ct);

        var rowNumber = 1;
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
                    await UpsertTaskAsync(db, row, map, fileName, rowNumber, result, taskByExternalId, taskByCode, ++nextTaskSortOrder, ct);
                }
                else if (rowType.Equals("R", StringComparison.OrdinalIgnoreCase))
                {
                    result.ResourceRows++;
                    var created = await UpsertResourceAssignmentAsync(
                        db,
                        row,
                        map,
                        rowNumber,
                        result,
                        taskByExternalId,
                        taskByCode,
                        foldersByPath,
                        resourcesByKey,
                        assignmentsByKey,
                        nextFolderSortOrderByParent,
                        ++nextResourceSortOrder,
                        fileName,
                        ct);

                    if (!created)
                        nextResourceSortOrder--;
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

    private static async Task LoadTaskDictionariesAsync(
        TaskResourceBlueprintsContext db,
        Dictionary<string, TaskDefinition> taskByExternalId,
        Dictionary<string, TaskDefinition> taskByCode,
        CancellationToken ct)
    {
        var tasks = await db.Tasks.ToListAsync(ct);
        foreach (var task in tasks)
        {
            if (!string.IsNullOrWhiteSpace(task.Code))
                taskByCode[task.Code.Trim()] = task;
        }
    }

    private static async Task<Dictionary<string, ResourceCategory>> LoadFoldersAsync(
        TaskResourceBlueprintsContext db,
        CancellationToken ct)
    {
        var folders = await db.ResourceCategories.ToListAsync(ct);
        var byId = folders.ToDictionary(x => x.Id);
        var byPath = new Dictionary<string, ResourceCategory>(StringComparer.OrdinalIgnoreCase);

        foreach (var folder in folders)
        {
            byPath[BuildFolderPath(folder, byId)] = folder;
        }

        return byPath;
    }

    private static async Task<Dictionary<string, ResourceDefinition>> LoadResourcesAsync(
        TaskResourceBlueprintsContext db,
        CancellationToken ct)
    {
        var resources = await db.Resources.ToListAsync(ct);
        return resources
            .GroupBy(x => ResourceKey(x.Name, x.FolderId, x.ResType), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<Dictionary<string, TaskResourceAssignment>> LoadAssignmentsAsync(
        TaskResourceBlueprintsContext db,
        CancellationToken ct)
    {
        return await db.TaskResourceAssignments
            .ToDictionaryAsync(x => AssignmentKey(x.TaskId, x.ResourceId), x => x, ct);
    }

    private static async Task UpsertTaskAsync(
        TaskResourceBlueprintsContext db,
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> map,
        string fileName,
        int rowNumber,
        TaskResourceCsvImportResult result,
        Dictionary<string, TaskDefinition> taskByExternalId,
        Dictionary<string, TaskDefinition> taskByCode,
        int sortOrder,
        CancellationToken ct)
    {
        var externalId = Get(row, map, "TaskId").Trim();
        var code = Get(row, map, "Code", fallbackIndex: 4).Trim();
        var name = Get(row, map, "Name", fallbackIndex: 5).Trim();

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Task name is required.");

        TaskDefinition? task = null;
        if (!string.IsNullOrWhiteSpace(externalId))
            taskByExternalId.TryGetValue(externalId, out task);

        if (!string.IsNullOrWhiteSpace(code))
            task ??= taskByCode.TryGetValue(code, out var byCode) ? byCode : null;

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
        task.Quantity = ParseNullableDecimal(Get(row, map, "Quantity"));
        task.UnitCode = EmptyToNull(Get(row, map, "Unit"));
        task.FieldNotes = EmptyToNull(Get(row, map, "CalculationMethod"));
        task.AdminNote = BuildAdminNote(fileName, account: null, category: Get(row, map, "Category"), rowNumber);
        task.ChangeFactor1 = 1m;
        task.ChangeFactor2 = 1m;
        task.RefreshNormalizedTextSv();

        if (!string.IsNullOrWhiteSpace(externalId))
            taskByExternalId[externalId] = task;

        if (!string.IsNullOrWhiteSpace(task.Code))
            taskByCode[task.Code.Trim()] = task;

        await db.SaveChangesAsync(ct);
    }

    private static async Task DeleteExistingImportDataAsync(TaskResourceBlueprintsContext db, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        await db.NumericResourceAssignments.ExecuteDeleteAsync(ct);
        await db.OptionResourceAssignments.ExecuteDeleteAsync(ct);
        await db.ConditionResourceAssignments.ExecuteDeleteAsync(ct);
        await db.TaskResourceAssignments.ExecuteDeleteAsync(ct);
        await db.ResourceChoiceOptions.ExecuteDeleteAsync(ct);
        await db.ResourceAttributeValues.ExecuteDeleteAsync(ct);
        await db.ResourceTenantLinks.ExecuteDeleteAsync(ct);
        await db.Tasks.ExecuteDeleteAsync(ct);
        await db.Resources.ExecuteDeleteAsync(ct);
        await db.ResourceCategories.ExecuteUpdateAsync(
            setters => setters.SetProperty(x => x.ParentCategoryId, (int?)null),
            ct);
        await db.ResourceCategories.ExecuteDeleteAsync(ct);

        await transaction.CommitAsync(ct);
    }

    private static async Task<bool> UpsertResourceAssignmentAsync(
        TaskResourceBlueprintsContext db,
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> map,
        int rowNumber,
        TaskResourceCsvImportResult result,
        Dictionary<string, TaskDefinition> taskByExternalId,
        Dictionary<string, TaskDefinition> taskByCode,
        Dictionary<string, ResourceCategory> foldersByPath,
        Dictionary<string, ResourceDefinition> resourcesByKey,
        Dictionary<string, TaskResourceAssignment> assignmentsByKey,
        Dictionary<int, int> nextFolderSortOrderByParent,
        int resourceSortOrder,
        string fileName,
        CancellationToken ct)
    {
        var parentTaskId = Get(row, map, "ParentTaskId").Trim();
        var parentCode = Get(row, map, "ParentCode").Trim();
        var name = Get(row, map, "Name", fallbackIndex: 5).Trim();

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Resource name is required.");

        var task = ResolveTask(taskByExternalId, taskByCode, parentTaskId, parentCode);
        if (task is null)
            throw new InvalidOperationException($"Parent task was not found. ParentTaskId='{parentTaskId}', ParentCode='{parentCode}'.");

        var folder = await EnsureFolderPathAsync(
            db,
            Get(row, map, "ResourceFolder"),
            foldersByPath,
            nextFolderSortOrderByParent,
            result,
            ct);

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
                CalcResCost = new CalcResCost(),
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
        resource.AdminNote = BuildAdminNote(
            fileName,
            account: Get(row, map, "Account"),
            category: Get(row, map, "Category"),
            rowNumber);
        resource.Data ??= new ResourceMetadata();
        resource.Data.Unit = Get(row, map, "Unit").Trim();
        resource.Data.Quantity = null;
        resource.Data.ChangeFactor1 = factor;
        resource.Data.ChangeFactor2 = 1m;
        resource.Data.CapWaste = capWaste;
        resource.Data.Cost = unitCost;
        //resource.Data.BaseCost = unitCost;
        resource.Data.Note = Get(row, map, "CalculationMethod").Trim();
        ApplyCapWaste(resource.Data, capWaste, capWasteType);
        resource.Data.Normalize();

        var assignmentKey = AssignmentKey(task.Id, resource.Id);
        if (!assignmentsByKey.TryGetValue(assignmentKey, out var assignment))
        {
            assignment = new TaskResourceAssignment
            {
                Task = task,
                Resource = resource,
                TaskId = task.Id,
                ResourceId = resource.Id,
                IsActive = true,
            };
            db.TaskResourceAssignments.Add(assignment);
            assignmentsByKey[assignmentKey] = assignment;
            result.CreatedAssignments++;
        }
        else
        {
            result.UpdatedAssignments++;
        }

        assignment.ChangeFactor1 = factor;
        assignment.ChangeFactor2 = 1m;
        assignment.CapWaste = capWaste;
        //assignment.BaseCost = unitCost;
        assignment.Uncontrollable = resourceType == ResourceTypesEnum.Subcontractors;
        assignment.Expressions = string.IsNullOrWhiteSpace(resource.Data.Note) ? [] : [resource.Data.Note];

        if (folder is not null && !task.VisibleFolderIds.Contains(folder.Id))
            task.VisibleFolderIds.Add(folder.Id);

        await db.SaveChangesAsync(ct);
        return createdResource;
    }

    private static TaskDefinition? ResolveTask(
        IReadOnlyDictionary<string, TaskDefinition> taskByExternalId,
        IReadOnlyDictionary<string, TaskDefinition> taskByCode,
        string parentTaskId,
        string parentCode)
    {
        if (!string.IsNullOrWhiteSpace(parentTaskId) && taskByExternalId.TryGetValue(parentTaskId, out var byId))
            return byId;

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

        if (token.Contains("material"))
            return ResourceTypesEnum.Materials;

        if (token.Contains("maskin") || token.Contains("machine") || token.Contains("equipment"))
            return ResourceTypesEnum.MachinesAndEquipments;

        if (token.Contains("arbet") || token.Contains("worker") || token.Contains("personal") || token.Contains("montor"))
            return ResourceTypesEnum.Worker;

        if (token is "ue" || token.Contains("underentrepren") || token.Contains("subcontract"))
            return ResourceTypesEnum.Subcontractors;

        if (token.Contains("manager") || token.Contains("ledning"))
            return ResourceTypesEnum.Managers;

        if (token.Contains("design") || token.Contains("projektering"))
            return ResourceTypesEnum.Design;

        if (token.Contains("risk"))
            return ResourceTypesEnum.Risk;

        if (token.Contains("overhead"))
            return ResourceTypesEnum.ProjectOverheadCosts;

        result.Issues.Add(new(rowNumber, $"Unknown resource type '{value}', imported as Adjustment."));
        return ResourceTypesEnum.Adjustment;
    }

    private static IReadOnlyDictionary<string, int> BuildColumnMap(IReadOnlyList<string> header)
    {
        var aliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Radtyp"] = ["Radtyp", "RowType"],
            ["TaskId"] = ["TaskId"],
            ["ParentTaskId"] = ["ParentTaskId"],
            ["ParentCode"] = ["ParentCode"],
            ["Code"] = ["Code", "Kod"],
            ["Name"] = ["Name", "Namn"],
            ["Quantity"] = ["Mängd", "Mangd", "Quantity"],
            ["Unit"] = ["Enhet", "Unit"],
            ["Account"] = ["Konto", "Account"],
            ["ResourceType"] = ["Resurs typ", "Resurstyp", "Resource type", "ResourceType"],
            ["ResourceFolder"] = ["Resursmapp", "Resource folder", "ResourceFolder"],
            ["CapWaste"] = ["Kapacitet/Spill", "Capacity/Waste"],
            ["CapWasteType"] = ["Kapacitet/Spill typ", "Capacity/Waste type"],
            ["QuantityFactor"] = ["Mängdpåverkande faktor", "Mangdpaverkande faktor", "Quantity factor"],
            ["UnitCost"] = ["Enhetskostnad", "Unit cost"],
            ["Category"] = ["Kategori", "Category"],
            ["CalculationMethod"] = ["Beräkningssätt", "Berakningssatt", "Calculation method"]
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

    private static decimal? ParseNullableDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.GetCultureInfo("sv-SE"), out var sv))
            return sv;

        var normalized = value.Replace(',', '.');
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
        if (!string.IsNullOrWhiteSpace(fileName))
            parts.Add($"File: {fileName.Trim()}.");
        if (!string.IsNullOrWhiteSpace(account))
            parts.Add($"Account: {account.Trim()}.");
        if (!string.IsNullOrWhiteSpace(category))
            parts.Add($"Category: {category.Trim()}.");

        return string.Join(" ", parts);
    }

    private static string ResourceKey(string name, int? folderId, ResourceTypesEnum type)
        => $"{NormalizeToken(name)}|{folderId?.ToString(CultureInfo.InvariantCulture) ?? ""}|{(int)type}";

    private static string AssignmentKey(int taskId, int resourceId)
        => $"{taskId}:{resourceId}";

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

    private static int ParentSortKey(int? parentId)
        => parentId ?? 0;

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
