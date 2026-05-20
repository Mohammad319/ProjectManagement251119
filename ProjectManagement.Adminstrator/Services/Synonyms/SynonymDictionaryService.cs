using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Helper.Text;
using System.Text;
using TaskResourceBlueprints.Infrastructure;
using TaskResourceBlueprints.Services.ProjectTask;

namespace ProjectManagement.Adminstrator.Services.Synonyms;

public interface ISynonymDictionaryService
{
    string DictionaryPath { get; }
    Task<IReadOnlyList<SynonymDictionaryEntry>> GetEntriesAsync(CancellationToken ct = default);
    Task<SynonymDictionarySaveResult> SaveEntryAsync(string source, string target, CancellationToken ct = default);
    Task<bool> DeleteEntryAsync(string source, CancellationToken ct = default);
    Task<SynonymDictionaryImportResult> ImportAsync(Stream stream, bool replaceExisting, CancellationToken ct = default);
    Task<string> ExportAsync(CancellationToken ct = default);
    Task<int> ReloadAsync(CancellationToken ct = default);
    Task<int> RebuildSearchIndexAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SynonymMissingTokenItem>> GetMissingTokensAsync(int maxItems = 30, CancellationToken ct = default);
    Task<IReadOnlyList<SynonymMissingTokenTaskItem>> GetMissingTokenTasksAsync(string token, int maxItems = 500, CancellationToken ct = default);
    Task<IReadOnlyList<ImportedTaskSynonymItem>> GetImportedTaskSynonymsAsync(int maxItems = 500, CancellationToken ct = default);
}

public sealed record SynonymDictionaryEntry(int RowNumber, string Source, string Target);

public sealed record SynonymDictionarySaveResult(bool Saved, string Message, int TotalEntries);

public sealed record SynonymDictionaryImportResult(int Imported, int Skipped, int TotalEntries);

public sealed record SynonymMissingTokenItem(string Token, int Count, IReadOnlyList<string> Examples);

public sealed record SynonymMissingTokenTaskItem(
    int Id,
    string Code,
    string Name,
    string Unit,
    string HierarchyPath);

public sealed record ImportedTaskSynonymItem(
    int TaskId,
    string TaskCode,
    string TaskName,
    string Synonym,
    string Target,
    string Kind);

