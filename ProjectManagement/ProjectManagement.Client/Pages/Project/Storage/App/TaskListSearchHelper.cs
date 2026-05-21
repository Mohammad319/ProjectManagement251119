using ProjectManagement.Shared.Helper.Text;

namespace ProjectManagement.Client.Pages.Project.Storage.App;

internal static class TaskListSearchHelper
{
    public static TaskListSearchResult BuildResult(TaskListSearchInput group, string query)
    {
        var badges = new List<TaskListSearchBadge>();
        if (string.IsNullOrWhiteSpace(query))
            return new TaskListSearchResult(1.0, badges, null);

        // Normalize query once — reused for all scoring below.
        var normalizedQuery = SwedishTaskTextNormalizer.Normalize(query);
        var score = FuzzySearchHelper.ScoreNormalized(normalizedQuery, group.NormalizedCombinedText);
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

        var normalizedDisplayName = SwedishTaskTextNormalizer.Normalize(group.DisplayName);
        var nameScore = FuzzySearchHelper.ScoreNormalized(normalizedQuery, normalizedDisplayName);
        if (nameScore >= FuzzySearchHelper.Threshold)
        {
            badges.Add(new($"Name {nameScore.ToString("0%", System.Globalization.CultureInfo.InvariantCulture)}", "name"));
            matchHint ??= $"Name: {group.DisplayName}";
        }

        var resourceScore = FuzzySearchHelper.ScoreNormalized(normalizedQuery, group.NormalizedResourceText);
        var matchedResource = FindMatchedResourceText(group.ResourceSearchText, normalizedQuery);
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

    public static double GetTokenBonus(string normalizedCombinedText, IReadOnlyList<string> tokens)
    {
        if (tokens.Count < 2)
            return 0;

        var t0 = tokens[0];
        var t1 = tokens[1];
        var has0 = normalizedCombinedText.Contains(t0, StringComparison.OrdinalIgnoreCase);
        var has1 = normalizedCombinedText.Contains(t1, StringComparison.OrdinalIgnoreCase);
        return has0 && has1 ? 0.15 : 0;
    }

    private static string? FindMatchedResourceText(string resourceSearchText, string normalizedQuery)
    {
        if (string.IsNullOrWhiteSpace(normalizedQuery) || string.IsNullOrWhiteSpace(resourceSearchText))
            return null;

        return resourceSearchText
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(text =>
            {
                var normalizedText = SwedishTaskTextNormalizer.Normalize(text);
                return new
                {
                    Text = text,
                    Score = FuzzySearchHelper.ScoreNormalized(normalizedQuery, normalizedText),
                    Contains = normalizedText.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                };
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
    string NormalizedCombinedText,
    string ResourceSearchText,
    string NormalizedResourceText,
    bool HasStates,
    bool HasResources);

internal sealed record TaskListSearchBadge(string Label, string Kind);

internal sealed record TaskListSearchResult(
    double Score,
    List<TaskListSearchBadge> Badges,
    string? MatchHint);
