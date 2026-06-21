using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Services.DemoSeed;

// Demo data definitions for three companies. Values are intentionally *different* between companies
// (some matching, some missing, some same code/different name) so import/export name-matching and the
// manual-mapping flow can be exercised. All seeding keyed off these is idempotent (match by name/code).

public readonly record struct DemoLookup(string Name, int Sort, bool IsDefault = false, string Color = "#64748B");

public readonly record struct DemoAccount(string Code, string Name, string Group);

// Maps a fixed ResourceTypesEnum kind to the company-specific display name (e.g. "UE" vs "Underentreprenör").
public readonly record struct DemoResType(ResourceTypesEnum Kind, string Name, int Sort);

public readonly record struct DemoOrg(string Name, string Category, string OrgNumber, string Email, string Phone);

public readonly record struct DemoResource(string Name, ResourceTypesEnum Kind, string Sort, string AccountCode, decimal Cost, string Unit);

public sealed class DemoCompany
{
    public required string Name { get; init; }
    public required string Slug { get; init; }                 // db name + email prefix
    public required DemoLookup[] ProjectStatuses { get; init; }
    public required DemoLookup[] ProjectTypes { get; init; }    // shared "Typ" lookup (project + calc)
    public required DemoLookup[] ProcurementForms { get; init; }   // Upphandlingsform
    public required DemoLookup[] ProcurementProcedures { get; init; }
    public required DemoLookup[] ContractForms { get; init; }   // Entreprenadform
    public required DemoLookup[] Compensations { get; init; }   // Ersättningsform
    public required DemoLookup[] CalcStatuses { get; init; }
    public required string[] AccountGroups { get; init; }
    public required DemoAccount[] Accounts { get; init; }
    public required DemoResType[] ResourceTypes { get; init; }
    public required DemoLookup[] ResourceSorts { get; init; }
    public required DemoOrg[] Organisations { get; init; }
    public required DemoResource[] Resources { get; init; }
}

public static class DemoSeedCatalog
{
    public const string AdminEmailDomain = "test.local";
    // Dev/test only shared password (meets the app's identity policy).
    public const string SharedPassword = "Demo!Pass123";

    public static readonly string[] OrgCategories =
        ["Eget företag", "Kund", "Leverantör", "Konkurrent", "Annan"];

    public static readonly string[] DemoFolders =
        ["Demoprojekt", "Pågående", "Vunna projekt", "Arkivtest"];

    public static readonly string[] Departments = ["Kalkyl", "Produktion", "Ledning"];

    public static DemoCompany[] Companies => [Nordbygg, Sydprojekt, Vastinfra];

    private static DemoLookup[] L(params (string Name, bool IsDefault)[] items)
    {
        var arr = new DemoLookup[items.Length];
        for (var i = 0; i < items.Length; i++)
            arr[i] = new DemoLookup(items[i].Name, (i + 1) * 100, items[i].IsDefault);
        return arr;
    }

    private static DemoResType[] RT(params (ResourceTypesEnum Kind, string Name)[] items)
    {
        var arr = new DemoResType[items.Length];
        for (var i = 0; i < items.Length; i++)
            arr[i] = new DemoResType(items[i].Kind, items[i].Name, (i + 1) * 100);
        return arr;
    }

