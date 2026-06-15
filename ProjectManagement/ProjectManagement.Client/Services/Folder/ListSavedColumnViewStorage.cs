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
/// Persists named column views ("kolumnvyer") per user. The server (per
/// tenant/user) is the source of truth; localStorage is kept only as a fast
/// cache and offline fallback. Project list and calculation list use separate
/// scopes and never mix with saved filters.
/// </summary>
public sealed class ListSavedColumnViewStorage(
    IJSRuntime js,
    IClientLogger clientLogger,
    AuthenticationStateProvider authenticationStateProvider,
    UserListSettingsRepository settingsRepository)
{
    public const string ProjectListScope = "ProjectList";
    public const string CalculationListScope = "CalculationList";
    private const string Kind = "SavedColumnViews";

    public async Task<List<SavedColumnView>> LoadAsync(string scope)
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
            await clientLogger.ErrorAsync($"Loading saved column views from server failed ({scope})", ex: ex);
            return cached;
        }
    }

    public async Task SaveAllAsync(string scope, List<SavedColumnView> views)
    {
        // Write-through: update the cache immediately, then persist to the server.
        await SaveToCacheAsync(scope, views);

        try
        {
            await settingsRepository.UpsertAsync(scope, Kind, JsonSerializer.Serialize(views));
        }
        catch (Exception ex)
        {
            await clientLogger.ErrorAsync($"Saving saved column views to server failed ({scope})", ex: ex);
        }
    }

    // ── cache helpers ──────────────────────────────────────────────────────

    private async Task<List<SavedColumnView>> LoadFromCacheAsync(string scope)
    {
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", await GetStorageKeyAsync(scope));
            if (!string.IsNullOrWhiteSpace(json))
                return Normalize(Deserialize(json));
        }
        catch (Exception ex)
        {
            await clientLogger.ErrorAsync($"Loading cached column views failed ({scope})", ex: ex);
        }
        return [];
    }

    private async Task SaveToCacheAsync(string scope, List<SavedColumnView> views)
    {
        try
        {
            var json = JsonSerializer.Serialize(views);
            await js.InvokeVoidAsync("localStorage.setItem", await GetStorageKeyAsync(scope), json);
        }
        catch (Exception ex)
        {
            await clientLogger.ErrorAsync($"Caching column views failed ({scope})", ex: ex);
        }
    }

    private static List<SavedColumnView> Deserialize(string json)
        => JsonSerializer.Deserialize<List<SavedColumnView>>(json) ?? [];

    private static List<SavedColumnView> Normalize(List<SavedColumnView> views)
        => views
            .Where(v => !string.IsNullOrWhiteSpace(v.Name))
            .OrderBy(v => v.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private async Task<string> GetStorageKeyAsync(string scope)
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        var userId = user.FindFirst(PMClaimsConst.UserId)?.Value
                     ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? "anonymous";
        return $"{scope}.SavedColumnViews.{userId}";
    }
}
