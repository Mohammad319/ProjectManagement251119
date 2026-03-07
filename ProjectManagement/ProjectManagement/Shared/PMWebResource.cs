using System.Globalization;
using System.Resources;

namespace ProjectManagement.Shared;

/// <summary>
/// Marker + strongly-typed helper for PM web-only UI strings.
/// Non-static so it can be used with IStringLocalizer<PMWebResource>.
/// </summary>
public class PMWebResource
{
    private static readonly ResourceManager ResourceManager = new("ProjectManagement.Shared.PMWebResource", typeof(PMWebResource).Assembly);

    public static CultureInfo? Culture { get; set; }

    public static string Settings => GetString(nameof(Settings), "Settings");
    public static string AdminSettings => GetString(nameof(AdminSettings), "Organisation settings");
    public static string SettingsDescription => GetString(nameof(SettingsDescription), "Manage application settings and preferences.");
    public static string Close => GetString(nameof(Close), "Close");
    public static string ToggleLanguage => GetString(nameof(ToggleLanguage), "Change language");
    public static string ToggleTheme => GetString(nameof(ToggleTheme), "Toggle theme");
    public static string PageNotFoundTitle => GetString(nameof(PageNotFoundTitle), "Page not found");
    public static string PageNotFoundMessage => GetString(nameof(PageNotFoundMessage), "Sorry, the page you are looking for does not exist or is no longer available.");
    public static string ReconnectFirstAttempt => GetString(nameof(ReconnectFirstAttempt), "Rejoining the server...");
    public static string ReconnectRetrying => GetString(nameof(ReconnectRetrying), "Rejoin failed... trying again in");
    public static string SecondsSuffix => GetString(nameof(SecondsSuffix), "seconds.");
    public static string ReconnectFailed => GetString(nameof(ReconnectFailed), "Failed to rejoin. Please retry or reload the page.");
    public static string Retry => GetString(nameof(Retry), "Retry");
    public static string SessionPausedByServer => GetString(nameof(SessionPausedByServer), "The session has been paused by the server.");
    public static string Resume => GetString(nameof(Resume), "Resume");
    public static string ResumeFailed => GetString(nameof(ResumeFailed), "Failed to resume the session. Please reload the page.");
    public static string SettingsMenu => GetString(nameof(SettingsMenu), "Settings menu");

    public static string Actions => GetString(nameof(Actions), "Actions");
    public static string BasicInformation => GetString(nameof(BasicInformation), "Basic information");
    public static string Category => GetString(nameof(Category), "Category");
    public static string LoadingOrganisationDetails => GetString(nameof(LoadingOrganisationDetails), "Loading organisation details...");
    public static string NewCategory => GetString(nameof(NewCategory), "New category");
    public static string NewOrganisation => GetString(nameof(NewOrganisation), "New organisation");
    public static string NewOrganisationType => GetString(nameof(NewOrganisationType), "New organisation type");
    public static string NoContactsAvailable => GetString(nameof(NoContactsAvailable), "No contacts available.");
    public static string NoNotesAdded => GetString(nameof(NoNotesAdded), "No notes added.");
    public static string NoOrganisationCategories => GetString(nameof(NoOrganisationCategories), "No organisation categories found.");
    public static string NoOrganisationTypesFound => GetString(nameof(NoOrganisationTypesFound), "No organisation types found.");
    public static string NoOrganisationsFound => GetString(nameof(NoOrganisationsFound), "No organisations found.");
    public static string Notes => GetString(nameof(Notes), "Notes");
    public static string OpenOrganisationTypes => GetString(nameof(OpenOrganisationTypes), "Open organisation types");
    public static string OrganisationNotesHint => GetString(nameof(OrganisationNotesHint), "Add internal notes for this organisation.");
    public static string OrganisationOverviewTitle => GetString(nameof(OrganisationOverviewTitle), "Organisation overview");
    public static string OrganisationTypesDescription => GetString(nameof(OrganisationTypesDescription), "Manage the organisation types available for selection.");
    public static string OrganisationTypesTitle => GetString(nameof(OrganisationTypesTitle), "Organisation types");
    public static string OrganisationsInCategoryDescription => GetString(nameof(OrganisationsInCategoryDescription), "Organisations linked to the selected category.");
    public static string RemoveNote => GetString(nameof(RemoveNote), "Remove note");
    public static string SelectOrganisationCategoryHint => GetString(nameof(SelectOrganisationCategoryHint), "Select a category to view organisations.");

    public static string DepartmentsTitle => GetString(nameof(DepartmentsTitle), "Departments");
    public static string DepartmentsDescription => GetString(nameof(DepartmentsDescription), "Manage departments and their users.");
    public static string NewDepartment => GetString(nameof(NewDepartment), "New department");
    public static string AllUsersHint => GetString(nameof(AllUsersHint), "Open all users across departments.");
    public static string QuickAction => GetString(nameof(QuickAction), "Quick action");
    public static string OpenAllUsers => GetString(nameof(OpenAllUsers), "Open all users");
    public static string UsersLabel => GetString(nameof(UsersLabel), "Users");
    public static string AdminUsersDescription => GetString(nameof(AdminUsersDescription), "Manage department users and their access.");
    public static string AllUsersLabel => GetString(nameof(AllUsersLabel), "All users");
    public static string NoDepartmentsMessage => GetString(nameof(NoDepartmentsMessage), "No departments found.");
    public static string DepartmentUsersDescription => GetString(nameof(DepartmentUsersDescription), "Manage users belonging to the selected department.");
    public static string BackToDepartments => GetString(nameof(BackToDepartments), "Back to departments");
    public static string RecreateAuthUser => GetString(nameof(RecreateAuthUser), "Recreate login");
    public static string RemoveFromRegisterOnly => GetString(nameof(RemoveFromRegisterOnly), "Remove login only");
    public static string Role => GetString(nameof(Role), "Role");
    public static string AuthStatus => GetString(nameof(AuthStatus), "Auth status");
    public static string UserRegistered => GetString(nameof(UserRegistered), "Registered");
    public static string UserMissingAuth => GetString(nameof(UserMissingAuth), "Missing login");
    public static string Lockout => GetString(nameof(Lockout), "Lockout");
    public static string Enabled => GetString(nameof(Enabled), "Enabled");
    public static string Disabled => GetString(nameof(Disabled), "Disabled");
    public static string LockoutStart => GetString(nameof(LockoutStart), "Lockout start");
    public static string LockoutEnd => GetString(nameof(LockoutEnd), "Lockout end");
    public static string NoUsersFound => GetString(nameof(NoUsersFound), "No users found.");
    public static string DepartmentUsersTitle => GetString(nameof(DepartmentUsersTitle), "Department users");
    public static string AllTenantUsersTitle => GetString(nameof(AllTenantUsersTitle), "All tenant users");
    public static string RegisterUserHint => GetString(nameof(RegisterUserHint), "Create a login for this user and assign a department/role.");
    public static string EditUserHint => GetString(nameof(EditUserHint), "Update user details, role and lockout settings.");
    public static string EditDepartmentHint => GetString(nameof(EditDepartmentHint), "Update the selected department.");
    public static string CreateDepartmentHint => GetString(nameof(CreateDepartmentHint), "Create a new department for the tenant.");
    public static string ToggleVisibility => GetString(nameof(ToggleVisibility), "Toggle visibility");

    private static string GetString(string name, string fallback)
        => ResourceManager.GetString(name, Culture) ?? fallback;
}
