using System.Text.RegularExpressions;
using ProjectManagement.Shared.Constant;

namespace ProjectManagement.Shared.Helper.Text;

public static partial class SwedishTaskTextNormalizer
{
    // ── Compound word splitting ───────────────────────────────────────────────
    // Swedish construction terms are heavily compounded (vattenledning, planteringsyta…).
    // These roots cover the LEFT and RIGHT parts of the most common compounds.
    // Only meaningful construction-domain roots — short ambiguous words (dag, ny…) excluded.
    private static readonly HashSet<string> CompoundRoots = new(StringComparer.OrdinalIgnoreCase)
    {
        // Water & pipe systems
        "vatten", "dagvatten", "spillvatten", "avlopp", "dricksvatten",
        "ledning", "rörledning",
        "rör", "kabel", "slang",
        "brunn", "pump", "ventil", "lucka",
        "kulvert", "kanal", "dike", "ränna", "dräner",

        // Excavation & earthworks
        "schakt", "schaktning",
        "grav", "rörgrav", "ledningsgrav",
        "fyll", "fyllning", "återfyll",
        "terrass", "pål", "plint",
        "jord", "berg", "lera", "sand", "grus", "mark", "mull",
        "sprängning", "kross", "block",

        // Concrete & reinforcement
        "betong", "armering", "stål", "järn",
        "gjut", "gjutning",
        "puts",

        // Structural elements
        "grund", "grundlägg",
        "pelare", "balk", "bjälk", "bjälklag",
        "platta", "mur", "murverk",
        "vägg", "tak", "golv", "fasad",
        "trapp", "räck", "dörr", "fönster",

        // Foundation & piling
        "spont", "borr", "spets",

        // Road & surface materials
        "väg", "gata", "torg",
        "gång", "cykel",
        "asfalt", "belägg", "beläggning",
        "marksten", "gatsten", "kantsten", "kantband",
        "parkering",

        // Areas & surfaces
        "plan", "plats", "yta", "area", "zon", "sektion",
        "park", "grön",

        // Green & landscape
        "plantering", "gräs", "träd", "buske", "häck",
        "lekplats", "sport",

        // Stormwater & water management
        "fördröjning", "magasin", "infiltrat", "perkolat",

        // Heating & energy
        "fjärrvärme", "fjärrkyla", "värme", "energi",

        // Telecom & fiber
        "fiber", "tele", "signal",

        // Piping & sealing
        "anslutning", "fogning",
        "tätning", "isolering", "membran",

        // Electrical
        "belysning", "stolpe", "elledning",

        // Waste & containers
        "avfall", "container",

        // Misc
        "montering", "demonter",
        "riv", "röj", "saner",
        "målning", "rostskydd",
        "kant", "fog",
        "trall", "brygga", "sten",
        "staket",
    };

    // Linking morphemes between compound parts (s is by far the most common in Swedish).
    private static readonly string[] LinkingMorphemes = ["nings", "ings", "s", "e", ""];

    // ── Semantic synonyms (Swedish ↔ Swedish) ─────────────────────────────
    // Maps conceptually equivalent but lexically different Swedish terms to a
    // shared canonical form so queries and task names meet even when different
    // professional vocabulary is used.
    // Values may contain a space to expand into two tokens.
    private static readonly Dictionary<string, string> SynonymMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Vatten (water variants) ──────────────────────────────────────
        ["regnvatten"]        = "dagvatten",
        ["ytvatten"]          = "dagvatten",
        ["stormwater"]        = "dagvatten",
        ["lod"]               = "dagvatten infiltrat",  // Lokal Omhändertagande av Dagvatten

        // ── Avlopp / Avvattning ──────────────────────────────────────────
        ["kanalisation"]      = "avlopp",
        ["kloakering"]        = "avlopp",
        ["avvattning"]        = "dränering",
        ["bortledning"]       = "avlopp ledning",
        ["dränage"]           = "dränering",
        ["avvattningsarbete"] = "avvattning dränering",

        // ── Schakt / Markarbete ──────────────────────────────────────────
        ["markarbete"]        = "schakt",
        ["markberedning"]     = "schakt",
        ["jordarbete"]        = "schakt",
        ["markrörning"]       = "schakt",
        ["sprängarbete"]      = "sprängning riv",

