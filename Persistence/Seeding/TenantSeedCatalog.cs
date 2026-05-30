using ProjectManagement.Shared.Enums;

namespace Persistence.Seeding;

public readonly record struct LookupSeed(string Name, string Color, int SortOrder, bool IsVisible = true);

public readonly record struct CalcStatusSeed(
    string Name, string Color, int SortOrder,
    bool IsVisible = true,
    bool IsApprovalStatus = false,
    bool LocksCalculation = false,
    bool AllowsProductionCalculation = false);

public readonly record struct AccountSeed(string Code, string Name, string GroupName, bool IsVisible = true);

public static class TenantSeedCatalog
{
    public static readonly LookupSeed[] ResourceStatuses =
    [
        new("Active", "#16a34a", 100),
        new("Inactive", "#dc2626", 200),
    ];

    public static readonly LookupSeed[] TaskStatuses =
    [
        new("Planned", "#2563eb", 100),
        new("In Progress", "#f59e0b", 200),
        new("Done", "#16a34a", 300),
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
        new("Förfrågan",              "#6B7280", 100),
        new("Planerad",               "#93C5FD", 200),
        new("Pågående",               "#1D4ED8", 300),
        new("Behöver granskas",       "#5EEAD4", 400),
        new("Granskad",               "#0D9488", 500),
        new("Godkänd / låst",         "#0F766E", 600, IsApprovalStatus: true, LocksCalculation: true, AllowsProductionCalculation: true),
        new("Skickad / inlämnad",     "#EAB308", 700, IsApprovalStatus: true, LocksCalculation: true, AllowsProductionCalculation: true),
        new("Tilldelad / vunnen",     "#166534", 800, IsApprovalStatus: true, LocksCalculation: true, AllowsProductionCalculation: true),
        new("Förlorad",               "#DC2626", 900),
        new("Avbruten",               "#F97316", 1000),
        new("Ej intressant / ej lämnat", "#FEF08A", 1100),
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
