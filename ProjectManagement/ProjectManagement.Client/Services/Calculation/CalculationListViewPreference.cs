using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.Repositories.UserSettings;
using ProjectManagement.Shared.Constant;

namespace ProjectManagement.Client.Services.Calculation;

/// <summary>
/// Persists the calculation list's visible columns and column widths per user.
/// The server (per tenant/user) is the source of truth; localStorage is kept
/// only as a fast cache and offline fallback.
/// </summary>
public sealed class CalculationListViewPreference(
    IJSRuntime js,
    IClientLogger clientLogger,
    AuthenticationStateProvider authenticationStateProvider,
    UserListSettingsRepository settingsRepository)
{
    private const string Scope = "CalculationList";
    private const string VisibleColumnsKind = "VisibleColumns";
    private const string ColumnWidthsKind = "ColumnWidths";

    // ── visible columns ────────────────────────────────────────────────────

    public Task<Dictionary<string, bool>?> LoadColumnsAsync()
        => LoadAsync<Dictionary<string, bool>>(VisibleColumnsKind);

    public Task SaveColumnsAsync(Dictionary<string, bool> columns)
        => SaveAsync(VisibleColumnsKind, columns);

    // ── column widths ──────────────────────────────────────────────────────

    public Task<Dictionary<string, int>?> LoadColumnWidthsAsync()
        => LoadAsync<Dictionary<string, int>>(ColumnWidthsKind);

    public Task SaveColumnWidthsAsync(Dictionary<string, int> widths)
        => SaveAsync(ColumnWidthsKind, widths);

    // ── shared load/save (server-authoritative, cache-backed) ──────────────

    private async Task<T?> LoadAsync<T>(string kind) where T : class
    {
        var cached = await LoadFromCacheAsync<T>(kind);

        try
        {
            var settings = await settingsRepository.GetByScopeAsync(Scope);
            var payload = settings.FirstOrDefault(s => s.Kind == kind)?.Payload;

            if (!string.IsNullOrWhiteSpace(payload))
            {
                var fromServer = JsonSerializer.Deserialize<T>(payload);
                if (fromServer is not null)
                {
                    await SaveToCacheAsync(kind, fromServer);
                    return fromServer;
                }
            }

            // Nothing on the server yet: migrate any existing local cache up once.
            if (cached is not null)
                await settingsRepository.UpsertAsync(Scope, kind, JsonSerializer.Serialize(cached));

            return cached;
        }
        catch (Exception ex)
        {
            await clientLogger.ErrorAsync($"Loading {kind} from server failed ({Scope})", ex: ex);
            return cached;
        }
    }

    private async Task SaveAsync<T>(string kind, T value)
    {
        // Write-through: update the cache immediately, then persist to the server.
        await SaveToCacheAsync(kind, value);

        try
        {
            await settingsRepository.UpsertAsync(Scope, kind, JsonSerializer.Serialize(value));
        }
        catch (Exception ex)
        {
            await clientLogger.ErrorAsync($"Saving {kind} to server failed ({Scope})", ex: ex);
        }
    }

    // ── cache helpers ──────────────────────────────────────────────────────

    private async Task<T?> LoadFromCacheAsync<T>(string kind) where T : class
    {
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", await GetCacheKeyAsync(kind));
            if (!string.IsNullOrWhiteSpace(json))
                return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            await clientLogger.ErrorAsync($"Loading cached {kind} failed ({Scope})", ex: ex);
        }
        return null;
    }

    private async Task SaveToCacheAsync<T>(string kind, T value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await js.InvokeVoidAsync("localStorage.setItem", await GetCacheKeyAsync(kind), json);
        }
        catch (Exception ex)
        {
            await clientLogger.ErrorAsync($"Caching {kind} failed ({Scope})", ex: ex);
        }
    }

    private async Task<string> GetCacheKeyAsync(string kind)
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        var userId = user.FindFirst(PMClaimsConst.UserId)?.Value
                     ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? "anonymous";
        return $"{Scope}.{kind}.{userId}";
    }
}
