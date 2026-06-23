using System.Globalization;
using ProjectManagement.Shared;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Notification;
using ProjectManagement.Shared.Enums;

namespace ProjectManagement.Services.UI;

/// <summary>
/// Renders a notification's title/message/labels from its structured fields in the reader's current
/// language (sv/en) via <see cref="PMWebResource"/>. Kept UI-free so the bell component stays thin.
/// </summary>
public static class NotificationPresenter
{
    public static string Title(NotificationListItemDTO n) => n.Type switch
    {
        NotificationType.ProjectSharedWithUser => PMWebResource.NotificationTitleSharedWithUser,
        NotificationType.ProjectSharedWithDepartment => PMWebResource.NotificationTitleSharedWithDepartment,
        NotificationType.ProjectAccessChanged => PMWebResource.NotificationTitleAccessChanged,
        NotificationType.ProjectAccessRemoved => PMWebResource.NotificationTitleAccessRemoved,
        NotificationType.ProjectShareValidityChanged => PMWebResource.NotificationTitleValidityChanged,
        NotificationType.ProjectSharedCalculationsChanged => PMWebResource.NotificationTitleCalculationsChanged,
        _ => string.Empty
    };

    public static string Message(NotificationListItemDTO n)
    {
        var project = string.IsNullOrWhiteSpace(n.ProjectName) ? "—" : n.ProjectName!;

        return n.Type switch
        {
            NotificationType.ProjectSharedWithUser =>
                Format(PMWebResource.NotificationMsgSharedWithUser, n.ActorName ?? "", project)
                + "\n" + AccessSentence(n.Role) + "\n" + Validity(n.ValidUntil),

            NotificationType.ProjectSharedWithDepartment =>
                Format(PMWebResource.NotificationMsgSharedWithDepartment, project, n.DepartmentName ?? "")
                + "\n" + AccessSentence(n.Role) + "\n" + Validity(n.ValidUntil),

            NotificationType.ProjectAccessChanged =>
                Format(PMWebResource.NotificationMsgAccessChanged, project)
                + "\n" + AccessSentence(n.Role) + "\n" + Validity(n.ValidUntil),

            NotificationType.ProjectShareValidityChanged =>
                Format(PMWebResource.NotificationMsgValidityChanged, project) + "\n" + Validity(n.ValidUntil),

            NotificationType.ProjectSharedCalculationsChanged =>
                Format(PMWebResource.NotificationMsgCalculationsChanged, project),

            NotificationType.ProjectAccessRemoved =>
                Format(PMWebResource.NotificationMsgAccessRemoved, project),

            _ => string.Empty
        };
    }

    /// <summary>Whether an access-level / calculation chip is meaningful for this notification type.</summary>
    public static bool ShowsAccessMeta(NotificationType type) => type is
        NotificationType.ProjectSharedWithUser or
        NotificationType.ProjectSharedWithDepartment or
        NotificationType.ProjectAccessChanged;

    public static string? AccessLevelLabel(NotificationListItemDTO n)
    {
        if (!ShowsAccessMeta(n.Type) || string.IsNullOrWhiteSpace(n.Role))
            return null;

        return IsEdit(n.Role) ? PMWebResource.NotificationLevelEdit : PMWebResource.NotificationLevelView;
    }

    public static string? CalculationsInfo(NotificationListItemDTO n)
    {
        if (!ShowsAccessMeta(n.Type) || n.CalcCount <= 0)
            return null;

        if (n.CalcAllAvailable)
            return PMWebResource.NotificationCalcAll;

        return n.CalcCount == 1
            ? PMWebResource.NotificationCalcOne
            : Format(PMWebResource.NotificationCalcMany, n.CalcCount);
    }

    private static bool IsEdit(string? role) =>
        role is PMRolesConst.Tenant.Admin or PMRolesConst.Tenant.Manger;

    private static string AccessSentence(string? role) =>
        IsEdit(role) ? PMWebResource.NotificationSentenceCanEdit : PMWebResource.NotificationSentenceCanView;

    private static string Validity(DateTime? validUntil) =>
        validUntil.HasValue
            ? Format(PMWebResource.NotificationValidityUntil, validUntil.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            : PMWebResource.NotificationValidityIndefinite;

    private static string Format(string template, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, template, args);
}
