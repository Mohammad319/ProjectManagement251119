using System.Text.RegularExpressions;
using ProjectManagement.Shared.Constant;

namespace ProjectManagement.Shared.Helper.Text;

public static partial class SwedishTaskTextNormalizer
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Swedish function words
        "för", "med", "och", "av", "till", "på", "i", "inom", "vid", "utan",
        "en", "ett", "den", "det", "de", "som", "är", "har", "kan", "ska",
        "från", "efter", "under", "över", "mot", "hos", "per",
        // AMA abbreviations (expanded)
        "inkl", "inklusive", "exkl", "exklusive", "samt", "alt",
        "resp", "dvs", "bl", "bla", "ca", "typ", "enl", "enligt",
        // Generic construction filler
        "arbete", "arbeten", "utförande", "åtgärd", "åtgärder",
        // Single-letter abbreviation fragments (from "m m", "o d", "e d")
        "m", "o", "e",
        // Generic English/multilingual words common in material price lists
        "standard", "normal", "general", "type", "quality", "class", "grade",
        "product", "material", "item", "article", "unit", "piece", "set",
        "new", "used", "various", "other", "misc", "general",
        // Dutch/German filler common in European supplier catalogues
        "van", "de", "het", "voor", "met", "und", "der", "die", "das",
        "für", "mit", "von"
    };

    private static readonly Dictionary<string, string> PhraseMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Earthworks
        ["grundschaktning"]              = "grund schakt",
        ["grundschakt"]                  = "grund schakt",
        ["rörgravsschakt"]               = "rörgrav schakt",
        ["jordschakt för grundläggning"] = "jordschakt grundläggning",
        ["schakt för grund"]             = "grund schakt",
        ["schaktning för grund"]         = "grund schakt",
        ["schaktning för grunder"]       = "grund schakt",
        ["schakt för ledning"]           = "schakt ledning",
        ["schakt för rör"]               = "schakt rör",
        ["dränerande lager"]             = "dränering lager",
        ["dränerande material"]          = "dränering material",
        ["tillfällig grundvattensänkning"] = "grundvatten sänkning",
        ["tillfällig grundvattenhöjning"]  = "grundvatten höjning",
        ["tillfällig väg"]               = "väg",
        // Concrete
        ["betonggjutning"]               = "betong gjut",
        ["armeringsarbete"]              = "armering",
        ["platsgjuten betong"]           = "betong gjut",
        ["platsgjutna konstruktioner"]   = "betong gjut konstruktion",
        // Road surfaces
        ["beläggning av betongmarksten"] = "belägg betong marksten",
        ["beläggning av smågatsten"]     = "belägg gatsten",
        ["beläggning av storgatsten"]    = "belägg gatsten",
        ["återställande av"]             = "återställ",
        // Pipes
        ["dagvattenledning"]             = "dagvatten ledning",
        ["vattenledning"]                = "vatten ledning",
        ["avloppsledning"]               = "avlopp ledning",
        ["spillvattenledning"]           = "spillvatten ledning",
    };

    private static readonly Dictionary<string, string> TokenMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Schakt / Excavation ──────────────────────────────────────────
        ["schaktning"]         = "schakt",
        ["schakta"]            = "schakt",
        ["utgrävning"]         = "schakt",
        ["grävning"]           = "schakt",
        ["jordschaktning"]     = "jordschakt",
        ["bergschaktning"]     = "bergschakt",
        ["markschaktning"]     = "schakt",
        ["rörgrävning"]        = "rörgrav",
        ["rörgravsschaktning"] = "rörgrav schakt",
        ["rörgravar"]          = "rörgrav",

        // ── Grund / Foundation ───────────────────────────────────────────
        ["grunder"]            = "grund",
        ["fundament"]          = "grund",
        ["fundamenten"]        = "grund",
        ["grundläggning"]      = "grundlägg",
        ["grundläggningar"]    = "grundlägg",

        // ── Fyllning / Fill ──────────────────────────────────────────────
        ["fyllning"]           = "fyll",
        ["fyllningar"]         = "fyll",
        ["återfyllning"]       = "fyll",
        ["återfyllningar"]     = "fyll",

        // ── Dränering / Drainage ─────────────────────────────────────────
        ["dränerande"]         = "dränering",
        ["dräneringsrör"]      = "dränering rör",
        ["dräneringsarbete"]   = "dränering",
        ["dräneringsmaterial"] = "dränering",
        ["perkolation"]        = "perkolat",
        ["perkolationsmagasin"] = "perkolat",
        ["infiltration"]       = "infiltrat",

        // ── Terrass / Earthworks ─────────────────────────────────────────
        ["terrassering"]       = "terrass",
        ["terrasser"]          = "terrass",
        ["terrassarbete"]      = "terrass",

        // ── Pål / Piling ─────────────────────────────────────────────────
        ["pålning"]            = "pål",
        ["pålar"]              = "pål",
        ["pålarbete"]          = "pål",
        ["pålningsarbete"]     = "pål",

        // ── Markförstärkning / Ground reinforcement ──────────────────────
        ["markförstärkning"]   = "förstärkning",
        ["förstärkningar"]     = "förstärkning",
        ["förstärkningsarbete"] = "förstärkning",

        // ── Rivning / Demolition ─────────────────────────────────────────
        ["rivning"]            = "riv",
        ["rivningsarbete"]     = "riv",
        ["rivningsarbeten"]    = "riv",
        ["röjning"]            = "röj",
        ["röjningsarbete"]     = "röj",
        ["demontering"]        = "demonter",
        ["demonteringsarbete"] = "demonter",
        ["sanering"]           = "saner",
        ["saneringsarbete"]    = "saner",

        // ── Betong / Concrete ─────────────────────────────────────────────
        ["betongkonstruktion"] = "betong",
        ["betongarbete"]       = "betong",
        ["betongarbeten"]      = "betong",
        ["betongelement"]      = "betong element",
        ["gjutning"]           = "gjut",
        ["gjuta"]              = "gjut",
        ["gjuten"]             = "gjut",

        // ── Armering / Reinforcement ──────────────────────────────────────
        ["armerings"]          = "armering",
        ["armeringsjärn"]      = "armering",
        ["armeringar"]         = "armering",
        ["armera"]             = "armering",

        // ── Murning / Masonry ─────────────────────────────────────────────
        ["murning"]            = "mur",
        ["murverk"]            = "mur",
        ["murningsarbete"]     = "mur",

        // ── Beläggning / Pavement ─────────────────────────────────────────
        ["beläggningar"]       = "belägg",
        ["beläggningsarbete"]  = "belägg",
        ["gatubeläggning"]     = "belägg",
        ["gatubeläggningar"]   = "belägg",
        ["vägbeläggning"]      = "belägg",

        // ── Gatsten / Paving stones ───────────────────────────────────────
        ["gatstensbeläggning"] = "gatsten",
        ["smågatsten"]         = "gatsten",
        ["storgatsten"]        = "gatsten",
        ["kantstenar"]         = "kantsten",
        ["kantstöd"]           = "kantsten",
        ["kantstödar"]         = "kantsten",
        ["betongmarksten"]     = "marksten",
        ["markstenar"]         = "marksten",

        // ── Återställande / Restoration ───────────────────────────────────
        ["återställande"]      = "återställ",
        ["återställning"]      = "återställ",
        ["återställningar"]    = "återställ",

        // ── Målning / Painting ────────────────────────────────────────────
        ["nymålning"]          = "målning",
        ["ommålning"]          = "målning",
        ["rostskyddsmålning"]  = "rostskydd målning",
        ["putsarbete"]         = "puts",

        // ── Ledning / Pipes & Conduits ────────────────────────────────────
        ["ledningsdragning"]   = "ledning",
        ["rörledningar"]       = "rör ledning",
        ["anslutningsledning"] = "ledning",
        ["spillvattenledning"] = "spillvatten ledning",
        ["brunnar"]            = "brunn",
        ["brunnstillverkning"] = "brunn",
        ["anslutningsarbete"]  = "anslutning",
        ["tätningar"]          = "tätning",

        // ── Vägg / Wall ───────────────────────────────────────────────────
        ["väggar"]             = "vägg",

        // ── Platta / Slab ─────────────────────────────────────────────────
        ["plattor"]            = "platta",

        // ── Övriga ────────────────────────────────────────────────────────
        ["ledningar"]          = "ledning",
        ["rören"]              = "rör",
        ["kablar"]             = "kabel",
        ["trall"]              = "trall",
        ["trätrall"]           = "trall",

        // ── Enheter / Units ───────────────────────────────────────────────
        ["m2"]                 = "m²",
        ["kvm"]                = "m²",
        ["m3"]                 = "m³",
        ["kbm"]                = "m³",
        ["st"]                 = "styck",
        ["st."]                = "styck",
        ["inkl."]              = "inkl",
        ["ca."]                = "ca",
    };

    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var text = input.Trim().ToLowerInvariant();

        // Strip Swedish abbreviation phrases before tokenizing
        text = AmaAbbreviationRegex().Replace(text, " ");

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

            if (string.IsNullOrWhiteSpace(token) ||
                StopWords.Contains(token))
                continue;

            if (TokenMap.TryGetValue(token, out var mapped))
                token = mapped;

            token = NormalizeToken(token);

            if (string.IsNullOrWhiteSpace(token) ||
                StopWords.Contains(token))
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

    public static string NormalizeTask(string? name, string? code, string? unit, decimal? quantity = null)
    {
        var parts = new[]
        {
            code,
            name,
            unit,
            FormatQuantity(quantity)
        };

        return Normalize(string.Join(' ', parts.Where(x => !string.IsNullOrWhiteSpace(x))));
    }

    public static double CalculateSimilarity(string? leftNormalizedText, string? rightNormalizedText)
    {
        if (string.IsNullOrWhiteSpace(leftNormalizedText) || string.IsNullOrWhiteSpace(rightNormalizedText))
            return 0d;

        if (string.Equals(leftNormalizedText, rightNormalizedText, StringComparison.OrdinalIgnoreCase))
            return 1d;

        var leftWeights  = TokenizeWeighted(leftNormalizedText);
        var rightWeights = TokenizeWeighted(rightNormalizedText);

        if (leftWeights.Count == 0 || rightWeights.Count == 0)
            return 0d;

        var leftSet  = leftWeights.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rightSet = rightWeights.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (leftSet.SetEquals(rightSet))
            return 1d;

        // Weighted Jaccard: tokens that look like codes/numbers count more
        var sharedTokens = leftSet.Intersect(rightSet, StringComparer.OrdinalIgnoreCase).ToList();
        var allTokens    = leftSet.Union(rightSet, StringComparer.OrdinalIgnoreCase).ToList();

        var intersectionWeight = sharedTokens.Sum(t =>
            Math.Max(leftWeights.GetValueOrDefault(t, 1d), rightWeights.GetValueOrDefault(t, 1d)));
        var unionWeight = allTokens.Sum(t =>
            Math.Max(leftWeights.GetValueOrDefault(t, 0d), rightWeights.GetValueOrDefault(t, 0d)));

        var jaccard = unionWeight == 0d ? 0d : intersectionWeight / unionWeight;

        var minTotalWeight = Math.Min(
            leftWeights.Values.Sum(),
            rightWeights.Values.Sum());
        var coverage = minTotalWeight == 0d ? 0d : intersectionWeight / minTotalWeight;

        var containsBonus =
            leftNormalizedText.Contains(rightNormalizedText, StringComparison.OrdinalIgnoreCase) ||
            rightNormalizedText.Contains(leftNormalizedText, StringComparison.OrdinalIgnoreCase)
                ? 0.10d
                : 0d;

        return Math.Round(Math.Min((0.60d * jaccard) + (0.30d * coverage) + (0.10d * containsBonus), 1d), 4);
    }

    /// <summary>
    /// Assigns importance weight to a token.
    /// Alphanumeric codes (VA-100, C25) → 3×, pure numbers → 2×, long words → 1.2×, rest → 1×.
    /// </summary>
    private static double TokenImportance(string token)
    {
        var hasDigit  = token.Any(char.IsDigit);
        var hasLetter = token.Any(char.IsLetter);

        if (hasDigit && hasLetter) return 3.0d; // article codes, standards: "VA100", "C25", "M16"
        if (hasDigit)              return 2.0d; // pure dimensions: "110", "220"
        if (token.Length >= 6)     return 1.2d; // specific long words are more discriminating
        return 1.0d;
    }

    /// <summary>
    /// Returns a graduated score (0–0.15) based on how many AMA code letters match hierarchically.
    /// CBB == CBB → 0.15, CB == CB → 0.10, C == C → 0.05, no match → 0.
    /// </summary>
    public static double GetCodeHierarchyScore(string? targetCode, string? candidateCode)
    {
        if (string.IsNullOrWhiteSpace(targetCode) || string.IsNullOrWhiteSpace(candidateCode))
            return 0d;

        var t = ExtractAmaLetterPrefix(targetCode.Trim());
        var c = ExtractAmaLetterPrefix(candidateCode.Trim());

        if (t.Length == 0 || c.Length == 0)
            return 0d;

        var minLen = Math.Min(t.Length, c.Length);

        // Count matching prefix letters
        var matchLen = 0;
        for (int i = 0; i < minLen; i++)
        {
            if (char.ToUpperInvariant(t[i]) == char.ToUpperInvariant(c[i]))
                matchLen++;
            else
                break;
        }

        return matchLen switch
        {
            >= 3 => 0.15d,
            2    => 0.10d,
            1    => 0.05d,
            _    => 0d
        };
    }

    private static string ExtractAmaLetterPrefix(string code)
    {
        var dotIndex = code.IndexOf('.');
        var prefix = dotIndex > 0 ? code[..dotIndex] : code;
        // Keep only leading letters (the AMA letter portion)
        var end = 0;
        while (end < prefix.Length && char.IsLetter(prefix[end]))
            end++;
        return end > 0 ? prefix[..end] : string.Empty;
    }

    private static string NormalizeToken(string token)
    {
        if (token.Length <= 2)
            return token;

        if (token.EndsWith("arna", StringComparison.OrdinalIgnoreCase) && token.Length > 6)
            token = token[..^4];
        else if (token.EndsWith("erna", StringComparison.OrdinalIgnoreCase) && token.Length > 6)
            token = token[..^4];
        else if (token.EndsWith("ingar", StringComparison.OrdinalIgnoreCase) && token.Length > 7)
            token = token[..^2];  // "schaktningar" → "schaktning"
        else if (token.EndsWith("ningar", StringComparison.OrdinalIgnoreCase) && token.Length > 8)
            token = token[..^2];
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

    private static Dictionary<string, double> TokenizeWeighted(string normalizedText)
    {
        var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            result.TryAdd(token, TokenImportance(token));
        return result;
    }

    private static string? FormatQuantity(decimal? quantity)
        => quantity.HasValue
            ? quantity.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
            : null;

    // Strips "m m", "m.m.", "o d", "o.d.", "e d", "e.d." and variants
    [GeneratedRegex(@"\b(m\.?\s*m|o\.?\s*d|e\.?\s*d)\b\.?", RegexOptions.IgnoreCase)]
    private static partial Regex AmaAbbreviationRegex();

    [GeneratedRegex(@"[^\p{L}\p{N}\s²³]")]
    private static partial Regex NonSearchCharactersRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
