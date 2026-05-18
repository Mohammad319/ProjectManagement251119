using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Helper.Text;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Infrastructure;
using TaskResourceBlueprints.Infrastructure.ConfigurationConstants;

namespace TaskResourceBlueprints.Services.Import;

public interface ITaskNameCorpusImportService
{
    Task<TaskNameCorpusImportAnalysis> AnalyzeAsync(
        Stream fileStream,
        string fileName,
        CancellationToken ct = default);

    Task<TaskNameCorpusImportResult> ImportAsync(
        Stream fileStream,
        string fileName,
        bool updateExistingTasks = false,
        CancellationToken ct = default);
}

public class TaskNameCorpusImportAnalysis
{
    public int TotalRows { get; set; }
    public int ParsedRows { get; set; }
    public int UniqueTasks { get; set; }
    public int DuplicateRows { get; set; }
    public int SkippedRows { get; set; }
    public string NameColumn { get; set; } = string.Empty;
    public string? CodeColumn { get; set; }
    public string? QuantityColumn { get; set; }
    public string? UnitColumn { get; set; }
    public List<TaskNameCorpusImportIssue> Issues { get; set; } = [];
}

public sealed class TaskNameCorpusImportResult : TaskNameCorpusImportAnalysis
{
    public int CreatedTasks { get; set; }
    public int UpdatedTasks { get; set; }
    public int ExistingTasks { get; set; }
}

public sealed record TaskNameCorpusImportIssue(int RowNumber, string Message);

public sealed record TaskNameCorpusRow(
    int RowNumber,
    string Name,
    string? Code,
    string? ParentCode,
    decimal? Quantity,
    string? UnitCode,
    IReadOnlyList<string> NameSynonyms,
    IReadOnlyList<string> UnitSynonyms);

public sealed class TaskNameCorpusImportService(IDbContextFactory<TaskResourceBlueprintsContext> dbContextFactory)
    : ITaskNameCorpusImportService
{
    public async Task<TaskNameCorpusImportAnalysis> AnalyzeAsync(
        Stream fileStream,
        string fileName,
        CancellationToken ct = default)
    {
        var (analysis, _) = await TaskNameCorpusFileReader.ReadAsync(fileStream, fileName, ct);
        return analysis;
    }

    public async Task<TaskNameCorpusImportResult> ImportAsync(
        Stream fileStream,
        string fileName,
        bool updateExistingTasks = false,
        CancellationToken ct = default)
    {
        var (analysis, rows) = await TaskNameCorpusFileReader.ReadAsync(fileStream, fileName, ct);
        var result = new TaskNameCorpusImportResult
        {
            TotalRows = analysis.TotalRows,
            ParsedRows = analysis.ParsedRows,
            UniqueTasks = analysis.UniqueTasks,
            DuplicateRows = analysis.DuplicateRows,
            SkippedRows = analysis.SkippedRows,
            NameColumn = analysis.NameColumn,
            CodeColumn = analysis.CodeColumn,
            QuantityColumn = analysis.QuantityColumn,
            UnitColumn = analysis.UnitColumn,
            Issues = [.. analysis.Issues],
        };

        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var existingTasks = await db.Tasks.ToListAsync(ct);
        var taskByCode = existingTasks
            .Where(x => !string.IsNullOrWhiteSpace(x.Code))
            .GroupBy(x => x.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var taskByNameKey = existingTasks
            .GroupBy(x => BuildNameKey(x.Name))
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var importedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var nextSortOrder = existingTasks.Select(x => (int?)x.SortOrder).Max() ?? 0;

        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();

            var key = BuildImportKey(row);
            if (!importedKeys.Add(key))
                continue;

            var existing = ResolveExistingTask(row, taskByCode, taskByNameKey);
            if (existing is not null)
            {
                result.ExistingTasks++;
                if (!updateExistingTasks)
                    continue;

                ApplyRow(existing, row, fileName, taskByCode);
                result.UpdatedTasks++;
                continue;
            }

            var task = new TaskDefinition
            {
                SortOrder = ++nextSortOrder,
                Status = TaskStatusEnum.Ready,
                IsActive = true,
                IsVisible = true,
            };
            ApplyRow(task, row, fileName, taskByCode);

            db.Tasks.Add(task);
            result.CreatedTasks++;

            if (!string.IsNullOrWhiteSpace(task.Code))
                taskByCode[task.Code.Trim()] = task;
            taskByNameKey[BuildNameKey(task.Name)] = task;
        }

        await db.SaveChangesAsync(ct);
        return result;
    }

    private static TaskDefinition? ResolveExistingTask(
        TaskNameCorpusRow row,
        IReadOnlyDictionary<string, TaskDefinition> taskByCode,
        IReadOnlyDictionary<string, TaskDefinition> taskByNameKey)
    {
        if (!string.IsNullOrWhiteSpace(row.Code) &&
            taskByCode.TryGetValue(row.Code.Trim(), out var byCode))
            return byCode;

        return taskByNameKey.TryGetValue(BuildNameKey(row.Name), out var byName)
            ? byName
            : null;
    }

    private static void ApplyRow(
        TaskDefinition task,
        TaskNameCorpusRow row,
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
        task.AdminNote = $"Imported from clean task corpus '{fileName}' row {row.RowNumber}.";
        task.ChangeFactor1 = 1m;
        task.ChangeFactor2 = 1m;
        task.RefreshNormalizedTextSv();
    }

    private static string BuildImportKey(TaskNameCorpusRow row)
        => !string.IsNullOrWhiteSpace(row.Code)
            ? $"code:{row.Code.Trim()}"
            : $"name:{BuildNameKey(row.Name)}";

    private static string BuildNameKey(string name)
        => SwedishTaskTextNormalizer.Normalize(name);

    private static string Limit(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength].Trim();

    private static List<string> CleanSynonyms(IEnumerable<string> values)
        => values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}

