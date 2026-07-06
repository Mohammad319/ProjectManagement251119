using ProjectManagement.Shared.Constants;
using ProjectManagement.Shared.DTO.Calculation.Template;
using ProjectManagement.Shared.Enums;
using static ProjectManagement.Shared.DTO.Calculation.Template.NetColumnId;

namespace Persistence.Seeding;

/// <summary>A read-only standard column template: a name + the ordered net-calc columns it shows.</summary>
public readonly record struct ColumnTemplateSeed(string Name, NetColumnId[] Columns);

public readonly record struct LookupSeed(
    string Name,
    string Color,
    int SortOrder,
    bool IsVisible = true,
    string Code = "",
    bool IsDefault = false,
    bool IsSystemDefault = false);

public readonly record struct CalcStatusSeed(
    string Name, string Color, int SortOrder,
    bool IsVisible = true,
    bool IsApprovalStatus = false,
    bool LocksCalculation = false,
    bool AllowsProductionCalculation = false,
    bool CountsAsSubmittedBid = false,
    bool CountsAsWonBid = false,
    bool CountsAsLostBid = false,
    bool IsDefault = false);

public readonly record struct AccountSeed(string Code, string Name, string GroupName, bool IsVisible = true);

public static class TenantSeedCatalog
{
    public static readonly LookupSeed[] ResourceStatuses =
    [
        new("Utkast", "#64748B", 100, Code: "DRAFT", IsDefault: true, IsSystemDefault: true),
        new("Kontroll krävs", "#F97316", 200, Code: "REVIEW_REQUIRED", IsSystemDefault: true),
        new("Mängdkontroll", "#F97316", 300, Code: "QUANTITY_REVIEW", IsSystemDefault: true),
        new("Kostnadskontroll", "#F97316", 400, Code: "COST_REVIEW", IsSystemDefault: true),
        new("Kontrollerad", "#16A34A", 500, Code: "REVIEWED", IsSystemDefault: true),
        new("Avvikande", "#DC2626", 600, Code: "DEVIATING", IsSystemDefault: true),
    ];

    public static readonly LookupSeed[] TaskStatuses =
    [
        new("Utkast", "#64748B", 100, Code: "DRAFT", IsDefault: true, IsSystemDefault: true),
        new("Kontroll krävs", "#F97316", 200, Code: "REVIEW_REQUIRED", IsSystemDefault: true),
        new("Mängdkontroll", "#F97316", 300, Code: "QUANTITY_REVIEW", IsSystemDefault: true),
        new("Kostnadskontroll", "#F97316", 400, Code: "COST_REVIEW", IsSystemDefault: true),
        new("Kontrollerad", "#16A34A", 500, Code: "REVIEWED", IsSystemDefault: true),
        new("Avvikande", "#DC2626", 600, Code: "DEVIATING", IsSystemDefault: true),
    ];

    public static readonly string[] AccountGroups = ["AG1", "AG2"];

    public static readonly AccountSeed[] Accounts =
    [
        new("Code1", "Acc1", "AG1"),
        new("Code2", "Acc2", "AG1"),
        new("Code3", "Acc3", "AG2"),
        new("Code4", "Acc4", "AG2"),
    ];

    public static readonly LookupSeed[] Compensations =
    [
        new("Fast pris",            "#3b82f6", 100),
        new("À-pris / mängd",       "#0ea5e9", 200),
        new("Löpande räkning",      "#8b5cf6", 300),
        new("Målkostnad",           "#16a34a", 400),
        new("Incitamentsavtal",     "#ca8a04", 500),
        new("Garanterat maxpris",   "#0369a1", 600),
        new("Självkostnad",         "#64748b", 700),
        new("Tid och material",     "#94a3b8", 800),
        new("Annat",                "#9ca3af", 900),
    ];

    public static readonly LookupSeed[] Contracts =
    [
        new("Totalentreprenad",         "#0ea5e9", 100),
        new("Utförandeentreprenad",     "#0284c7", 200),
        new("Generalentreprenad",       "#0369a1", 300),
        new("Delad entreprenad",        "#7c3aed", 400),
        new("Samverkansentreprenad",    "#16a34a", 500),
        new("Underentreprenad",         "#ca8a04", 600),
        new("Underhållsavtal",          "#64748b", 700),
        new("Serviceavtal",             "#94a3b8", 800),
        new("Annat",                    "#9ca3af", 900),
    ];