        // ── Betong (concrete variants) ───────────────────────────────────
        ["sprutbetong"]       = "betong",
        ["lättbetong"]        = "betong",
        ["cellbetong"]        = "betong",
        ["skumbetong"]        = "betong",
        ["gjutbetong"]        = "betong gjut",
        ["prefabricerat"]     = "betong element",
        ["prefab"]            = "betong element",

        // ── Armering (reinforcement variants) ────────────────────────────
        ["nätarmering"]       = "armering",
        ["fiberarmering"]     = "armering",
        ["stålarmering"]      = "armering stål",

        // ── Grund / Fundament ────────────────────────────────────────────
        ["fundamentering"]    = "grundlägg",
        ["undergrund"]        = "grund",
        ["underbyggnad"]      = "grund",
        ["spontning"]         = "spont",

        // ── Isolering ────────────────────────────────────────────────────
        ["värmeisolering"]    = "isolering",
        ["ljudisolering"]     = "isolering",
        ["fuktskydd"]         = "tätning",
        ["fuktspärr"]         = "tätning membran",

        // ── Mur / Vägg ───────────────────────────────────────────────────
        ["stödmur"]           = "mur",
        ["stödvägg"]          = "vägg",
        ["stödkonstruktion"]  = "mur",

        // ── Väg / Yta ────────────────────────────────────────────────────
        ["vägbana"]           = "väg",
        ["körbana"]           = "väg",
        ["gångbana"]          = "gång belägg",
        ["cykelbanor"]        = "cykel",
        ["vägarbete"]         = "väg",
        ["gatuarbete"]        = "väg schakt",

        // ── Asfalt ───────────────────────────────────────────────────────
        ["asfaltering"]       = "asfalt belägg",
        ["asfaltbeläggning"]  = "asfalt belägg",
        ["asfaltläggning"]    = "asfalt belägg",
        ["vägmarkering"]      = "belägg målning",
        ["vägmålning"]        = "belägg målning",

        // ── Plantering / Vegetation ──────────────────────────────────────
        ["vegetation"]        = "plantering",
        ["beplantning"]       = "plantering",
        ["grönområde"]        = "grön plantering",
        ["grästorv"]          = "gräs",
        ["gräsmatta"]         = "gräs",
        ["rabatt"]            = "plantering",
        ["blomsterplantering"] = "plantering",
        ["regnbädd"]          = "dagvatten plantering",

        // ── Fyll / Underlag ──────────────────────────────────────────────
        ["underlag"]          = "fyll",
        ["bärlager"]          = "fyll grus",
        ["förstärkningslager"] = "fyll grus",
        ["geotextil"]         = "fyll förstärkning",

        // ── Rör / Ledning ────────────────────────────────────────────────
        ["rörsystem"]         = "rör ledning",
        ["ledningsnät"]       = "ledning",
        ["servisledning"]     = "ledning",
        ["anläggningsledning"] = "ledning",

        // ── Fjärrvärme / Heating ─────────────────────────────────────────
        ["fjärrvärmearbete"]  = "fjärrvärme ledning",
        ["värmeledning"]      = "fjärrvärme ledning",

        // ── Fiber / Tele ─────────────────────────────────────────────────
        ["fiberarbete"]       = "fiber kabel",
        ["telearbete"]        = "tele kabel",
        ["telekabel"]         = "tele kabel",
        ["bredbandskabel"]    = "fiber kabel",

        // ── El / Belysning ───────────────────────────────────────────────
        ["elarbete"]          = "kabel belysning",
        ["elinstallation"]    = "kabel",
        ["gatubelysning"]     = "belysning stolpe",
        ["vägbelysning"]      = "belysning stolpe",

        // ── Staket / Fencing ─────────────────────────────────────────────
        ["stängsling"]        = "staket",
        ["inhägnad"]          = "staket",

