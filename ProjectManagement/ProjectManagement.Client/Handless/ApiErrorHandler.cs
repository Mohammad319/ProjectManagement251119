using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.Repositories;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectManagement.Client.Handless
{
    public class ApiErrorHandler(IErrorDialog ui, IClientLogger clientLogger) : DelegatingHandler
    {
        private DateTime _lastDialogUtc = DateTime.MinValue;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.RequestUri?.ToString().Contains("api/client-logs", StringComparison.OrdinalIgnoreCase) == true)
            {
                return await base.SendAsync(request, ct); // لا Dialog ولا parsing
            }

            HttpResponseMessage response;

            try
            {
                response = await base.SendAsync(request, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                ShowOnce("تعذر الاتصال", "لا يمكن الاتصال بالسيرفر. تحقق من الإنترنت ثم حاول مرة أخرى.");
                _ = clientLogger.ErrorAsync("Network error while calling API", traceId: null, ex: ex);
                throw;
            }
            catch (Exception ex)
            {
                ShowOnce("خطأ", "حدث خطأ غير متوقع.");
                _ = clientLogger.ErrorAsync("Unexpected client error while calling API", traceId: null, ex: ex);
                throw;
            }

            if (response.IsSuccessStatusCode)
                return response;

            // ✅ تجاهل 401 هنا (يتولاه UnauthorizedRedirectHandler)
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return response;

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                ShowOnce("صلاحيات", "ليس لديك صلاحية لتنفيذ هذه العملية.");
                var endpoint = request.RequestUri?.ToString() ?? "(unknown-endpoint)";
                _ = clientLogger.ErrorAsync($"Forbidden (403) from API {endpoint}", traceId: null, ex: null);
                return response;
            }

            var contentType = response.Content?.Headers?.ContentType?.MediaType ?? "";
            var body = await SafeReadAsync(response, ct);

            // ProblemDetails
            if (contentType.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase))
            {
                var pd = TryParseProblemDetails(body);

                var title = pd?.Title ?? "فشل الطلب";
                var detail = pd?.Detail ?? "تعذر إتمام الطلب.";
                var traceId = pd?.TraceId;

                ShowOnce(title, detail, traceId);

                // ✅ body نص، نحوله إلى Exception حتى يوافق توقيع IClientLogger
                _ = clientLogger.ErrorAsync($"API ProblemDetails: {title}", traceId, new Exception(body));

                return response;
            }

            // HTML
            if (LooksLikeHtml(body))
            {
                ShowOnce("خطأ في السيرفر", "حدث خطأ في السيرفر. حاول لاحقًا.");
                _ = clientLogger.ErrorAsync("API returned HTML error page", traceId: null, new Exception(body));
                return response;
            }

            // نص عادي
            var msg = string.IsNullOrWhiteSpace(body) ? "تعذر إتمام الطلب." : Trim(body, 300);
            ShowOnce("فشل الطلب", msg);
            _ = clientLogger.ErrorAsync("API returned non-success response", traceId: null, new Exception(body));

            return response;
        }

        private void ShowOnce(string title, string message, string? traceId = null)
        {
            var now = DateTime.UtcNow;
            if ((now - _lastDialogUtc).TotalSeconds < 2) return;

            _lastDialogUtc = now;
            ui.Show(title, message, traceId);
        }

        private static async Task<string> SafeReadAsync(HttpResponseMessage resp, CancellationToken ct)
        {
            try { return resp.Content is null ? "" : await resp.Content.ReadAsStringAsync(ct); }
            catch { return ""; }
        }

        private static bool LooksLikeHtml(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            var t = s.TrimStart();
            return t.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
                || t.StartsWith("<html", StringComparison.OrdinalIgnoreCase)
                || t.Contains("<body", StringComparison.OrdinalIgnoreCase);
        }

        private static string Trim(string s, int max) => s.Length <= max ? s : s[..max];

        private static ApiProblemDetails? TryParseProblemDetails(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<ApiProblemDetails>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        private sealed class ApiProblemDetails
        {
            public string? Title { get; set; }
            public string? Detail { get; set; }
            [JsonExtensionData]
            public Dictionary<string, JsonElement>? Extensions { get; set; }

            public string? TraceId =>
                Extensions != null && Extensions.TryGetValue("traceId", out var v) ? v.GetString() ?? v.ToString() : null;
        }
    }
}
