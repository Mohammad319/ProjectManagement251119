using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.Model.Filter;
using ProjectManagement.Shared.Constant;

namespace ProjectManagement.Client.Services.Folder;

/// <summary>
/// Persists named column views ("kolumnvyer") for the right-panel lists.
/// Views are personal (the storage key includes the current user id) and
/// kept completely separate from saved filters and from the other list:
/// project list and calculation list use different scopes and never mix.
/// </summary>
public sealed class ListSavedColumnViewStorage(
    IJSRuntime js,
    IClientLogger clientLogger,
    AuthenticationStateProvider authenticationStateProvider)
{
    public const string ProjectListScope = "ProjectList";
    public const string CalculationListScope = "CalculationList";

    public async Task<List<SavedColumnView>> LoadAsync(string scope)
    {
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", await GetStorageKeyAsync(scope));
            if (!string.IsNullOrWhiteSpace(json))
            {
                var views = JsonSerializer.Deserialize<List<SavedColumnView>>(json) ?? [];
                return views
                    .Where(v => !string.IsNullOrWhiteSpace(v.Name))
                    .OrderBy(v => v.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            await clientLogger.ErrorAsync($"Loading saved column views failed ({scope})", ex: ex);
        }
        return [];
    }

    public async Task SaveAllAsync(string scope, List<SavedColumnView> views)
    {
        try
        {
            var json = JsonSerializer.Serialize(views);
            await js.InvokeVoidAsync("localStorage.setItem", await GetStorageKeyAsync(scope), json);
        }
        catch (Exception ex)
        {
            await clientLogger.ErrorAsync($"Saving saved column views failed ({scope})", ex: ex);
        }
    }

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
