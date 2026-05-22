namespace ProjectManagement.Client.Helper
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Authorization;
    using Microsoft.Extensions.Logging;
    using ProjectManagement.Shared.Constant;
    using ProjectManagement.Shared.DTO.General;
    using System.Net.Http.Json;
    using System.Security.Claims;

    public interface IClientLogger
    {
        Task ErrorAsync(string message, string? traceId = null, Exception? ex = null);
        Task WarnAsync(string message, string? traceId = null);
        Task InfoAsync(string message, string? traceId = null);
    }

    public class ClientLogger(
        IHttpClientFactory factory,
        NavigationManager nav,
        AuthenticationStateProvider authStateProvider,
        ILogger<ClientLogger> logger) : IClientLogger
    {
        private const int MaxMessageLength = 500;
        private const int MaxExceptionLength = 1_500;
        private readonly HttpClient _http = factory.CreateClient("Log");

        public Task ErrorAsync(string message, string? traceId = null, Exception? ex = null)
            => LogAsync("Error", message, traceId, ex);

        public Task WarnAsync(string message, string? traceId = null)
            => LogAsync("Warning", message, traceId, null);

        public Task InfoAsync(string message, string? traceId = null)
            => LogAsync("Info", message, traceId, null);

        private async Task LogAsync(string level, string message, string? traceId, Exception? ex)
        {
            try
            {
                var user = (await authStateProvider.GetAuthenticationStateAsync()).User;
                var evt = new ClientLogEvent(
                    Level: level,
                    Message: Trim(message, MaxMessageLength),
                    TraceId: TrimOrNull(traceId, 128),
                    Url: BuildSafeUrl(nav.Uri),
                    UserId: GetClaim(user, PMClaimsConst.UserId, ClaimTypes.NameIdentifier),
                    TenantId: GetClaim(user, PMClaimsConst.Tenant),
                    Exception: BuildSafeExceptionText(ex),
                    ClientTimeUtc: DateTimeOffset.UtcNow);

                await _http.PostAsJsonAsync("api/client-logs", evt);
            }
            catch (Exception logException)
            {
                logger.LogDebug(logException, "Failed to send client log event.");
            }
        }

        private static string? BuildSafeUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return null;

            return uri.AbsolutePath;
        }

        private static string? BuildSafeExceptionText(Exception? ex)
        {
            if (ex is null)
                return null;

            var message = $"{ex.GetType().Name}: {ex.Message}";
            if (ex.InnerException is not null)
            {
                message += $" | Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
            }

            return Trim(message, MaxExceptionLength);
        }

        private static string? GetClaim(ClaimsPrincipal user, params string[] claimTypes)
        {
            foreach (var claimType in claimTypes)
            {
                var value = user.FindFirst(claimType)?.Value;
                if (!string.IsNullOrWhiteSpace(value))
                    return Trim(value, 128);
            }

            return null;
        }

        private static string? TrimOrNull(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return Trim(value, max);
        }

        private static string Trim(string value, int max)
            => value.Length <= max ? value : value[..max];
    }
}
