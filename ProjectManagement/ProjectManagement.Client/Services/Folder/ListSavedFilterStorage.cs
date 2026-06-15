using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.Model.Filter;
using ProjectManagement.Client.Shared.Repositories.UserSettings;
using ProjectManagement.Shared.Constant;

namespace ProjectManagement.Client.Services.Folder;

/// <summary>
/// Persists named list filters per user. The server (per tenant/user) is the
/// source of truth; localStorage is kept only as a fast cache and offline
/// fallback. Project list and calculation list use separate scopes.
/// </summary>
public sealed class ListSavedFilterStorage(
    IJSRuntime js,
    IClientLogger clientLogger,
    AuthenticationStateProvider authenticationStateProvider,
    UserListSettingsRepository settingsRepository)
{
    public const string ProjectListScope = "ProjectList";
    public const string CalculationListScope = "CalculationList";
    private const string Kind = "SavedFilters";

    public async Task<List<SavedListFilter>> LoadAsync(string scope)
    {
        var cached = await LoadFromCacheAsync(scope);

        try
        {
            var settings = await settingsRepository.GetByScopeAsync(scope);
            var payload = settings.FirstOrDefault(s => s.Kind == Kind)?.Payload;

            if (!string.IsNullOrWhiteSpace(payload))
            {
                var fromServer = Normalize(Deserialize(payload));
                await SaveToCacheAsync(scope, fromServer);
                return fromServer;
            }

            // Nothing on the server yet: migrate any existing local cache up once.
            if (cached.Count > 0)
                await settingsRepository.UpsertAsync(scope, Kind, JsonSerializer.Serialize(cached));

            return cached;
        }
        catch (Exception ex)
        {
            await clientLogger.ErrorAsync($"Loading saved list filters from server failed ({scope})", ex: ex);
            return cached;
        }
    }

    public async Task SaveAllAsync(string scope, List<SavedListFilter> filters)
    {
        // Write-through: update the cache immediately, then persist to the server.
        await SaveToCacheAsync(scope, filters);

        try
        {
            await settingsRepository.UpsertAsync(scope, Kind, JsonSerializer.Serialize(filters));
        }
        catch (Exception ex)
        {
            await clientLogger.ErrorAsync($"Saving saved list filters to server failed ({scope})", ex: ex);
        }
    }

    // ── cache helpers ──────────────────────────────────────────────────────

    private async Task<List<SavedListFilter>> LoadFromCacheAsync(string scope)
    {
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", await GetStorageKeyAsync(scope));
            if (!string.IsNullOrWhiteSpace(json))
                return Normalize(Deserialize(json));
        }
        catch (Exception ex)
        {
            await clientLogger.ErrorAsync($"Loading cached list filters failed ({scope})", ex: ex);
        }
        return [];
    }

    private async Task SaveToCacheAsync(string scope, List<SavedListFilter> filters)
    {
        try
        {
            var json = JsonSerializer.Serialize(filters);
            await js.InvokeVoidAsync("localStorage.setItem", await GetStorageKeyAsync(scope), json);
        }
        catch (Exception ex)
        {
            await clientLogger.ErrorAsync($"Caching list filters failed ({scope})", ex: ex);
        }
    }

    private static List<SavedListFilter> Deserialize(string json)
        => JsonSerializer.Deserialize<List<SavedListFilter>>(json) ?? [];

    private static List<SavedListFilter> Normalize(List<SavedListFilter> filters)
        => filters
            .Where(f => !string.IsNullOrWhiteSpace(f.Name))
            .OrderBy(f => f.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private async Task<string> GetStorageKeyAsync(string scope)
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        var userId = user.FindFirst(PMClaimsConst.UserId)?.Value
                     ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? "anonymous";
        return $"{scope}.SavedFilters.{userId}";
    }
}
