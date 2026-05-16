namespace ProjectManagement.Shared.Helper.Text;

/// <summary>
/// Pre-computed IDF (Inverse Document Frequency) lookup built from the task corpus.
/// Higher weight = rarer token = more distinctive for search.
/// </summary>
public sealed class TfIdfIndex
{
    private readonly Dictionary<string, double> _idf;
    private readonly double _defaultIdf;

    public TfIdfIndex(Dictionary<string, double> idf)
    {
        _idf = idf ?? new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        _defaultIdf = _idf.Count > 0 ? _idf.Values.Max() : 5.0;
    }

    public static TfIdfIndex Empty { get; } = new(new Dictionary<string, double>());

    public bool IsEmpty => _idf.Count == 0;

    /// <summary>Returns the IDF weight for a token. Unknown tokens get the maximum score (assumed very rare).</summary>
    public double GetWeight(string token)
        => _idf.TryGetValue(token, out var w) ? w : _defaultIdf;

    /// <summary>
    /// Picks the single most distinctive token from a normalized text —
    /// the best candidate for a server-side Contains search.
    /// </summary>
    public string BestToken(string normalizedText)
    {
        var tokens = normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens.Length switch
        {
            0 => string.Empty,
            1 => tokens[0],
            _ => tokens
                    .OrderByDescending(GetWeight)
                    .ThenByDescending(t => t.Length)
                    .First()
        };
    }

    /// <summary>Returns all tokens with their weights, sorted by distinctiveness descending.</summary>
    public IReadOnlyList<(string Token, double Weight)> WeighTokens(string normalizedText)
        => normalizedText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => (t, GetWeight(t)))
            .OrderByDescending(x => x.Item2)
            .ToList();
}
