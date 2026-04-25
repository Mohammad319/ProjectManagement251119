using System.Text.RegularExpressions;
using ProjectManagement.Shared.Constant;

namespace ProjectManagement.Shared.Helper.Text;

public static partial class SwedishTaskTextNormalizer
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "för", "med", "och", "av", "till", "på", "i", "inom", "vid", "utan",
        "inkl", "inklusive", "samt", "en", "ett", "den", "det", "de",
        "arbete", "arbeten", "utförande"
    };

    private static readonly Dictionary<string, string> PhraseMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["grundschaktning"] = "grund schakt",
        ["grundschakt"] = "grund schakt",
        ["rörgravsschakt"] = "rörgrav schakt",
        ["betonggjutning"] = "betong gjut",
        ["armeringsarbete"] = "armering",
        ["schakt för grund"] = "grund schakt",
        ["schaktning för grund"] = "grund schakt",
        ["schaktning för grunder"] = "grund schakt"
    };

    private static readonly Dictionary<string, string> TokenMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["schaktning"] = "schakt",
        ["schakta"] = "schakt",
        ["utgrävning"] = "schakt",
        ["grävning"] = "schakt",

        ["grunder"] = "grund",
        ["fundament"] = "grund",
        ["fundamenten"] = "grund",

        ["gjutning"] = "gjut",
        ["gjuta"] = "gjut",
        ["gjuten"] = "gjut",

        ["armerings"] = "armering",
        ["armeringsjärn"] = "armering",
        ["armeringar"] = "armering",

        ["väggar"] = "vägg",
        ["plattor"] = "platta",
        ["ledningar"] = "ledning",
        ["rören"] = "rör",
        ["rörledningar"] = "rör",
        ["kablar"] = "kabel",

        ["m2"] = "m²",
        ["kvm"] = "m²",
        ["m3"] = "m³",
        ["kbm"] = "m³",
        ["st"] = "styck",
        ["st."] = "styck",
        ["inkl."] = "inkl",
        ["ca."] = "ca"
    };

    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var text = input.Trim().ToLowerInvariant();

        text = text.Replace("/", " ");
        text = text.Replace("-", " ");

        text = NonSearchCharactersRegex().Replace(text, " ");
        text = WhitespaceRegex().Replace(text, " ").Trim();

        foreach (var kv in PhraseMap.OrderByDescending(x => x.Key.Length))
        {
            text = Regex.Replace(
                text,
                $@"\b{Regex.Escape(kv.Key.ToLowerInvariant())}\b",
                kv.Value.ToLowerInvariant(),
                RegexOptions.IgnoreCase);
        }

        var tokens = new List<string>();
        var rawTokens = text.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var rawToken in rawTokens)
        {
            var token = rawToken.Trim();

            if (string.IsNullOrWhiteSpace(token) || StopWords.Contains(token))
                continue;

            if (TokenMap.TryGetValue(token, out var mapped))
                token = mapped;

            token = NormalizeToken(token);

            if (string.IsNullOrWhiteSpace(token) || StopWords.Contains(token))
                continue;

            tokens.Add(token);
        }

        var normalized = string.Join(
            " ",
            tokens
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

        return normalized.Length <= FieldLengths.NormalizedText
            ? normalized
            : normalized[..FieldLengths.NormalizedText].Trim();
    }

    public static string NormalizeTask(string? name, string? code, string? unit, object? taskType = null)
    {
        var parts = new[]
        {
            code,
            name,
            unit,
            taskType?.ToString()
        };

        return Normalize(string.Join(' ', parts.Where(x => !string.IsNullOrWhiteSpace(x))));
    }

    public static double CalculateSimilarity(string? leftNormalizedText, string? rightNormalizedText)
    {
        if (string.IsNullOrWhiteSpace(leftNormalizedText) || string.IsNullOrWhiteSpace(rightNormalizedText))
            return 0d;

        if (string.Equals(leftNormalizedText, rightNormalizedText, StringComparison.OrdinalIgnoreCase))
            return 1d;

        var leftTokens = Tokenize(leftNormalizedText);
        var rightTokens = Tokenize(rightNormalizedText);

        if (leftTokens.Count == 0 || rightTokens.Count == 0)
            return 0d;

        var intersectionCount = leftTokens.Intersect(rightTokens, StringComparer.OrdinalIgnoreCase).Count();
        var unionCount = leftTokens.Union(rightTokens, StringComparer.OrdinalIgnoreCase).Count();

        var jaccard = unionCount == 0 ? 0d : (double)intersectionCount / unionCount;
        var coverage = (double)intersectionCount / Math.Min(leftTokens.Count, rightTokens.Count);
        var containsBonus =
            leftNormalizedText.Contains(rightNormalizedText, StringComparison.OrdinalIgnoreCase) ||
            rightNormalizedText.Contains(leftNormalizedText, StringComparison.OrdinalIgnoreCase)
                ? 0.10d
                : 0d;

        return Math.Round(Math.Min((0.60d * jaccard) + (0.30d * coverage) + (0.10d * containsBonus), 1d), 4);
    }

    private static string NormalizeToken(string token)
    {
        if (token.Length <= 2)
            return token;

        if (token.EndsWith("arna", StringComparison.OrdinalIgnoreCase) && token.Length > 6)
            token = token[..^4];
        else if (token.EndsWith("erna", StringComparison.OrdinalIgnoreCase) && token.Length > 6)
            token = token[..^4];
        else if (token.EndsWith("ar", StringComparison.OrdinalIgnoreCase) && token.Length > 5)
            token = token[..^2];
        else if (token.EndsWith("er", StringComparison.OrdinalIgnoreCase) && token.Length > 5)
            token = token[..^2];
        else if (token.EndsWith("or", StringComparison.OrdinalIgnoreCase) && token.Length > 5)
            token = token[..^2];
        else if (token.EndsWith("en", StringComparison.OrdinalIgnoreCase) && token.Length > 5)
            token = token[..^2];
        else if (token.EndsWith("et", StringComparison.OrdinalIgnoreCase) && token.Length > 5)
            token = token[..^2];

        return TokenMap.TryGetValue(token, out var mapped)
            ? mapped.Trim()
            : token.Trim();
    }

    private static HashSet<string> Tokenize(string normalizedText) =>
        normalizedText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    [GeneratedRegex(@"[^\p{L}\p{N}\s²³]")]
    private static partial Regex NonSearchCharactersRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
