namespace ProjectManagement.Client.Handless;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.Repositories;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Shared.Helper;
using System.Net;

public class UnauthorizedRedirectHandler(
    IErrorDialog ui,
    NavigationManager nav,
    IClientLogger clientLogger,
    IStringLocalizer<ResourceApp> appLoc) : DelegatingHandler
{
    private DateTime _lastDialogUtc = DateTime.MinValue;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var response = await base.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var currentLocalUrl = GetCurrentLocalUrl();

            if (!AuthRecoveryPathHelper.HasRetryFlag(currentLocalUrl))
            {
                ShowOnce(appLoc["sessionTitle"], appLoc["sessionRefreshMessage"]);
                _ = clientLogger.ErrorAsync($"Unauthorized (401) recovered via refresh for {request.RequestUri}");
                nav.NavigateTo(AuthRecoveryPathHelper.BuildRefreshUrl(currentLocalUrl), forceLoad: true);
            }
            else
            {
                ShowOnce(appLoc["signInTitle"], appLoc["signInRedirectMessage"]);
                _ = clientLogger.ErrorAsync($"Unauthorized (401) redirected to login for {request.RequestUri}");
                nav.NavigateTo(AuthRecoveryPathHelper.BuildLoginUrl(currentLocalUrl), forceLoad: true);
            }
        }

        return response;
    }

    private void ShowOnce(string title, string message, string? traceId = null)
    {
        var now = DateTime.UtcNow;
        if ((now - _lastDialogUtc).TotalSeconds < 3)
            return;

        _lastDialogUtc = now;
        ui.Show(title, message, traceId);
    }

    private string GetCurrentLocalUrl()
    {
        var uri = new Uri(nav.Uri);
        return AuthRecoveryPathHelper.NormalizeLocalUrl($"{uri.PathAndQuery}{uri.Fragment}");
    }
}