    // ── Company 1: Nordbygg Kalkyl AB ──────────────────────────────────────────
    public static readonly DemoCompany Nordbygg = new()
    {
        Name = "Nordbygg Kalkyl AB",
        Slug = "nordbygg",
        ProjectStatuses = L(("Förfrågan", true), ("Pågående", false), ("Inlämnat", false),
            ("Vunnet", false), ("Förlorat", false), ("Arkiverat", false)),
        ProjectTypes = L(("Markarbete", true), ("Bygg", false), ("VA", false), ("Energi", false)),
        ProcurementForms = L(("Offentlig upphandling", true), ("Privat upphandling", false), ("Ramavtal", false)),
        ProcurementProcedures = L(("Öppet förfarande", true), ("Selektivt förfarande", false), ("Direktupphandling", false)),
        ContractForms = L(("Totalentreprenad", true), ("Utförandeentreprenad", false)),
        Compensations = L(("Fast pris", true), ("Löpande räkning", false), ("Mängdreglering", false)),
        CalcStatuses = L(("Utkast", true), ("Under arbete", false), ("Granskning", false),
            ("Godkänd / låst", false), ("Skickad", false), ("Tilldelad / vunnen", false), ("Förlorad", false)),
        AccountGroups = ["Kostnad", "Intäkt"],
        Accounts =
        [
            new("4010", "Material", "Kostnad"),
            new("4020", "Underentreprenör", "Kostnad"),
            new("5010", "Arbete", "Kostnad"),
            new("5210", "Maskiner", "Kostnad"),
            new("5810", "Transport", "Kostnad"),
        ],
        ResourceTypes = RT((ResourceTypesEnum.Materials, "Material"), (ResourceTypesEnum.Worker, "Arbete"),
            (ResourceTypesEnum.MachinesAndEquipments, "Maskin"), (ResourceTypesEnum.Subcontractors, "Underentreprenör"),
            (ResourceTypesEnum.overheadCosts, "Övrigt")),
        ResourceSorts = L(("Mark", false), ("Betong", false), ("Transport", false), ("Etablering", false), ("Administration", false)),
        Organisations =
        [
            new("Nordbygg Kalkyl AB", "Eget företag", "556001-1001", "info@nordbygg.test.local", "08-100100"),
            new("Stockholms Stad", "Kund", "212000-0142", "upphandling@stockholm.test.local", "08-111111"),
            new("Trafikverket", "Kund", "202100-6297", "kund@trafikverket.test.local", "08-222222"),
            new("Region Uppsala", "Kund", "232100-0024", "inkop@regionuppsala.test.local", "018-333333"),
            new("Akademiska Hus", "Kund", "556459-9156", "kontakt@akademiskahus.test.local", "08-444444"),
            new("Riksbyggen", "Kund", "702001-7781", "info@riksbyggen.test.local", "08-555555"),
            new("ByggPartner AB", "Leverantör", "556594-1234", "order@byggpartner.test.local", "023-100100"),
            new("Cementa AB", "Leverantör", "556013-5864", "order@cementa.test.local", "08-625000"),
            new("Ahlsell Sverige AB", "Leverantör", "556012-9206", "order@ahlsell.test.local", "08-685700"),
            new("Swecon Anläggning", "Leverantör", "556472-1234", "uthyrning@swecon.test.local", "010-556000"),
            new("Asfaltbolaget Syd", "Leverantör", "556777-2222", "order@asfaltsyd.test.local", "040-666666"),
            new("Skanska Sverige AB", "Konkurrent", "556033-9086", "info@skanska.test.local", "010-448000"),
            new("Peab Anläggning AB", "Konkurrent", "556568-6721", "info@peab.test.local", "0431-890000"),
            new("NCC Sverige AB", "Konkurrent", "556034-5174", "info@ncc.test.local", "08-585100"),
            new("Maskinpoolen Norr", "Annan", "", "info@maskinpoolennorr.test.local", "090-777777"),
            new("Byggsäkerhet i Norr", "Annan", "", "kontakt@byggsakerhet.test.local", "090-888888"),
        ],
        Resources =
        [
            new("Betong C30/37", ResourceTypesEnum.Materials, "Betong", "4010", 1450m, "m3"),
            new("Armeringsnät", ResourceTypesEnum.Materials, "Betong", "4010", 320m, "st"),
            new("Bergkross 0-32", ResourceTypesEnum.Materials, "Mark", "4010", 180m, "ton"),
            new("Asfalt ABT16", ResourceTypesEnum.Materials, "Transport", "4010", 950m, "ton"),
            new("Grävmaskin 20 ton", ResourceTypesEnum.MachinesAndEquipments, "Mark", "5210", 850m, "tim"),
            new("Hjullastare", ResourceTypesEnum.MachinesAndEquipments, "Mark", "5210", 720m, "tim"),
            new("Lastbil med släp", ResourceTypesEnum.MachinesAndEquipments, "Transport", "5810", 690m, "tim"),
            new("Vibroplatta", ResourceTypesEnum.MachinesAndEquipments, "Mark", "5210", 220m, "tim"),
            new("Anläggningsarbetare", ResourceTypesEnum.Worker, "Mark", "5010", 420m, "tim"),
            new("Betongarbetare", ResourceTypesEnum.Worker, "Betong", "5010", 460m, "tim"),
            new("Yrkesarbetare bygg", ResourceTypesEnum.Worker, "Betong", "5010", 480m, "tim"),
            new("Maskinist", ResourceTypesEnum.Worker, "Mark", "5010", 510m, "tim"),
            new("Projektledning", ResourceTypesEnum.Managers, "Administration", "5010", 780m, "tim"),
            new("Arbetsledare", ResourceTypesEnum.Managers, "Administration", "5010", 620m, "tim"),
            new("UE Asfaltering", ResourceTypesEnum.Subcontractors, "Transport", "4020", 0m, "post"),
            new("UE Markarbeten", ResourceTypesEnum.Subcontractors, "Mark", "4020", 0m, "post"),
            new("UE El", ResourceTypesEnum.Subcontractors, "Etablering", "4020", 0m, "post"),
            new("Etablering byggplats", ResourceTypesEnum.overheadCosts, "Etablering", "5010", 25000m, "post"),
            new("Bodar och kontor", ResourceTypesEnum.overheadCosts, "Etablering", "5010", 12000m, "mån"),
            new("Byggström", ResourceTypesEnum.overheadCosts, "Etablering", "5810", 8000m, "mån"),
        ],
    };

