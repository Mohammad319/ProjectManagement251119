using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using ProjectManagement.Client.Configuration;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.Repositories;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Shared.Helper;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectManagement.Client.Handless;

public class ApiErrorHandler(
    IErrorDialog ui,
    IClientLogger clientLogger,
    NavigationManager navigationManager,
    IStringLocalizer<ResourceErrors> errLoc,
    ILogger<ApiErrorHandler> logger,
    ClientApiOptions options) : DelegatingHandler
{
    private const int MaxDialogBodyLength = 300;
    private const int MaxLogBodyLength = 4_000;
    private DateTime _lastDialogUtc = DateTime.MinValue;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.RequestUri?.ToString().Contains("api/client-logs", StringComparison.OrdinalIgnoreCase) == true)
            return await base.SendAsync(request, ct);

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
            ShowOnce(errLoc["networkUnavailableTitle"], errLoc["networkUnavailableMessage"]);
            logger.LogError(ex, "Network error while calling API {RequestUri}", request.RequestUri);
            _ = clientLogger.ErrorAsync("Network error while calling API", traceId: null, ex: ex);
            throw;
        }
        catch (Exception ex)
        {
            ShowOnce(ResourceApp.error, ResourceApp.AnUnexpectedErrorHasOccurred);
            logger.LogError(ex, "Unexpected client error while calling API {RequestUri}", request.RequestUri);
            _ = clientLogger.ErrorAsync("Unexpected client error while calling API", traceId: null, ex: ex);
            throw;
        }

        if (response.IsSuccessStatusCode)
            return response;

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return response;

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var currentLocalUrl = GetCurrentLocalUrl();

            if (!AuthRecoveryPathHelper.HasRetryFlag(currentLocalUrl))
            {
                ShowOnce(errLoc["permissionsTitle"], errLoc["permissionsRefreshMessage"]);
                logger.LogWarning("Forbidden API response recovered via refresh for {RequestUri}", request.RequestUri);
                _ = clientLogger.ErrorAsync($"Forbidden (403) recovered via refresh for {request.RequestUri}", traceId: null, ex: null);
                navigationManager.NavigateTo(AuthRecoveryPathHelper.BuildRefreshUrl(currentLocalUrl), forceLoad: true);
                return response;
            }

            ShowOnce(errLoc["permissionsTitle"], errLoc["permissionsDeniedMessage"]);
            var endpoint = request.RequestUri?.ToString() ?? "(unknown-endpoint)";
            logger.LogWarning("Forbidden API response from {RequestUri}", request.RequestUri);
            _ = clientLogger.ErrorAsync($"Forbidden (403) from API {endpoint}", traceId: null, ex: null);
            return response;
        }

        var contentType = response.Content?.Headers?.ContentType?.MediaType ?? string.Empty;
        var body = await SafeReadAsync(response, ct);

        if (contentType.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase))
        {
            var problemDetails = TryParseProblemDetails(body);
            var title = problemDetails?.Title ?? errLoc["requestFailedTitle"];
            var detail = BuildSafeDialogMessage(problemDetails?.Detail, errLoc["requestFailedMessage"]);
            var traceId = problemDetails?.TraceId;

            ShowOnce(title, detail, traceId);
            logger.LogWarning(
                "API ProblemDetails from {RequestUri}. StatusCode: {StatusCode}. TraceId: {TraceId}",
                request.RequestUri,
                (int)response.StatusCode,
                traceId);
            _ = clientLogger.ErrorAsync($"API ProblemDetails: {title}", traceId, BuildBodyException(body));

            return response;
        }

        if (LooksLikeHtml(body))
        {
            ShowOnce(errLoc["serverErrorTitle"], errLoc["serverErrorMessage"]);
            logger.LogWarning("API returned HTML error page from {RequestUri}. StatusCode: {StatusCode}", request.RequestUri, (int)response.StatusCode);
            _ = clientLogger.ErrorAsync("API returned HTML error page", traceId: null, ex: BuildBodyException(body));
            return response;
        }

        var message = BuildSafeDialogMessage(body, errLoc["requestFailedMessage"]);
        ShowOnce(errLoc["requestFailedTitle"], message);
        logger.LogWarning("API returned non-success response from {RequestUri}. StatusCode: {StatusCode}", request.RequestUri, (int)response.StatusCode);
        _ = clientLogger.ErrorAsync("API returned non-success response", traceId: null, ex: BuildBodyException(body));

        return response;
    }

    private string BuildSafeDialogMessage(string? detail, string fallback)
    {
        if (!options.ShowDetailedErrors || string.IsNullOrWhiteSpace(detail))
            return fallback;

        return Trim(detail, MaxDialogBodyLength);
    }

    private void ShowOnce(string title, string message, string? traceId = null)
    {
        var now = DateTime.UtcNow;
        if ((now - _lastDialogUtc).TotalSeconds < 2)
            return;

        _lastDialogUtc = now;
        ui.Show(title, message, traceId);
    }

    private static async Task<string> SafeReadAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return response.Content is null ? string.Empty : await response.Content.ReadAsStringAsync(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool LooksLikeHtml(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return false;

        var trimmed = body.TrimStart();
        return trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("<body", StringComparison.OrdinalIgnoreCase);
    }

    private static Exception? BuildBodyException(string body)
        => string.IsNullOrWhiteSpace(body) ? null : new Exception(Trim(body, MaxLogBodyLength));

    private static string Trim(string value, int max)
        => value.Length <= max ? value : value[..max];

    private static readonly JsonSerializerOptions _problemDetailsOptions = new(JsonSerializerDefaults.Web);

    private static ApiProblemDetails? TryParseProblemDetails(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ApiProblemDetails>(json, _problemDetailsOptions);
        }
        catch
        {
            return null;
        }
    }

    private string GetCurrentLocalUrl()
    {
        var uri = new Uri(navigationManager.Uri);
        return AuthRecoveryPathHelper.NormalizeLocalUrl($"{uri.PathAndQuery}{uri.Fragment}");
    }

    private sealed class ApiProblemDetails
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extensions { get; set; }

        public string? TraceId =>
            Extensions != null && Extensions.TryGetValue("traceId", out var value)
                ? value.GetString() ?? value.ToString()
                : null;
    }
}
