using AuthPermissions.Context;
using System.Globalization;

namespace ProjectManagement.Adminstrator.Services.UserNotifications;

internal sealed class NullUserLifecycleNotificationPreparer : IUserLifecycleNotificationPreparer
{
    private readonly ILogger<NullUserLifecycleNotificationPreparer> _logger;

    public NullUserLifecycleNotificationPreparer(ILogger<NullUserLifecycleNotificationPreparer> logger)
    {
        _logger = logger;
    }

    public Task OnUserCreatedAsync(ApplicationUser user, string initialPassword, string? cultureName = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Prepared localized user-created notification for {Email} using culture {Culture}. Delivery will be enabled in the next phase.",
            user.Email,
            cultureName ?? CultureInfo.CurrentUICulture.Name);

        return Task.CompletedTask;
    }

    public Task OnPasswordChangedAsync(ApplicationUser user, string? cultureName = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Prepared localized password-change notification for {Email} using culture {Culture}. Delivery will be enabled in the next phase.",
            user.Email,
            cultureName ?? CultureInfo.CurrentUICulture.Name);

        return Task.CompletedTask;
    }
}
