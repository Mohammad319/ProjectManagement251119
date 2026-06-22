using BlazorMHD.UI.Core.Data;
using ProjectManagement.Client.Shared.Model.Filter;

namespace ProjectManagement.Client.Services.Folder;

/// <summary>
/// Adapts a right-panel list's existing per-user persistence to the library's
/// <see cref="IMhdColumnViewStore"/> so a <see cref="MhdColumnViewManager"/> can
/// drive the column UI. Visible-column load/save are supplied as delegates (each
/// list has its own typed <c>…ViewPreference</c> service), while named views go
/// through <see cref="ListSavedColumnViewStorage"/> under the given scope.
///
/// The server/localStorage wiring stays in the app; only the shape is translated
/// here, including mapping between the app's <see cref="SavedColumnView"/> and the
/// library's <see cref="MhdSavedColumnView"/> (their JSON shapes are identical, so
/// persisted data is unaffected).
/// </summary>
public sealed class ListColumnViewStore(
    string scope,
    Func<Task<Dictionary<string, bool>?>> loadVisibleColumns,
    Func<Dictionary<string, bool>, Task> saveVisibleColumns,
    ListSavedColumnViewStorage savedViewStorage) : IMhdColumnViewStore
{
    public Task<Dictionary<string, bool>?> LoadVisibleColumnsAsync()
        => loadVisibleColumns();

    public Task SaveVisibleColumnsAsync(Dictionary<string, bool> columns)
        => saveVisibleColumns(columns);

    public async Task<List<MhdSavedColumnView>> LoadSavedViewsAsync()
    {
        var views = await savedViewStorage.LoadAsync(scope);
        return views.Select(ToLibrary).ToList();
    }

    public Task SaveSavedViewsAsync(List<MhdSavedColumnView> views)
        => savedViewStorage.SaveAllAsync(scope, views.Select(ToApp).ToList());

    private static MhdSavedColumnView ToLibrary(SavedColumnView v) => new()
    {
        Name = v.Name,
        CreatedAt = v.CreatedAt,
        Columns = new Dictionary<string, bool>(v.Columns),
    };

    private static SavedColumnView ToApp(MhdSavedColumnView v) => new()
    {
        Name = v.Name,
        CreatedAt = v.CreatedAt,
        Columns = new Dictionary<string, bool>(v.Columns),
    };
}
