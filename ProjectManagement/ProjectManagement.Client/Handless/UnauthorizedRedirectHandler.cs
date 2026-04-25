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
    IStringLocalizer<ResourceErrors> errLoc) : DelegatingHandler
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
                _ = clientLogger.ErrorAsync($"Unauthorized (401) recovered via refresh for {request.RequestUri}");
                nav.NavigateTo(AuthRecoveryPathHelper.BuildRefreshUrl(currentLocalUrl), forceLoad: true);
            }
            else
            {
                _ = clientLogger.ErrorAsync($"Unauthorized (401) redirected to login for {request.RequestUri}");
                nav.NavigateTo(AuthRecoveryPathHelper.BuildLoginUrl(currentLocalUrl), forceLoad: true);
            }

            // Block until the page navigation cancels all pending requests.
            // This prevents callers from processing the 401 response and showing error states.
            using var safetyCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            safetyCts.CancelAfter(TimeSpan.FromSeconds(8));
            try { await Task.Delay(Timeout.Infinite, safetyCts.Token); }
            catch (OperationCanceledException) { }
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
