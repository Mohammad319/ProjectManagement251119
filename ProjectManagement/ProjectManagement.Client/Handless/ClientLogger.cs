namespace ProjectManagement.Client.Helper
{
    using Microsoft.AspNetCore.Components;
    using ProjectManagement.Shared.DTO.General;
    using System.Net.Http.Json;

    public interface IClientLogger
    {
        Task ErrorAsync(string message, string? traceId = null, Exception? ex = null);
    }

    public class ClientLogger(IHttpClientFactory factory, NavigationManager nav) : IClientLogger
    {
        private readonly HttpClient _http = factory.CreateClient("Log");

        public async Task ErrorAsync(string message, string? traceId = null, Exception? ex = null)
        {
            try
            {
                var evt = new ClientLogEvent(
                    Level: "Error",
                    Message: message,
                    TraceId: traceId,
                    Url: nav.Uri,
                    UserId: null,
                    TenantId: null,
                    Exception: ex?.ToString(),
                    ClientTimeUtc: DateTimeOffset.UtcNow);

                await _http.PostAsJsonAsync("api/client-logs", evt);
            }
            catch
            {
                // تجاهل — لا نريد استثناءً داخل معالج الاستثناء
            }
        }
    }
}
