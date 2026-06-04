using System.Text.Json;
using Microsoft.JSInterop;
using ProjectManagement.Client.Helper;

namespace ProjectManagement.Client.Services.Calculation;

public sealed class CalculationListViewPreference(IJSRuntime js, IClientLogger clientLogger)
{
    private const string ColumnsKey = "CalculationList.VisibleColumns";

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
            await clientLogger.ErrorAsync("Loading calculation list column preferences failed", ex: ex);
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
            await clientLogger.ErrorAsync("Saving calculation list column preferences failed", ex: ex);
        }
    }
}
