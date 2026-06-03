using ProjectManagement.Shared.Enums;

namespace Persistence.Seeding;

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
    bool CountsAsLostBid = false);

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
        new("unit price contract", "#8b5cf6", 100),
        new("time and Materials contract", "#8b5cf6", 200),
        new("lump-sum contract", "#8b5cf6", 300),
        new("integrated project delivery contract", "#8b5c60", 400),
        new("incentive construction contract", "#805cf6", 500),
        new("guaranteed maximum price contract", "#8b5cf6", 600),
        new("design and build contract", "#8b5cf6", 700),
        new("cost-plus construction contract", "#00ff00", 800),
    ];

    public static readonly LookupSeed[] Contracts =
    [
        new("Traditional procurement", "#0ea5e9", 100),
        new("Design & Build Contract", "#00a590", 200),
    ];

    public static readonly LookupSeed[] ProcurementMethods =
    [
        new("Limited Procedure", "#00ff00", 100),
        new("Selective Tending", "#00ff00", 200),
        new("Open Tendering", "#00ff00", 300),
    ];

    public static readonly CalcStatusSeed[] CalculationStatuses =
    [
        new("Utkast",                 "#64748B", 50),
        new("Förfrågan",              "#6B7280", 100),
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
        new("Förfrågan",                "#6B7280", 100),
        new("Pågående",                 "#1D4ED8", 200),
        new("Inlämnat / väntar beslut", "#EAB308", 300, CountsAsSubmittedBid: true),
        new("Vunnet",                   "#166534", 400, CountsAsSubmittedBid: true, CountsAsWonBid: true),
        new("Förlorat",                 "#DC2626", 500, CountsAsSubmittedBid: true, CountsAsLostBid: true),
        new("Ej inlämnat",              "#F97316", 600),
        new("Ej intressant",            "#FEF08A", 700),
        new("Avbrutet",                 "#7C2D12", 800),
    ];

    public static readonly LookupSeed[] ProjectTypes =
    [
        new("General", "#3b82f6", 10),
    ];

    public static IEnumerable<(ResourceTypesEnum Kind, int SortOrder)> ResourceTypes()
    {
        var order = 10;
        foreach (var type in Enum.GetValues<ResourceTypesEnum>())
        {
            yield return (type, order);
            order += 10;
        }
    }
}
