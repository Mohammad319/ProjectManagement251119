using AuthPermissions.Context;

namespace ProjectManagement.Adminstrator.Services.UserNotifications;

public interface IUserLifecycleNotificationPreparer
{
    Task OnUserCreatedAsync(ApplicationUser user, string initialPassword, string? cultureName = null, CancellationToken cancellationToken = default);
    Task OnPasswordChangedAsync(ApplicationUser user, string? cultureName = null, CancellationToken cancellationToken = default);
}