        // ── Parkering ────────────────────────────────────────────────────
        ["parkeringsyta"]     = "parkering yta",
        ["parkeringsplats"]   = "parkering",
    };

    private static readonly Lazy<IReadOnlyDictionary<string, string>> ExternalSynonymMap =
        new(LoadExternalSynonymMap, isThreadSafe: true);

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
        // Stormwater
        ["lod-anläggning"]               = "dagvatten infiltrat",
        ["lokalt omhändertagande"]       = "dagvatten infiltrat",
        // Asphalt
        ["asfaltbeläggning av"]          = "asfalt belägg",
        ["asfaltering av"]               = "asfalt belägg",
        ["ny asfalt"]                    = "asfalt",
        // Fiber / Infra
        ["va-ledning"]                   = "vatten avlopp ledning",
        ["va-arbete"]                    = "vatten avlopp",
        ["va ledning"]                   = "vatten avlopp ledning",
        ["fiber och tele"]               = "fiber tele kabel",
        // Heating
        ["fjärrvärme och fjärrkyla"]     = "fjärrvärme fjärrkyla",
        // Marking
        ["horisontell signering"]        = "belägg målning",
        ["vägmarkeringar"]               = "belägg målning",
        // Demolition / Remediation
        ["rivning och schakt"]           = "riv schakt",
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

        // ── Sprängning / Blasting ──────────────────────────────────────────
        ["sprängningar"]       = "sprängning",
        ["bergsprängning"]     = "berg sprängning",
        ["lössprängt"]         = "sprängning",

        // ── Spont / Sheet piling ───────────────────────────────────────────
        ["spontar"]            = "spont",
        ["spontning"]          = "spont",
        ["stålspont"]          = "spont stål",

        // ── Borrning / Drilling ────────────────────────────────────────────
        ["borrning"]           = "borr",
        ["borrningar"]         = "borr",
        ["borrarbete"]         = "borr",
        ["kärnborrning"]       = "borr",

        // ── Fjärrvärme / District heating ─────────────────────────────────
        ["fjärrvärmeledning"]  = "fjärrvärme ledning",
        ["fjärrvärmerör"]      = "fjärrvärme rör",
        ["fjärrkylerör"]       = "fjärrkyla rör",

        // ── Fiber / Tele ───────────────────────────────────────────────────
        ["fiberledning"]       = "fiber ledning",
        ["fiberrör"]           = "fiber rör",
        ["tomrör"]             = "fiber rör kabel",

        // ── Gräs / Grass ──────────────────────────────────────────────────
        ["gräsytor"]           = "gräs yta",
        ["gräsyta"]            = "gräs yta",
        ["gräsmattor"]         = "gräs",
        ["gräsetablering"]     = "gräs plantering",

        // ── Parkering / Parking ────────────────────────────────────────────
        ["parkeringsplatser"]  = "parkering",
        ["parkeringsytor"]     = "parkering yta",
        ["p-platser"]          = "parkering",
        ["p-yta"]              = "parkering yta",

        // ── Staket / Stängsling ────────────────────────────────────────────
        ["stängslar"]          = "staket",
        ["staketen"]           = "staket",

        // ── Trappa / Stairs ────────────────────────────────────────────────
        ["trappor"]            = "trapp",
        ["trappsteg"]          = "trapp",
        ["trappstenar"]        = "trapp sten",

        // ── Dagvatten / Stormwater ─────────────────────────────────────────
        ["dagvattenhantering"] = "dagvatten",
        ["dagvattensystem"]    = "dagvatten ledning",
        ["dagvattenanläggning"] = "dagvatten",
        ["dagvattenmagasin"]   = "dagvatten magasin",
        ["dagvattenbädd"]      = "dagvatten plantering",

        // ── Beläggning / Markbeläggning ────────────────────────────────────
        ["gångbanor"]          = "gång belägg",
        ["gångbana"]           = "gång belägg",

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

        var primaryTokens = new List<string>(); // original-order tokens for bigram generation
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

            // Track primary word (first word of possibly multi-word mapped value)
            var primaryWord = token.Split(' ', 2)[0];
            if (!string.IsNullOrWhiteSpace(primaryWord) && !StopWords.Contains(primaryWord))
                primaryTokens.Add(primaryWord);

            tokens.Add(token);

            // Semantic synonym expansion: adds canonical Swedish equivalent.
            // Applied on BOTH query and target so "regnvatten" ↔ "dagvatten" meet.
            if (TryGetSynonym(token, out var synonym))
            {
                foreach (var st in synonym.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!string.IsNullOrWhiteSpace(st) && !StopWords.Contains(st))
                        tokens.Add(st);
                }
            }

            // Decompose long tokens that may be Swedish compound words.
            // Parts go through the same pipeline so "schaktning" → "schakt" via TokenMap.
            if (token.Length >= 6)
            {
                foreach (var part in SplitSwedishCompound(token))
                {
                    var p = part;
                    if (string.IsNullOrWhiteSpace(p) || StopWords.Contains(p)) continue;
                    if (TokenMap.TryGetValue(p, out var pm)) p = pm;
                    p = NormalizeToken(p);
                    if (!string.IsNullOrWhiteSpace(p) && !StopWords.Contains(p))
                        tokens.Add(p);
                }
            }
        }

        // Adjacent bigrams (a_b and b_a) for symmetric phrase-level matching.
        // Stored with _ separator so Contains("schakt_planteringsyta") is exact.
        // Both orderings added so the client can search either way.
        for (int i = 0; i < primaryTokens.Count - 1; i++)
        {
            var a = primaryTokens[i];
            var b = primaryTokens[i + 1];
            if (!string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) && a != b)
            {
                tokens.Add($"{a}_{b}");
                tokens.Add($"{b}_{a}");
            }
        }

        // Individual tokens first, bigrams last — ensures the 800-char limit
        // truncates bigrams before individual tokens when the field gets long.
        var normalized = string.Join(
            " ",
            tokens
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t.Contains('_') ? 1 : 0)
                .ThenBy(t => t, StringComparer.OrdinalIgnoreCase));

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

    public static IReadOnlyCollection<string> ExtractNormalizedTokens(string? normalizedText)
    {
        if (string.IsNullOrWhiteSpace(normalizedText))
            return [];

        return normalizedText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 1 && !x.Contains('_'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static double CalculateTokenCoverage(string? leftNormalizedText, string? rightNormalizedText)
    {
        if (string.IsNullOrWhiteSpace(leftNormalizedText) || string.IsNullOrWhiteSpace(rightNormalizedText))
            return 0d;

        var leftWeights = TokenizeWeighted(leftNormalizedText)
            .Where(x => x.Key.Length > 1 && !x.Key.Contains('_'))
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        var rightWeights = TokenizeWeighted(rightNormalizedText)
            .Where(x => x.Key.Length > 1 && !x.Key.Contains('_'))
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

        if (leftWeights.Count == 0 || rightWeights.Count == 0)
            return 0d;

        var denominator = leftWeights.Values.Sum();
        if (denominator <= 0d)
            return 0d;

        var covered = leftWeights
            .Where(x => rightWeights.ContainsKey(x.Key))
            .Sum(x => x.Value);

        return Math.Round(Math.Clamp(covered / denominator, 0d, 1d), 4);
    }

    private static bool TryGetSynonym(string token, out string synonym)
    {
        if (SynonymMap.TryGetValue(token, out synonym!))
            return true;

        return ExternalSynonymMap.Value.TryGetValue(token, out synonym!);
    }

    private static IReadOnlyDictionary<string, string> LoadExternalSynonymMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "ProjectManagement",
                "ML",
                "task-synonyms.csv");

            if (!File.Exists(path))
                return map;

            foreach (var line in File.ReadLines(path))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                    continue;

                var separator = trimmed.Contains(';') ? ';' : ',';
                var parts = trimmed.Split(separator, 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2)
                    continue;

                var source = NormalizeExternalSynonymPart(parts[0]);
                var target = NormalizeExternalSynonymPart(parts[1]);
                if (!string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(target))
                    map[source] = target;
            }
        }
        catch
        {
            return map;
        }

        return map;
    }

    private static string NormalizeExternalSynonymPart(string value)
    {
        var text = value.Trim().ToLowerInvariant();
        text = NonSearchCharactersRegex().Replace(text.Replace("/", " ").Replace("-", " "), " ");
        text = WhitespaceRegex().Replace(text, " ").Trim();
        return text;
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

        // ── Exact weighted Jaccard ────────────────────────────────────────────
        var sharedTokens = leftSet.Intersect(rightSet, StringComparer.OrdinalIgnoreCase).ToList();
        var allTokens    = leftSet.Union(rightSet, StringComparer.OrdinalIgnoreCase).ToList();

        var exactIntersection = sharedTokens.Sum(t =>
            Math.Max(leftWeights.GetValueOrDefault(t, 1d), rightWeights.GetValueOrDefault(t, 1d)));
        var unionWeight = allTokens.Sum(t =>
            Math.Max(leftWeights.GetValueOrDefault(t, 0d), rightWeights.GetValueOrDefault(t, 0d)));

        // ── Fuzzy intersection: character-level similarity for unmatched tokens ──
        // Handles typos: "betnog"↔"betong", "scahkt"↔"schakt", "lednnig"↔"ledning".
        // Only for tokens ≥ 4 chars to avoid false matches on short words.
        // Threshold 0.70 = max 1 edit per ~3 chars (e.g. distance ≤ 2 for 7-char word).
        var unmatchedLeft  = leftSet.Except(rightSet, StringComparer.OrdinalIgnoreCase)
                                    .Where(t => t.Length >= 2).ToList();
        var unmatchedRight = rightSet.Except(leftSet, StringComparer.OrdinalIgnoreCase)
                                     .Where(t => t.Length >= 2).ToList();

        var fuzzyIntersection = 0d;
        if (unmatchedLeft.Count > 0 && unmatchedRight.Count > 0)
        {
            var matchedRight = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var lt in unmatchedLeft)
            {
                var best = unmatchedRight
                    .Where(rt => !matchedRight.Contains(rt))
                    .Select(rt => (rt, sim: TokenSimilarity(lt, rt)))
                    .Where(x => x.sim >= 0.68d)
                    .OrderByDescending(x => x.sim)
                    .FirstOrDefault();

                if (best.rt is null) continue;
                matchedRight.Add(best.rt);

                // Partial credit: scaled by character similarity × token importance weight
                var weight = Math.Max(
                    leftWeights.GetValueOrDefault(lt, 1d),
                    rightWeights.GetValueOrDefault(best.rt, 1d));
                fuzzyIntersection += best.sim * weight;
            }
        }

        var intersectionWeight = exactIntersection + fuzzyIntersection;

        var jaccard = unionWeight == 0d ? 0d : intersectionWeight / unionWeight;

        var leftTotal  = leftWeights.Values.Sum();
        var rightTotal = rightWeights.Values.Sum();
        var minTotalWeight = Math.Min(leftTotal, rightTotal);
        var coverage = minTotalWeight == 0d ? 0d : intersectionWeight / minTotalWeight;

        var containsBonus =
            leftNormalizedText.Contains(rightNormalizedText, StringComparison.OrdinalIgnoreCase) ||
            rightNormalizedText.Contains(leftNormalizedText, StringComparison.OrdinalIgnoreCase)
                ? 0.10d
                : 0d;

        // Coverage (= fraction of query tokens found in target) is the primary signal
        // for search: a short query fully contained in a longer task name should score high.
        // Jaccard is kept as a secondary signal to rank more specific matches above broad ones.
        return Math.Round(Math.Min((0.30d * jaccard) + (0.60d * coverage) + (0.10d * containsBonus), 1d), 4);
    }

    private static double TokenSimilarity(string a, string b)
    {
        var minLength = Math.Min(a.Length, b.Length);

        if (minLength >= 2 && (a.StartsWith(b, StringComparison.OrdinalIgnoreCase) ||
                               b.StartsWith(a, StringComparison.OrdinalIgnoreCase)))
        {
            var prefixScore = minLength switch
            {
                >= 4 => 0.96d,
                3 => 0.90d,
                _ => 0.78d
            };

            return Math.Min(prefixScore, 0.70d + (0.30d * minLength / Math.Max(a.Length, b.Length)));
        }

        if (a.Length < 4 || b.Length < 4 || Math.Abs(a.Length - b.Length) > 2)
            return 0d;

        return CharacterSimilarity(a, b);
    }

    /// <summary>
    /// Normalised character-level similarity using Damerau-Levenshtein distance.
    /// Returns 1.0 for identical strings, 0.0 for completely different ones.
    /// Handles transpositions (betnog↔betong) as a single edit, not two.
    /// </summary>
    private static double CharacterSimilarity(string a, string b)
    {
        var distance = DamerauLevenshtein(a, b);
        return 1.0d - (double)distance / Math.Max(a.Length, b.Length);
    }

    /// <summary>
    /// Optimal String Alignment variant of Damerau-Levenshtein.
    /// Counts insertions, deletions, substitutions, and adjacent transpositions.
    /// </summary>
    private static int DamerauLevenshtein(string a, string b)
    {
        int m = a.Length, n = b.Length;
        var d = new int[m + 1, n + 1];

        for (int i = 0; i <= m; i++) d[i, 0] = i;
        for (int j = 0; j <= n; j++) d[0, j] = j;

        for (int i = 1; i <= m; i++)
        for (int j = 1; j <= n; j++)
        {
            int cost = char.ToLowerInvariant(a[i - 1]) == char.ToLowerInvariant(b[j - 1]) ? 0 : 1;
            d[i, j] = Math.Min(
                Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                d[i - 1, j - 1] + cost);

            // Transposition: swap adjacent characters counts as 1 edit
            if (i > 1 && j > 1
                && char.ToLowerInvariant(a[i - 1]) == char.ToLowerInvariant(b[j - 2])
                && char.ToLowerInvariant(a[i - 2]) == char.ToLowerInvariant(b[j - 1]))
            {
                d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
            }
        }

        return d[m, n];
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

    /// <summary>
    /// Splits a (already-normalized) token into its Swedish compound parts.
    /// Yields the meaningful sub-roots — NOT the original token itself.
    /// Caller is responsible for running the yielded parts through TokenMap + NormalizeToken.
    ///
    /// Examples:
    ///   "vattenledning"     → ["vatten", "ledning"]
    ///   "planteringsyta"    → ["plantering", "yta"]
    ///   "spillvattenledning"→ ["spillvatten", "ledning"]
    ///   "dagvattenledningsgrav" → ["dagvatten", "ledning", "grav"]
    ///   "schaktningsarbete" → ["schaktning"]  (arbete = stop word, prefix still extracted)
    ///   "betongarbete"      → ["betong"]
    /// </summary>
    private static IEnumerable<string> SplitSwedishCompound(string token)
    {
        if (token.Length < 6) yield break;

        foreach (var root in CompoundRoots
            .Where(r => r.Length >= 3
                     && r.Length < token.Length
                     && token.StartsWith(r, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.Length))
        {
            foreach (var link in LinkingMorphemes)
            {
                var remainder = token[root.Length..];
                if (!remainder.StartsWith(link, StringComparison.OrdinalIgnoreCase)) continue;

                var tail = remainder[link.Length..];
                if (tail.Length < 3) continue;

                // ── 2-part compound: tail is a known root ──────────────────
                if (CompoundRoots.Contains(tail))
                {
                    yield return root;
                    yield return tail;
                    yield break;
                }

                // ── tail is a stop word: prefix is still meaningful ────────
                // e.g. "betongarbete" → "betong" + arbete(stop) → extract "betong"
                if (StopWords.Contains(tail))
                {
                    yield return root;
                    yield break;
                }

                // ── 3-part compound ────────────────────────────────────────
                // e.g. "dagvattenledningsgrav" → dagvatten + ledning + grav
                foreach (var root2 in CompoundRoots
                    .Where(r => r.Length >= 3
                             && r.Length < tail.Length
                             && tail.StartsWith(r, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(r => r.Length))
                {
                    var tail2 = tail[root2.Length..];

                    foreach (var link2 in LinkingMorphemes)
                    {
                        if (!tail2.StartsWith(link2, StringComparison.OrdinalIgnoreCase)) continue;
                        var tail3 = tail2[link2.Length..];
                        if (tail3.Length < 3) continue;

                        if (CompoundRoots.Contains(tail3) || StopWords.Contains(tail3))
                        {
                            yield return root;
                            yield return root2;
                            if (CompoundRoots.Contains(tail3)) yield return tail3;
                            yield break;
                        }
                    }

                    // root2 fills all of tail (tail2 is empty or stop word)
                    if (tail2.Length == 0 || StopWords.Contains(tail2))
                    {
                        yield return root;
                        yield return root2;
                        yield break;
                    }
                }
            }
        }
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