    public static readonly LookupSeed[] ProcurementMethods =
    [
        new("Offentlig upphandling",             "#3b82f6", 100),
        new("Privat upphandling",                "#0ea5e9", 200),
        new("Ramavtal",                          "#8b5cf6", 300),
        new("Avrop",                             "#16a34a", 400),
        new("Förnyad konkurrensutsättning",      "#ca8a04", 500),
        new("Direkttilldelning",                 "#dc2626", 600),
        new("Anbudsförfrågan",                   "#64748b", 700),
        new("Annat",                             "#9ca3af", 800),
    ];

    public static readonly CalcStatusSeed[] CalculationStatuses =
    [
        new("Förfrågan",              "#6B7280", 100, IsDefault: true),
        new("Planerad",               "#93C5FD", 200),
        new("Pågående",               "#1D4ED8", 300),
        new("Behöver granskas",       "#5EEAD4", 400),
        new("Granskad",               "#0D9488", 500),
        new("Godkänd / låst",         "#0F766E", 600, IsApprovalStatus: true, LocksCalculation: true, AllowsProductionCalculation: true),
        new("Skickad / inlämnad",     "#EAB308", 700, IsApprovalStatus: true, LocksCalculation: true, AllowsProductionCalculation: true, CountsAsSubmittedBid: true),
        new("Tilldelad / vunnen",     "#166534", 800, IsApprovalStatus: true, LocksCalculation: true, AllowsProductionCalculation: true, CountsAsSubmittedBid: true, CountsAsWonBid: true),
        new("Förlorad",               "#DC2626", 900, CountsAsSubmittedBid: true, CountsAsLostBid: true),
        new("Avbruten",               "#F97316", 1000),
        new("Ej intressant / ej lämnat", "#FEF08A", 1100),
    ];

    public static readonly CalcStatusSeed[] ProjectStatuses =
    [
        new("Förfrågan",                "#6B7280", 100, IsDefault: true),
        new("Pågående",                 "#1D4ED8", 200),
        new("Inlämnat / väntar beslut", "#EAB308", 300, CountsAsSubmittedBid: true),
        new("Vunnet",                   "#166534", 400, CountsAsSubmittedBid: true, CountsAsWonBid: true),
        new("Förlorat",                 "#DC2626", 500, CountsAsSubmittedBid: true, CountsAsLostBid: true),
        new("Ej inlämnat",              "#F97316", 600),
        new("Ej intressant",            "#FEF08A", 700),
        new("Avbrutet",                 "#7C2D12", 800),
    ];

    public static readonly LookupSeed[] ProcurementProcedures =
    [
        new("Öppet förfarande",                  "#3b82f6", 100),
        new("Selektivt förfarande",              "#0ea5e9", 200),
        new("Förhandlat förfarande",             "#8b5cf6", 300),
        new("Konkurrenspräglad dialog",          "#16a34a", 400),
        new("Direktupphandling",                 "#ca8a04", 500),
        new("Direkttilldelning",                 "#dc2626", 600),
        new("Förenklat förfarande",              "#64748b", 700),
        new("Annat",                             "#9ca3af", 800),
    ];

    public static readonly LookupSeed[] ProjectTypes =
    [
        new("General",                  "#3b82f6", 100, IsDefault: true),
        new("Väg",                      "#78716c", 200),
        new("Bro",                      "#a78bfa", 300),
        new("VA",                       "#06b6d4", 400),
        new("Markarbete",               "#84cc16", 500),
        new("Utemiljö / anläggning",    "#22c55e", 600),
        new("Husbyggnad",               "#f97316", 700),
        new("Renovering",               "#eab308", 800),
        new("Nybyggnation",             "#3b82f6", 900),
        new("Ombyggnad",                "#8b5cf6", 1000),
        new("Tillbyggnad",              "#0ea5e9", 1100),
        new("Drift och underhåll",      "#64748b", 1200),
        new("Underhållsprojekt",        "#94a3b8", 1300),
        new("Serviceprojekt",           "#f59e0b", 1400),
        new("Internt projekt",          "#6366f1", 1500),
        new("Annat",                    "#9ca3af", 1600),
    ];

