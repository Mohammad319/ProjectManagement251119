using ProjectManagement.Shared.Constant;

namespace ProjectManagement.Services.UI;

public static class PermissionDisplay
{
    public const string PermissionLabel = "Behörighet";
    public const string AllPermissionsLabel = "Alla behörigheter";
    public const string ChoosePermissionLabel = "Välj behörighet";
    public const string MissingPermissionLabel = "Ej angiven";

    public const string Administrator = "Administratör";
    public const string CalculationUser = "Kalkylanvändare";
    public const string Reader = "Läsare";

    public const string Administrators = "Administratörer";
    public const string CalculationUsers = "Kalkylanvändare";
    public const string Readers = "Läsare";

    public const string AdministratorDescription =
        "Kan hantera inställningar, användare, avdelningar och företagsdata.";

    public const string CalculationUserDescription =
        "Kan skapa och ändra kalkyler/projekt inom sina behöriga avdelningar.";

    public const string ReaderDescription =
        "Kan läsa information men inte ändra viktiga uppgifter.";

    public static string Label(string? role) => role switch
    {
        PMRolesConst.Tenant.Admin or PMRolesConst.APP.Admin or "Admin" or "Administrator" or "Administratör"
            => Administrator,
        PMRolesConst.Tenant.Manger or PMRolesConst.APP.Manger or "User" or "Användare" or "Kalkylanvändare"
            => CalculationUser,
        PMRolesConst.Tenant.User or PMRolesConst.Tenant.Viewer or PMRolesConst.APP.User or "Viewer" or "Visare" or "Läsare"
            => Reader,
        _ => string.IsNullOrWhiteSpace(role) ? MissingPermissionLabel : role
    };

    public static string PluralLabel(string? role) => role switch
    {
        PMRolesConst.Tenant.Admin or PMRolesConst.APP.Admin => Administrators,
        PMRolesConst.Tenant.Manger or PMRolesConst.APP.Manger => CalculationUsers,
        PMRolesConst.Tenant.User or PMRolesConst.Tenant.Viewer or PMRolesConst.APP.User => Readers,
        _ => Label(role)
    };

    public static string Description(string? role) => Label(role) switch
    {
        Administrator => AdministratorDescription,
        CalculationUser => CalculationUserDescription,
        Reader => ReaderDescription,
        _ => "Behörighet: Ej angiven"
    };

    public static string Tooltip(string? role)
        => $"{Label(role)}: {Description(role)}";
}
