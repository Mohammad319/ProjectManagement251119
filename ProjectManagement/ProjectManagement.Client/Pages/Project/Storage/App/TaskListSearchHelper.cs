using ProjectManagement.Shared.Helper.Text;

namespace ProjectManagement.Client.Pages.Project.Storage.App;

internal static class TaskListSearchHelper
{
    public static TaskListSearchResult BuildResult(TaskListSearchInput group, string query)
    {
        var badges = new List<TaskListSearchBadge>();
        if (string.IsNullOrWhiteSpace(query))
        {
            badges.Add(new("Loaded", "neutral"));
            return new TaskListSearchResult(1.0, badges, null);
        }

        var score = FuzzySearchHelper.Score(query, group.CombinedText);
        var normalizedQuery = SwedishTaskTextNormalizer.Normalize(query);
        string? matchHint = null;

        if (!string.IsNullOrWhiteSpace(group.Code))
        {
            if (group.Code.Equals(query, StringComparison.OrdinalIgnoreCase))
            {
                badges.Add(new("Code exact", "code"));
                matchHint = $"Code: {group.Code}";
                score = 1.0;
            }
            else if (group.Code.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            {
                badges.Add(new("Code prefix", "code"));
                matchHint = $"Code: {group.Code}";
                score = Math.Max(score, 0.94);
            }
            else if (group.Code.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                badges.Add(new("Code match", "code"));
                matchHint = $"Code: {group.Code}";
                score = Math.Max(score, 0.86);
            }
        }

        var nameScore = FuzzySearchHelper.Score(query, group.DisplayName);
        if (nameScore >= FuzzySearchHelper.Threshold)
        {
            badges.Add(new($"Name {nameScore.ToString("0%", System.Globalization.CultureInfo.InvariantCulture)}", "name"));
            matchHint ??= $"Name: {group.DisplayName}";
        }

        var resourceScore = FuzzySearchHelper.Score(query, group.ResourceSearchText);
        var matchedResource = FindMatchedResourceText(group.ResourceSearchText, query);
        if (!string.IsNullOrWhiteSpace(matchedResource) ||
            (!string.IsNullOrWhiteSpace(normalizedQuery) &&
             group.NormalizedResourceText.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)))
        {
            badges.Add(new("Resource match", "resource"));
            score = Math.Max(score, Math.Max(0.82, resourceScore));
            matchHint = $"Resource: {matchedResource ?? group.ResourceSearchText}";
        }

        if (group.HasStates)
            badges.Add(new("States", "state"));

        if (group.HasResources)
            badges.Add(new("Resources", "resource"));

        return new TaskListSearchResult(Math.Min(1.0, score), badges, matchHint);
    }

    public static double GetTokenBonus(string combinedText, IReadOnlyList<string> tokens)
    {
        if (tokens.Count < 2)
            return 0;

        var lower = combinedText.ToLowerInvariant();
        var normalizedCombined = SwedishTaskTextNormalizer.Normalize(combinedText);
        var t0 = tokens[0].ToLowerInvariant();
        var t1 = tokens[1].ToLowerInvariant();
        var has0 = normalizedCombined.Contains(t0) || lower.Contains(t0);
        var has1 = normalizedCombined.Contains(t1) || lower.Contains(t1);
        return has0 && has1 ? 0.15 : 0;
    }

    private static string? FindMatchedResourceText(string resourceSearchText, string query)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(resourceSearchText))
            return null;

        var normalizedQuery = SwedishTaskTextNormalizer.Normalize(query);
        return resourceSearchText
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(text => new
            {
                Text = text,
                Score = FuzzySearchHelper.Score(query, text),
                Contains = !string.IsNullOrWhiteSpace(normalizedQuery) &&
                           SwedishTaskTextNormalizer.Normalize(text).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
            })
            .Where(x => x.Contains || x.Score >= FuzzySearchHelper.Threshold)
            .OrderByDescending(x => x.Contains)
            .ThenByDescending(x => x.Score)
            .Select(x => x.Text)
            .FirstOrDefault();
    }
}

internal sealed record TaskListSearchInput(
    string DisplayName,
    string Code,
    string CombinedText,
    string ResourceSearchText,
    string NormalizedResourceText,
    bool HasStates,
    bool HasResources);

internal sealed record TaskListSearchBadge(string Label, string Kind);

internal sealed record TaskListSearchResult(
    double Score,
    List<TaskListSearchBadge> Badges,
    string? MatchHint);
