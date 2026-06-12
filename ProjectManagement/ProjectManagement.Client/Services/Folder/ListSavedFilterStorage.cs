using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.Model.Filter;
using ProjectManagement.Shared.Constant;

namespace ProjectManagement.Client.Services.Folder;

/// <summary>
/// Persists named list filters for the right panel. Filters are personal:
/// the storage key includes the current user id, so one user never sees
/// another user's saved filters. Project list and calculation list use
/// separate scopes and are never mixed.
/// </summary>
public sealed class ListSavedFilterStorage(
    IJSRuntime js,
    IClientLogger clientLogger,
    AuthenticationStateProvider authenticationStateProvider)
{
    public const string ProjectListScope = "ProjectList";
    public const string CalculationListScope = "CalculationList";

    public async Task<List<SavedListFilter>> LoadAsync(string scope)
    {
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", await GetStorageKeyAsync(scope));
            if (!string.IsNullOrWhiteSpace(json))
            {
                var filters = JsonSerializer.Deserialize<List<SavedListFilter>>(json) ?? [];
                return filters
                    .Where(f => !string.IsNullOrWhiteSpace(f.Name))
                    .OrderBy(f => f.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            await clientLogger.ErrorAsync($"Loading saved list filters failed ({scope})", ex: ex);
        }
        return [];
    }

    public async Task SaveAllAsync(string scope, List<SavedListFilter> filters)
    {
        try
        {
            var json = JsonSerializer.Serialize(filters);
            await js.InvokeVoidAsync("localStorage.setItem", await GetStorageKeyAsync(scope), json);
        }
        catch (Exception ex)
        {
            await clientLogger.ErrorAsync($"Saving saved list filters failed ({scope})", ex: ex);
        }
    }

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
