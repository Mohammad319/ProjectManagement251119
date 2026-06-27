namespace ProjectManagement.Client.Handless;

using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.Repositories;
using ProjectManagement.Shared.Helper;
using System.Net;

public class UnauthorizedRedirectHandler(
    NavigationManager nav,
    AuthRedirectState redirectState,
    IClientLogger clientLogger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var response = await base.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // The workspace fires many calls at once; a bad session 401s them all. Only the first one
            // performs the single full-page recovery navigation — the rest stand down so we never emit
            // a burst of /auth/refresh requests (which the server answers with 429) or flicker the UI.
            if (redirectState.TryBeginRedirect())
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
            }

            // Block until the page navigation cancels all pending requests (whether this call started
            // the redirect or another concurrent 401 already did). This prevents callers from processing
            // the 401 response and showing error states.
            using var safetyCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            safetyCts.CancelAfter(TimeSpan.FromSeconds(8));
            try { await Task.Delay(Timeout.Infinite, safetyCts.Token); }
            catch (OperationCanceledException) { }
        }

        return response;
    }

    private string GetCurrentLocalUrl()
    {
        var uri = new Uri(nav.Uri);
        return AuthRecoveryPathHelper.NormalizeLocalUrl($"{uri.PathAndQuery}{uri.Fragment}");
    }
}
