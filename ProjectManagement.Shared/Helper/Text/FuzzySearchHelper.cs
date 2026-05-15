namespace ProjectManagement.Shared.Helper.Text;

/// <summary>
/// Unified client-side fuzzy search helper for search-box filtering.
/// Delegates to SwedishTaskTextNormalizer for language-aware normalization and scoring,
/// which handles Swedish construction terms, AMA codes, stemming, and stop words.
/// </summary>
public static class FuzzySearchHelper
{
    /// <summary>
    /// Minimum score (0–1) for a result to be shown.
    /// Lower than server-side thresholds because partial queries are expected from the search box.
    /// </summary>
    public const double Threshold = 0.20;

    /// <summary>
    /// Scores a search query against a target string using Swedish-aware normalization.
    /// Returns 1.0 when the query is empty (show all results).
    /// Returns 0.0 when the target is empty.
    /// </summary>
    public static double Score(string query, string target)
    {
        if (string.IsNullOrWhiteSpace(query)) return 1.0;
        if (string.IsNullOrWhiteSpace(target)) return 0.0;

        var q = SwedishTaskTextNormalizer.Normalize(query);
        var t = SwedishTaskTextNormalizer.Normalize(target);

        // Query collapsed to nothing (only stop words typed) → show all
        if (string.IsNullOrWhiteSpace(q)) return 1.0;
        if (string.IsNullOrWhiteSpace(t)) return 0.0;

        return SwedishTaskTextNormalizer.CalculateSimilarity(q, t);
    }

    public static string BarColorClass(double score) => score switch
    {
        >= 0.70 => "bg-green-500",
        >= 0.40 => "bg-yellow-400",
        _       => "bg-orange-400"
    };

    public static string TextColorClass(double score) => score switch
    {
        >= 0.70 => "text-green-600 dark:text-green-400",
        >= 0.40 => "text-yellow-600 dark:text-yellow-400",
        _       => "text-orange-500 dark:text-orange-400"
    };
}
