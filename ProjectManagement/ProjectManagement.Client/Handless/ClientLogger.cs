namespace ProjectManagement.Client.Helper
{
    using Microsoft.AspNetCore.Components;
    using System.Net.Http.Json;
    public interface IClientLogger
    {
        Task ErrorAsync(string message, string? traceId = null, Exception? ex = null);
    }
    public class ClientLogger(IHttpClientFactory factory, NavigationManager nav) : IClientLogger
    {
        private readonly HttpClient _http = factory.CreateClient("Api");

        public async Task ErrorAsync(string message, string? traceId = null, Exception? ex = null)
        {
            try
            {
                var evt = new
                {
                    Level = "Error",
                    Message = message,
                    TraceId = traceId,
                    Url = nav.Uri,
                    Exception = ex?.ToString(),
                    ClientTimeUtc = DateTimeOffset.UtcNow
                };

                await _http.PostAsJsonAsync("api/client-logs", evt);
            }
            catch
            {
                // لا تفجّر التطبيق لو فشل إرسال اللوج
            }
        }
    }

}