    // ---------------------------------------------------------------------------------------------
    // Standard (read-only) column templates seeded for every new tenant. Names match the display names
    // detected as "standard" in TemplateIndex; each lists the ordered net-calc columns it shows.
    // ---------------------------------------------------------------------------------------------
    public static readonly ColumnTemplateSeed[] StandardColumnTemplates =
    [
        new("Kompakt nettokalkyl",
            [Code, Name, Quantity, Unit, Cost, TotalNetCost]),

        new("Standard nettokalkyl",
            [Code, Name, ResourceType, ResourceSort, Quantity, Unit, ChangeFactor1, Cost, TotalNetCost, Note]),

        new("Ekonomi",
            [Code, Name, Account, Cost, TotalNetCost, MinPrice, CeilingPrice, Diff, ChangeFactor1, Note]),

        new("Anbud",
            [Code, Name, Account, Quantity, Unit, PriceQ, PriceTotaly, PriceTotallyTax]),

        new("Produktion",
            [Code, Name, ResourceType, ResourceSort, Quantity, Unit, PriceProduction, WorkedQ, ActuallyQuantity, Responsible, Note]),

        new("CO2 / miljö",
            [Code, Name, ResourceType, Quantity, Unit, Co2, TotalCo2, Note]),

        new("Granskning / uppföljning",
            [Code, Name, Status, Responsible, Diff, CeilingPrice, MinPrice, Note]),

        new("Import / avstämning",
            [Code, Name, Account, Status, ImportInfo, Quantity, Unit, TotalNetCost, Note]),
    ];

    /// <summary>Standard appearance templates (name + whether it is the dark variant).</summary>
    public static readonly (string Name, bool Dark)[] StandardAppearanceTemplates =
    [
        ("Standard ljus", false),
        ("Standard mörk", true),
    ];

    /// <summary>Builds an ordered <see cref="NetColumnState"/> list, reusing the default widths.</summary>
    public static List<NetColumnState> BuildColumns(NetColumnId[] ids)
    {
        var defaults = TemplateDefaults.NetCalc().ToDictionary(x => x.Id, x => x);
        return ids
            .Select(id => defaults.TryGetValue(id, out var d)
                ? new NetColumnState { Id = id, Width = d.Width, Frozen = false }
                : new NetColumnState { Id = id, Width = 80, Frozen = false })
            .ToList();
    }

    /// <summary>Light template keeps the defaults; the dark one uses a genuinely dark palette so the two
    /// standard appearance templates look clearly different in the preview.</summary>
    public static TemplateData BuildAppearance(bool dark)
    {
        var data = new TemplateData();
        if (!dark)
            return data;

        data.NetCalc.Color = new NetColor
        {
            Header = "#1f2937",
            Note = "#374151",
            Border = "#475569",
            BorderStyle = "solid",
            Text = "#e5e7eb",
            Task = "#111827",
            SubTask = "#1f2937",
            Resource = "#0f172a",
            TaskCodeName = "#374151",
            TaskDetailBaseQuantity = "#1e293b",
            ResourceParameter = "#334155",
            ResourceAttachment = "#3f3f46",
            ResourceTime = "#292524",
            InactiveText = "#9ca3af",
        };

        data.SummarySheet.Color = new SummarySheetColor
        {
            Header = "#1f2937",
            Note = "#374151",
            Border = "#475569",
            BorderStyle = "solid",
            Text = "#e5e7eb",
            Sum = "#3730a3",
            Factor = "#3730a3",
        };

        return data;
    }

    public static IEnumerable<(ResourceTypesEnum Kind, string Name, int SortOrder)> ResourceTypes()
    {
        yield return (ResourceTypesEnum.Materials,             "Material",                 100);
        yield return (ResourceTypesEnum.MachinesAndEquipments, "Maskiner och utrustning",  200);
        yield return (ResourceTypesEnum.Worker,                "Arbetare",                 300);
        yield return (ResourceTypesEnum.Managers,              "Tjänstemän / ledning",     400);
        yield return (ResourceTypesEnum.Design,                "Projektering / design",    500);
        yield return (ResourceTypesEnum.Subcontractors,        "Underleverantörer",        600);
        yield return (ResourceTypesEnum.ProjectOverheadCosts,  "Projektomkostnader",       700);
        yield return (ResourceTypesEnum.overheadCosts,         "Allmänna omkostnader",     800);
        yield return (ResourceTypesEnum.Risk,                  "Risk",                     900);
        yield return (ResourceTypesEnum.Adjustment,            "Justering",               1000);
        yield return (ResourceTypesEnum.Information,           "Information",             1100);
    }
}
