using System.Text.Json;
using Microsoft.JSInterop;
using ProjectManagement.Client.Helper;

namespace ProjectManagement.Client.Services.Folder;

public sealed class ProjectListViewPreference(IJSRuntime js, IClientLogger clientLogger)
{
    private const string ColumnsKey = "ProjectList.VisibleColumns";

    public async Task<Dictionary<string, bool>?> LoadColumnsAsync()
    {
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", ColumnsKey);
            if (!string.IsNullOrWhiteSpace(json))
                return JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
        }
        catch (Exception ex)
        {
            await clientLogger.ErrorAsync("Loading project list column preferences failed", ex: ex);
        }
        return null;
    }

    public async Task SaveColumnsAsync(Dictionary<string, bool> columns)
    {
        try
        {
            var json = JsonSerializer.Serialize(columns);
            await js.InvokeVoidAsync("localStorage.setItem", ColumnsKey, json);
        }
        catch (Exception ex)
        {
            await clientLogger.ErrorAsync("Saving project list column preferences failed", ex: ex);
        }
    }
}