public static class TaskNameCorpusFileReader
{
    private static readonly HashSet<string> NameHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "name", "namn", "task", "taskname", "uppgift", "aktivitet", "arbetsmoment",
        "benamning", "rubrik", "beskrivning", "description", "atgard", "اسم", "المهمة"
    };

    private static readonly HashSet<string> CodeHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "code", "kod", "ama", "littera", "position", "pos", "taskid", "id", "رقم", "كود"
    };

    private static readonly HashSet<string> QuantityHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "quantity", "qty", "mangd", "antal", "volym", "amount", "كمية"
    };

    private static readonly HashSet<string> ParentCodeHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "parentcode", "parenttaskcode", "parenttask", "parent"
    };

    private static readonly HashSet<string> UnitHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "unit", "unitcode", "enhet", "enh", "وحدة"
    };

    public static async Task<(TaskNameCorpusImportAnalysis Analysis, IReadOnlyList<TaskNameCorpusRow> Rows)> ReadAsync(
        Stream fileStream,
        string fileName,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        var table = IsExcelFile(fileName)
            ? ReadExcelRows(fileStream)
            : await ReadTextRowsAsync(fileStream, ct);

        return AnalyzeRows(table);
    }

    private static (TaskNameCorpusImportAnalysis Analysis, IReadOnlyList<TaskNameCorpusRow> Rows) AnalyzeRows(
        IReadOnlyList<IReadOnlyList<string>> table)
    {
        var analysis = new TaskNameCorpusImportAnalysis();
        if (table.Count == 0)
            return (analysis, []);

        var columnMap = BuildColumnMap(table);
        analysis.NameColumn = columnMap.NameColumnName;
        analysis.CodeColumn = columnMap.CodeColumnName;
        analysis.QuantityColumn = columnMap.QuantityColumnName;
        analysis.UnitColumn = columnMap.UnitColumnName;

        var rows = new List<TaskNameCorpusRow>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = columnMap.FirstDataRowIndex; i < table.Count; i++)
        {
            var row = table[i];
            var rowNumber = i + 1;
            if (row.All(string.IsNullOrWhiteSpace))
                continue;

            analysis.TotalRows++;
            var name = Get(row, columnMap.NameIndex).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                analysis.SkippedRows++;
                analysis.Issues.Add(new(rowNumber, "Task name is empty."));
                continue;
            }

            var code = EmptyToNull(Get(row, columnMap.CodeIndex));
            var parentCode = EmptyToNull(Get(row, columnMap.ParentCodeIndex));
            var quantity = ParseNullableDecimal(Get(row, columnMap.QuantityIndex));
            var unitCode = EmptyToNull(Get(row, columnMap.UnitIndex));
            var parsed = new TaskNameCorpusRow(
                rowNumber,
                name,
                code,
                parentCode,
                quantity,
                unitCode,
                ReadSynonyms(row, columnMap.NameSynonym1Index, columnMap.NameSynonym2Index),
                ReadSynonyms(row, columnMap.UnitSynonym1Index, columnMap.UnitSynonym2Index));
            var key = !string.IsNullOrWhiteSpace(code)
                ? $"code:{code.Trim()}"
                : $"name:{SwedishTaskTextNormalizer.Normalize(name)}";

            if (!seenKeys.Add(key))
            {
                analysis.DuplicateRows++;
                analysis.Issues.Add(new(rowNumber, "Duplicate task in import file."));
                continue;
            }

            rows.Add(parsed);
            analysis.ParsedRows++;
        }

        analysis.UniqueTasks = rows.Count;
        return (analysis, rows);
    }

    private static TaskNameCorpusColumnMap BuildColumnMap(IReadOnlyList<IReadOnlyList<string>> table)
    {
        var firstRow = table[0];
        var normalizedHeaders = firstRow.Select(NormalizeHeader).ToList();
        var hasHeader = normalizedHeaders.Any(header =>
            NameHeaders.Contains(header) ||
            CodeHeaders.Contains(header) ||
            ParentCodeHeaders.Contains(header) ||
            QuantityHeaders.Contains(header) ||
            UnitHeaders.Contains(header));

        if (hasHeader)
        {
            var nameIndex = FindHeader(normalizedHeaders, NameHeaders);
            if (nameIndex < 0)
                nameIndex = InferNameIndex(table.Skip(1).ToList());

            var codeIndex = FindHeader(normalizedHeaders, CodeHeaders);
            var parentCodeIndex = FindHeader(normalizedHeaders, ParentCodeHeaders);
            var quantityIndex = FindHeader(normalizedHeaders, QuantityHeaders);
            var unitIndex = FindHeader(normalizedHeaders, UnitHeaders);
            var nameSynonym1Index = FindHeader(normalizedHeaders, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "tasknamesynonym1", "namesynonym1", "namnsynonym1" });
            var nameSynonym2Index = FindHeader(normalizedHeaders, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "tasknamesynonym2", "namesynonym2", "namnsynonym2" });
            var unitSynonym1Index = FindHeader(normalizedHeaders, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "taskunitsynonym1", "unitsynonym1", "enhetsynonym1" });
            var unitSynonym2Index = FindHeader(normalizedHeaders, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "taskunitsynonym2", "unitsynonym2", "enhetsynonym2" });

            return new TaskNameCorpusColumnMap(
                nameIndex,
                codeIndex,
                parentCodeIndex,
                quantityIndex,
                unitIndex,
                nameSynonym1Index,
                nameSynonym2Index,
                unitSynonym1Index,
                unitSynonym2Index,
                FirstDataRowIndex: 1,
                NameColumnName: Get(firstRow, nameIndex),
                CodeColumnName: codeIndex >= 0 ? Get(firstRow, codeIndex) : null,
                QuantityColumnName: quantityIndex >= 0 ? Get(firstRow, quantityIndex) : null,
                UnitColumnName: unitIndex >= 0 ? Get(firstRow, unitIndex) : null);
        }

        var inferredNameIndex = InferNameIndex(table);
        var inferredCodeIndex = InferCodeIndex(table, inferredNameIndex);
        var inferredQuantityIndex = InferQuantityIndex(table, inferredNameIndex, inferredCodeIndex);
        var inferredUnitIndex = InferUnitIndex(table, inferredNameIndex, inferredCodeIndex, inferredQuantityIndex);

        return new TaskNameCorpusColumnMap(
            inferredNameIndex,
            inferredCodeIndex,
            -1,
            inferredQuantityIndex,
            inferredUnitIndex,
            -1,
            -1,
            -1,
            -1,
            FirstDataRowIndex: 0,
            NameColumnName: $"Column {inferredNameIndex + 1}",
            CodeColumnName: inferredCodeIndex >= 0 ? $"Column {inferredCodeIndex + 1}" : null,
            QuantityColumnName: inferredQuantityIndex >= 0 ? $"Column {inferredQuantityIndex + 1}" : null,
            UnitColumnName: inferredUnitIndex >= 0 ? $"Column {inferredUnitIndex + 1}" : null);
    }

    private static int InferNameIndex(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var maxColumns = rows.Select(x => x.Count).DefaultIfEmpty(0).Max();
        if (maxColumns == 0)
            return 0;

        var bestIndex = 0;
        var bestScore = double.MinValue;
        for (var index = 0; index < maxColumns; index++)
        {
            var values = rows.Select(row => Get(row, index).Trim()).Where(x => x.Length > 0).Take(50).ToList();
            if (values.Count == 0)
                continue;

            var avgLength = values.Average(x => x.Length);
            var codeRatio = values.Count(LooksLikeCode) / (double)values.Count;
            var numericRatio = values.Count(x => ParseNullableDecimal(x).HasValue) / (double)values.Count;
            var score = avgLength - (codeRatio * 20d) - (numericRatio * 30d);

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = index;
            }
        }

        return bestIndex;
    }

    private static int InferCodeIndex(IReadOnlyList<IReadOnlyList<string>> rows, int nameIndex)
        => InferByRatio(rows, nameIndex, new HashSet<int>(), LooksLikeCode, minimumRatio: 0.45d);

    private static int InferQuantityIndex(IReadOnlyList<IReadOnlyList<string>> rows, int nameIndex, int codeIndex)
        => InferByRatio(rows, nameIndex, new HashSet<int> { codeIndex }, value => ParseNullableDecimal(value).HasValue, minimumRatio: 0.55d);

    private static int InferUnitIndex(IReadOnlyList<IReadOnlyList<string>> rows, int nameIndex, int codeIndex, int quantityIndex)
        => InferByRatio(rows, nameIndex, new HashSet<int> { codeIndex, quantityIndex }, LooksLikeUnit, minimumRatio: 0.45d);

    private static int InferByRatio(
        IReadOnlyList<IReadOnlyList<string>> rows,
        int nameIndex,
        IReadOnlySet<int> excludedIndexes,
        Func<string, bool> predicate,
        double minimumRatio)
    {
        var maxColumns = rows.Select(x => x.Count).DefaultIfEmpty(0).Max();
        var bestIndex = -1;
        var bestRatio = 0d;
        for (var index = 0; index < maxColumns; index++)
        {
            if (index == nameIndex || excludedIndexes.Contains(index))
                continue;

            var values = rows.Select(row => Get(row, index).Trim()).Where(x => x.Length > 0).Take(50).ToList();
            if (values.Count == 0)
                continue;

            var ratio = values.Count(predicate) / (double)values.Count;
            if (ratio > bestRatio)
            {
                bestRatio = ratio;
                bestIndex = index;
            }
        }

        return bestRatio >= minimumRatio ? bestIndex : -1;
    }

    private static int FindHeader(IReadOnlyList<string> normalizedHeaders, IReadOnlySet<string> knownHeaders)
    {
        for (var index = 0; index < normalizedHeaders.Count; index++)
        {
            if (knownHeaders.Contains(normalizedHeaders[index]))
                return index;
        }

        return -1;
    }

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
            var values = row.Cells(1, range.ColumnCount())
                .Select(cell => cell.GetFormattedString().Trim())
                .ToList();
            rows.Add(values);
        }

        return rows;
    }

    private static char DetectDelimiter(IReadOnlyList<string> lines)
    {
        var sample = lines.Take(10).ToList();
        var candidates = new[] { ';', '\t', ',' };
        return candidates
            .Select(delimiter => new
            {
                Delimiter = delimiter,
                Score = sample.Sum(line => ParseDelimitedLine(line, delimiter).Count)
            })
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
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

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

    private static bool LooksLikeCode(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length is < 2 or > 24)
            return false;

        return trimmed.Any(char.IsDigit) &&
            Regex.IsMatch(trimmed, @"^[A-Za-zÅÄÖåäö0-9._\- /]+$") &&
            (trimmed.Contains('.') || trimmed.Contains('-') || trimmed.Any(char.IsLetter));
    }

    private static bool LooksLikeUnit(string value)
    {
        var normalized = NormalizeHeader(value);
        return normalized is "m" or "m2" or "m3" or "st" or "kg" or "ton" or "h" or "tim" or "dag" or "vecka"
            or "lm" or "kvm" or "kbm" or "pcs";
    }

    private sealed record TaskNameCorpusColumnMap(
        int NameIndex,
        int CodeIndex,
        int ParentCodeIndex,
        int QuantityIndex,
        int UnitIndex,
        int NameSynonym1Index,
        int NameSynonym2Index,
        int UnitSynonym1Index,
        int UnitSynonym2Index,
        int FirstDataRowIndex,
        string NameColumnName,
        string? CodeColumnName,
        string? QuantityColumnName,
        string? UnitColumnName);
}