    // ── Company 2: Sydprojekt Entreprenad AB ───────────────────────────────────
    // Same account *codes* as Nordbygg but different *names* → tests uncertain account matching.
    public static readonly DemoCompany Sydprojekt = new()
    {
        Name = "Sydprojekt Entreprenad AB",
        Slug = "sydprojekt",
        ProjectStatuses = L(("Förfrågan", true), ("Kalkyleras", false), ("Inlämnat", false),
            ("Tilldelat", false), ("Ej tilldelat", false), ("Avslutat", false)),
        ProjectTypes = L(("Anläggning", true), ("Markarbete", false), ("Service", false), ("Energi", false)),
        ProcurementForms = L(("Offentlig upphandling", true), ("Privat kund", false), ("Ramavtal", false)),
        ProcurementProcedures = L(("Öppet förfarande", true), ("Förhandlat förfarande", false), ("Direktupphandling", false)),
        ContractForms = L(("Totalentreprenad", true), ("Generalentreprenad", false)),
        Compensations = L(("Fast pris", true), ("Löpande räkning", false), ("Takpris", false)),
        CalcStatuses = L(("Utkast", true), ("Pågående", false), ("Intern granskning", false),
            ("Låst", false), ("Skickad", false), ("Vunnen", false), ("Förlorad", false)),
        AccountGroups = ["Kostnad", "Intäkt"],
        Accounts =
        [
            new("4010", "Materialinköp", "Kostnad"),
            new("4020", "UE-kostnad", "Kostnad"),
            new("5010", "Personal", "Kostnad"),
            new("5210", "Maskinhyra", "Kostnad"),
            new("5810", "Transportkostnad", "Kostnad"),
        ],
        ResourceTypes = RT((ResourceTypesEnum.Materials, "Material"), (ResourceTypesEnum.Worker, "Personal"),
            (ResourceTypesEnum.MachinesAndEquipments, "Maskin"), (ResourceTypesEnum.Subcontractors, "UE"),
            (ResourceTypesEnum.overheadCosts, "Övrigt")),
        ResourceSorts = L(("Markarbete", false), ("Betongarbete", false), ("Transport", false), ("Etablering", false), ("Projektledning", false)),
        Organisations =
        [
            new("Sydprojekt Entreprenad AB", "Eget företag", "556002-2002", "info@sydprojekt.test.local", "040-200200"),
            new("Malmö Stad", "Kund", "212000-1124", "upphandling@malmo.test.local", "040-101010"),
            new("Trafikverket", "Kund", "202100-6297", "kund@trafikverket.test.local", "040-202020"),
            new("Region Skåne", "Kund", "232100-0255", "inkop@skane.test.local", "040-303030"),
            new("MKB Fastighets AB", "Kund", "556049-1432", "kontakt@mkb.test.local", "040-404040"),
            new("HSB Skåne", "Kund", "746000-0123", "info@hsbskane.test.local", "040-505050"),
            // Same org-number as Nordbygg's "ByggPartner AB" but different name → uncertain org matching.
            new("Bygg Partner Sverige AB", "Leverantör", "556594-1234", "order@byggpartnersverige.test.local", "040-606060"),
            new("Thomas Betong AB", "Leverantör", "556045-1234", "order@thomasbetong.test.local", "040-707070"),
            new("Dahl Sverige AB", "Leverantör", "556287-0123", "order@dahl.test.local", "040-808080"),
            new("Ramirent AB", "Leverantör", "556248-1234", "uthyrning@ramirent.test.local", "040-909090"),
            new("Skånsk Asfalt AB", "Leverantör", "556888-3333", "order@skanskasfalt.test.local", "040-111222"),
            new("Skanska Sverige AB", "Konkurrent", "556033-9086", "info@skanska.test.local", "010-448000"),
            new("Veidekke Entreprenad", "Konkurrent", "556550-7448", "info@veidekke.test.local", "08-635610"),
            // Same name as a Nordbygg competitor but no org-number → uncertain name-only matching.
            new("Peab Anläggning AB", "Konkurrent", "", "syd@peab.test.local", "0431-890000"),
            new("Maskincentralen Syd", "Annan", "", "info@maskincentralensyd.test.local", "040-333444"),
            new("Skånes Bygglogistik", "Annan", "", "kontakt@bygglogistik.test.local", "040-555666"),
        ],
        Resources =
        [
            new("Betong C28/35", ResourceTypesEnum.Materials, "Betongarbete", "4010", 1390m, "m3"),
            new("Armering K500", ResourceTypesEnum.Materials, "Betongarbete", "4010", 310m, "st"),
            new("Makadam 8-16", ResourceTypesEnum.Materials, "Markarbete", "4010", 170m, "ton"),
            new("Asfalt ABS16", ResourceTypesEnum.Materials, "Transport", "4010", 920m, "ton"),
            new("Grävmaskin 16 ton", ResourceTypesEnum.MachinesAndEquipments, "Markarbete", "5210", 790m, "tim"),
            new("Dumper", ResourceTypesEnum.MachinesAndEquipments, "Transport", "5210", 760m, "tim"),
            new("Lastbil 4-axlig", ResourceTypesEnum.MachinesAndEquipments, "Transport", "5810", 700m, "tim"),
            new("Padda", ResourceTypesEnum.MachinesAndEquipments, "Markarbete", "5210", 210m, "tim"),
            new("Anläggare", ResourceTypesEnum.Worker, "Markarbete", "5010", 430m, "tim"),
            new("Betongarbetare", ResourceTypesEnum.Worker, "Betongarbete", "5010", 465m, "tim"),
            new("Snickare", ResourceTypesEnum.Worker, "Betongarbete", "5010", 470m, "tim"),
            new("Maskinförare", ResourceTypesEnum.Worker, "Markarbete", "5010", 520m, "tim"),
            new("Projektledare", ResourceTypesEnum.Managers, "Projektledning", "5010", 800m, "tim"),
            new("Platschef", ResourceTypesEnum.Managers, "Projektledning", "5010", 850m, "tim"),
            new("UE Asfalt", ResourceTypesEnum.Subcontractors, "Transport", "4020", 0m, "post"),
            new("UE Schakt", ResourceTypesEnum.Subcontractors, "Markarbete", "4020", 0m, "post"),
            new("UE VS", ResourceTypesEnum.Subcontractors, "Etablering", "4020", 0m, "post"),
            new("Etablering", ResourceTypesEnum.overheadCosts, "Etablering", "5010", 28000m, "post"),
            new("Manskapsbodar", ResourceTypesEnum.overheadCosts, "Etablering", "5010", 13000m, "mån"),
            new("Byggel", ResourceTypesEnum.overheadCosts, "Etablering", "5810", 8500m, "mån"),
        ],
    };

