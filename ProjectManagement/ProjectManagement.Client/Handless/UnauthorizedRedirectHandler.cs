namespace ProjectManagement.Client.Handless
{
    using Microsoft.AspNetCore.Components;
    using ProjectManagement.Client.Shared.Repositories;
    using System.Net;
    public class UnauthorizedRedirectHandler(IErrorDialog ui, NavigationManager nav) : DelegatingHandler
    {
        private static DateTime _lastDialogUtc = DateTime.MinValue;
        private const string LoginPath = "/login";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = await base.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                ShowOnce("تسجيل الدخول", "انتهت الجلسة أو غير مصرح. سيتم تحويلك لتسجيل الدخول.");
                nav.NavigateTo(LoginPath, forceLoad: true);
            }

            return response;
        }

        private void ShowOnce(string title, string message, string? traceId = null)
        {
            var now = DateTime.UtcNow;
            if ((now - _lastDialogUtc).TotalSeconds < 3) return;

            _lastDialogUtc = now;
            ui.Show(title, message, traceId);
        }
    }
}