public sealed class SynonymDictionaryService(
    IDbContextFactory<TaskResourceBlueprintsContext> blueprintDbFactory,
    ITaskDefinitionService taskDefinitionService)
    : ISynonymDictionaryService
{
    private static readonly HashSet<string> IgnoredMissingTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "task", "tenanttask", "typ", "klass", "fall", "minsta", "galler",
        "m2", "m3", "st", "kg", "mm", "cm", "meter", "kvm", "kbm"
    };

    public string DictionaryPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ProjectManagement",
        "ML",
        "task-synonyms.csv");

    public async Task<IReadOnlyList<SynonymDictionaryEntry>> GetEntriesAsync(CancellationToken ct = default)
    {
        await EnsureFileAsync(ct);
        var lines = await File.ReadAllLinesAsync(DictionaryPath, ct);
        return ParseEntries(lines).ToList();
    }

    public async Task<SynonymDictionarySaveResult> SaveEntryAsync(string source, string target, CancellationToken ct = default)
    {
        source = NormalizePart(source);
        target = NormalizePart(target);
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
            return new SynonymDictionarySaveResult(false, "Source and target are required.", 0);

        var entries = (await GetEntriesAsync(ct)).ToList();
        var existing = entries.FindIndex(x => string.Equals(x.Source, source, StringComparison.OrdinalIgnoreCase));
        if (existing >= 0)
            entries[existing] = entries[existing] with { Target = target };
        else
            entries.Add(new SynonymDictionaryEntry(entries.Count + 1, source, target));

        await WriteEntriesAsync(entries, ct);
        var total = SwedishTaskTextNormalizer.ReloadExternalSynonyms();
        return new SynonymDictionarySaveResult(true, existing >= 0 ? "Updated." : "Added.", total);
    }

    public async Task<bool> DeleteEntryAsync(string source, CancellationToken ct = default)
    {
        source = NormalizePart(source);
        var entries = (await GetEntriesAsync(ct))
            .Where(x => !string.Equals(x.Source, source, StringComparison.OrdinalIgnoreCase))
            .ToList();

        await WriteEntriesAsync(entries, ct);
        SwedishTaskTextNormalizer.ReloadExternalSynonyms();
        return true;
    }

    public async Task<SynonymDictionaryImportResult> ImportAsync(Stream stream, bool replaceExisting, CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var importedRows = ParseEntries((await reader.ReadToEndAsync(ct)).Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            .ToList();

        var imported = 0;
        var skipped = 0;
        var entries = replaceExisting ? [] : (await GetEntriesAsync(ct)).ToList();
        var bySource = entries.ToDictionary(x => x.Source, x => x.Target, StringComparer.OrdinalIgnoreCase);

        foreach (var row in importedRows)
        {
            if (string.IsNullOrWhiteSpace(row.Source) || string.IsNullOrWhiteSpace(row.Target))
            {
                skipped++;
                continue;
            }

            bySource[row.Source] = row.Target;
            imported++;
        }

        var merged = bySource
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select((x, index) => new SynonymDictionaryEntry(index + 1, x.Key, x.Value))
            .ToList();

        await WriteEntriesAsync(merged, ct);
        var total = SwedishTaskTextNormalizer.ReloadExternalSynonyms();
        return new SynonymDictionaryImportResult(imported, skipped, total);
    }

    public async Task<string> ExportAsync(CancellationToken ct = default)
    {
        var entries = await GetEntriesAsync(ct);
        var directory = Path.Combine(Path.GetDirectoryName(DictionaryPath)!, "exports");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"task-synonyms-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        await File.WriteAllLinesAsync(
            path,
            new[] { "Source;Target" }.Concat(entries.Select(x => $"{Escape(x.Source)};{Escape(x.Target)}")),
            Encoding.UTF8,
            ct);
        return path;
    }

    public Task<int> ReloadAsync(CancellationToken ct = default)
        => Task.FromResult(SwedishTaskTextNormalizer.ReloadExternalSynonyms());

    public async Task<int> RebuildSearchIndexAsync(CancellationToken ct = default)
    {
        SwedishTaskTextNormalizer.ReloadExternalSynonyms();
        return await taskDefinitionService.RebuildNormalizedTextAsync(ct);
    }

    public async Task<IReadOnlyList<SynonymMissingTokenItem>> GetMissingTokensAsync(int maxItems = 30, CancellationToken ct = default)
    {
        maxItems = Math.Clamp(maxItems, 1, 100);
        var knownDictionaryTokens = (await GetEntriesAsync(ct))
            .SelectMany(x => new[] { x.Source, x.Target })
            .SelectMany(x => SwedishTaskTextNormalizer.ExtractNormalizedTokens(x))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        await using var db = await blueprintDbFactory.CreateDbContextAsync(ct);
        var tasks = await LeafTasksWithoutResources(db)
            .Select(x => new { x.Name, x.Code, x.UnitCode, x.Quantity })
            .Take(10000)
            .ToListAsync(ct);

        return tasks
            .Select(task => new
            {
                task.Name,
                Tokens = SwedishTaskTextNormalizer.ExtractNormalizedTokens(
                    SwedishTaskTextNormalizer.NormalizeTask(task.Name, task.Code, task.UnitCode, task.Quantity))
            })
            .SelectMany(row => row.Tokens
                .Where(token => token.Length >= 4 &&
                                !char.IsDigit(token[0]) &&
                                !IgnoredMissingTokens.Contains(token) &&
                                !knownDictionaryTokens.Contains(token))
                .Select(token => new { Token = token, row.Name }))
            .GroupBy(x => x.Token, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(x => x.Count())
            .Take(maxItems)
            .Select(group => new SynonymMissingTokenItem(
                group.Key,
                group.Count(),
                group.Select(x => x.Name)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3)
                    .ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<SynonymMissingTokenTaskItem>> GetMissingTokenTasksAsync(
        string token,
        int maxItems = 500,
        CancellationToken ct = default)
    {
        token = token.Trim();
        if (string.IsNullOrWhiteSpace(token))
            return [];

        maxItems = Math.Clamp(maxItems, 1, 2000);
        var normalizedToken = SwedishTaskTextNormalizer.Normalize(token);

        await using var db = await blueprintDbFactory.CreateDbContextAsync(ct);
        var broadMatches = await LeafTasksWithoutResources(db)
            .Where(x =>
                x.Name.Contains(token) ||
                (x.Code ?? string.Empty).Contains(token) ||
                (x.HierarchyPath ?? string.Empty).Contains(token) ||
                x.NormalizedTextSv.Contains(normalizedToken))
            .OrderBy(x => x.Code)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.UnitCode,
                x.HierarchyPath,
                x.NormalizedTextSv
            })
            .Take(maxItems * 3)
            .ToListAsync(ct);

        return broadMatches
            .Where(x => SwedishTaskTextNormalizer
                .ExtractNormalizedTokens(string.IsNullOrWhiteSpace(x.NormalizedTextSv)
                    ? SwedishTaskTextNormalizer.NormalizeTask(x.Name, x.Code, x.UnitCode)
                    : x.NormalizedTextSv)
                .Contains(normalizedToken, StringComparer.OrdinalIgnoreCase))
            .Take(maxItems)
            .Select(x => new SynonymMissingTokenTaskItem(
                x.Id,
                x.Code ?? string.Empty,
                x.Name,
                x.UnitCode ?? string.Empty,
                x.HierarchyPath ?? string.Empty))
            .ToList();
    }

    public async Task<IReadOnlyList<ImportedTaskSynonymItem>> GetImportedTaskSynonymsAsync(
        int maxItems = 500,
        CancellationToken ct = default)
    {
        maxItems = Math.Clamp(maxItems, 1, 2000);
        var promotedEntries = (await GetEntriesAsync(ct))
            .Select(x => $"{NormalizePart(x.Source)}|{NormalizePart(x.Target)}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        await using var db = await blueprintDbFactory.CreateDbContextAsync(ct);
        var tasks = await db.Tasks
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.UnitCode,
                x.NameSynonyms,
                x.UnitSynonyms
            })
            .ToListAsync(ct);

        return tasks
            .Where(task => task.NameSynonyms.Count > 0 || task.UnitSynonyms.Count > 0)
            .SelectMany(task =>
                task.NameSynonyms
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(synonym => new ImportedTaskSynonymItem(
                        task.Id,
                        task.Code ?? string.Empty,
                        task.Name,
                        NormalizePart(synonym),
                        NormalizePart(task.Name),
                        "Task name"))
                .Concat(task.UnitSynonyms
                    .Where(x => !string.IsNullOrWhiteSpace(x) && !string.IsNullOrWhiteSpace(task.UnitCode))
                    .Select(synonym => new ImportedTaskSynonymItem(
                        task.Id,
                        task.Code ?? string.Empty,
                        task.Name,
                        NormalizePart(synonym),
                        NormalizePart(task.UnitCode ?? string.Empty),
                        "Task unit"))))
            .Where(x => !string.IsNullOrWhiteSpace(x.Synonym) &&
                        !string.IsNullOrWhiteSpace(x.Target) &&
                        !string.Equals(x.Synonym, x.Target, StringComparison.OrdinalIgnoreCase) &&
                        !promotedEntries.Contains($"{x.Synonym}|{x.Target}"))
            .GroupBy(x => $"{x.Kind}|{x.Synonym}|{x.Target}", StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.Kind)
            .ThenBy(x => x.Synonym, StringComparer.OrdinalIgnoreCase)
            .Take(maxItems)
            .ToList();
    }

    private static IQueryable<TaskResourceBlueprints.Entities.Tasks.TaskDefinition> LeafTasksWithoutResources(
        TaskResourceBlueprintsContext db)
        => db.Tasks
            .AsNoTracking()
            .Where(x => x.IsActive &&
                        !x.ResourceLinks.Any() &&
                        !db.Tasks.Any(child =>
                            child.IsActive &&
                            child.HierarchyPath != null &&
                            child.HierarchyPath.StartsWith(
                                ((x.HierarchyPath == null || x.HierarchyPath == string.Empty)
                                    ? ((x.Code ?? string.Empty) + " " + x.Name)
                                    : x.HierarchyPath) + " >")));

    private async Task EnsureFileAsync(CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(DictionaryPath)!;
        Directory.CreateDirectory(directory);
        if (!File.Exists(DictionaryPath))
            await File.WriteAllTextAsync(DictionaryPath, "Source;Target" + Environment.NewLine, Encoding.UTF8, ct);
    }

    private async Task WriteEntriesAsync(IReadOnlyList<SynonymDictionaryEntry> entries, CancellationToken ct)
    {
        var lines = new[] { "Source;Target" }
            .Concat(entries
                .Where(x => !string.IsNullOrWhiteSpace(x.Source) && !string.IsNullOrWhiteSpace(x.Target))
                .OrderBy(x => x.Source, StringComparer.OrdinalIgnoreCase)
                .Select(x => $"{Escape(x.Source)};{Escape(x.Target)}"));

        var directory = Path.GetDirectoryName(DictionaryPath)!;
        Directory.CreateDirectory(directory);
        await File.WriteAllLinesAsync(DictionaryPath, lines, Encoding.UTF8, ct);
    }

    private static IEnumerable<SynonymDictionaryEntry> ParseEntries(IEnumerable<string> lines)
    {
        var row = 0;
        foreach (var line in lines)
        {
            row++;
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                continue;

            var separator = trimmed.Contains(';') ? ';' : ',';
            var parts = trimmed.Split(separator, 2, StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
                continue;

            if (row == 1 &&
                string.Equals(parts[0], "Source", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(parts[1], "Target", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var source = NormalizePart(Unescape(parts[0]));
            var target = NormalizePart(Unescape(parts[1]));
            if (!string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(target))
                yield return new SynonymDictionaryEntry(row, source, target);
        }
    }

    private static string NormalizePart(string value)
        => value.Trim().ToLowerInvariant();

    private static string Escape(string value)
        => value.Contains(';') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    private static string Unescape(string value)
    {
        value = value.Trim();
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            return value[1..^1].Replace("\"\"", "\"");
        return value;
    }
}