    // ── Company 3: Västinfra Konsult AB ────────────────────────────────────────
    // Different account *codes* (incl. an income account) → tests missing-account import deviations.
    public static readonly DemoCompany Vastinfra = new()
    {
        Name = "Västinfra Konsult AB",
        Slug = "vastinfra",
        ProjectStatuses = L(("Ny", true), ("Aktiv", false), ("Lämnad", false),
            ("Antagen", false), ("Avböjd", false), ("Stängd", false)),
        ProjectTypes = L(("Infrastruktur", true), ("VA", false), ("El och energi", false), ("Konsultuppdrag", false)),
        ProcurementForms = L(("Offentlig", true), ("Privat", false), ("Partneravtal", false)),
        ProcurementProcedures = L(("Öppet", true), ("Selektivt", false), ("Direkt", false)),
        ContractForms = L(("Total", true), ("Utförande", false), ("Konsult", false)),
        Compensations = L(("Fast arvode", true), ("Löpande", false), ("Budgetpris", false)),
        CalcStatuses = L(("Utkast", true), ("Bearbetas", false), ("Granskad", false),
            ("Låst", false), ("Inlämnad", false), ("Antagen", false), ("Avböjd", false)),
        AccountGroups = ["Kostnad", "Intäkt"],
        Accounts =
        [
            new("3001", "Intäkt projekt", "Intäkt"),
            new("4100", "Material", "Kostnad"),
            new("4200", "Underkonsult", "Kostnad"),
            new("5100", "Timmar", "Kostnad"),
            new("5200", "Maskiner", "Kostnad"),
        ],
        ResourceTypes = RT((ResourceTypesEnum.Materials, "Material"), (ResourceTypesEnum.Worker, "Timmar"),
            (ResourceTypesEnum.MachinesAndEquipments, "Maskiner"), (ResourceTypesEnum.Design, "Konsult"),
            (ResourceTypesEnum.Subcontractors, "Underkonsult")),
        ResourceSorts = L(("Projektering", false), ("Mark", false), ("Transport", false), ("Maskin", false), ("Ledning", false)),
        Organisations =
        [
            new("Västinfra Konsult AB", "Eget företag", "556003-3003", "info@vastinfra.test.local", "031-300300"),
            new("Göteborgs Stad", "Kund", "212000-1355", "upphandling@goteborg.test.local", "031-101010"),
            new("Trafikverket", "Kund", "202100-6297", "kund@trafikverket.test.local", "031-202020"),
            new("Västra Götalandsregionen", "Kund", "232100-0131", "inkop@vgregion.test.local", "031-303030"),
            new("Göteborg Energi AB", "Kund", "556362-6794", "kontakt@goteborgenergi.test.local", "031-404040"),
            new("Framtiden AB", "Kund", "556012-3456", "info@framtiden.test.local", "031-505050"),
            new("WSP Sverige AB", "Leverantör", "556057-4880", "info@wsp.test.local", "010-722500"),
            new("Sweco Sverige AB", "Leverantör", "556542-9841", "info@sweco.test.local", "08-695600"),
            new("Ramboll Sverige AB", "Leverantör", "556133-0506", "info@ramboll.test.local", "010-615600"),
            new("Geoteknik Väst AB", "Leverantör", "556789-4444", "order@geoteknikvast.test.local", "031-606060"),
            new("Mätkonsult Göteborg", "Leverantör", "556890-5555", "info@matkonsult.test.local", "031-707070"),
            new("Afry AB", "Konkurrent", "556120-6474", "info@afry.test.local", "010-505000"),
            new("Tyréns AB", "Konkurrent", "556194-7986", "info@tyrens.test.local", "010-452200"),
            new("Norconsult AB", "Konkurrent", "556212-1331", "info@norconsult.test.local", "010-141800"),
            new("Fältgeoteknik Väst", "Annan", "", "info@faltgeoteknik.test.local", "031-808080"),
            new("Dokumenthantering AB", "Annan", "", "kontakt@dokumenthantering.test.local", "031-909090"),
        ],
        Resources =
        [
            new("Geomaterial", ResourceTypesEnum.Materials, "Mark", "4100", 150m, "ton"),
            new("Rörmaterial VA", ResourceTypesEnum.Materials, "Mark", "4100", 420m, "m"),
            new("Kabel och kanalisation", ResourceTypesEnum.Materials, "Mark", "4100", 280m, "m"),
            new("Förbrukningsmaterial", ResourceTypesEnum.Materials, "Projektering", "4100", 90m, "post"),
            new("Borrigg", ResourceTypesEnum.MachinesAndEquipments, "Maskin", "5200", 1100m, "tim"),
            new("Mätbil", ResourceTypesEnum.MachinesAndEquipments, "Maskin", "5200", 650m, "tim"),
            new("Servicebil", ResourceTypesEnum.MachinesAndEquipments, "Transport", "5200", 480m, "tim"),
            new("Drönare kartering", ResourceTypesEnum.MachinesAndEquipments, "Projektering", "5200", 350m, "tim"),
            new("Handläggare", ResourceTypesEnum.Worker, "Ledning", "5100", 720m, "tim"),
            new("Tekniker fält", ResourceTypesEnum.Worker, "Mark", "5100", 560m, "tim"),
            new("Mätingenjör", ResourceTypesEnum.Worker, "Projektering", "5100", 680m, "tim"),
            new("Geotekniker", ResourceTypesEnum.Worker, "Mark", "5100", 700m, "tim"),
            new("Uppdragsledare", ResourceTypesEnum.Design, "Ledning", "5100", 950m, "tim"),
            new("Projekteringsledare", ResourceTypesEnum.Design, "Projektering", "5100", 880m, "tim"),
            new("Konstruktör väg", ResourceTypesEnum.Design, "Projektering", "5100", 820m, "tim"),
            new("Konstruktör VA", ResourceTypesEnum.Design, "Projektering", "5100", 810m, "tim"),
            new("Underkonsult geoteknik", ResourceTypesEnum.Subcontractors, "Mark", "4200", 0m, "post"),
            new("Underkonsult el", ResourceTypesEnum.Subcontractors, "Ledning", "4200", 0m, "post"),
            new("Underkonsult mätning", ResourceTypesEnum.Subcontractors, "Projektering", "4200", 0m, "post"),
            new("Resor och traktamente", ResourceTypesEnum.Subcontractors, "Transport", "4200", 5000m, "post"),
        ],
    };
}
