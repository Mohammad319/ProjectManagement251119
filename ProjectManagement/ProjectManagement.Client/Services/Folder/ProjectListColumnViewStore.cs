using BlazorMHD.UI.Core.Data;
using ProjectManagement.Client.Shared.Model.Filter;

namespace ProjectManagement.Client.Services.Folder;

/// <summary>
/// Adapts the project list's existing per-user persistence
/// (<see cref="ProjectListViewPreference"/> for visible columns +
/// <see cref="ListSavedColumnViewStorage"/> for named views) to the library's
/// <see cref="IMhdColumnViewStore"/> so a <see cref="MhdColumnViewManager"/> can
/// drive the column UI. The server/localStorage wiring stays in the app; only the
/// shape is translated here, including mapping between the app's
/// <see cref="SavedColumnView"/> and the library's <see cref="MhdSavedColumnView"/>
/// (their JSON shapes are identical, so persisted data is unaffected).
/// </summary>
public sealed class ProjectListColumnViewStore(
    ProjectListViewPreference viewPreference,
    ListSavedColumnViewStorage savedViewStorage) : IMhdColumnViewStore
{
    private const string Scope = ListSavedColumnViewStorage.ProjectListScope;

    public Task<Dictionary<string, bool>?> LoadVisibleColumnsAsync()
        => viewPreference.LoadColumnsAsync();

    public Task SaveVisibleColumnsAsync(Dictionary<string, bool> columns)
        => viewPreference.SaveColumnsAsync(columns);

    public async Task<List<MhdSavedColumnView>> LoadSavedViewsAsync()
    {
        var views = await savedViewStorage.LoadAsync(Scope);
        return views.Select(ToLibrary).ToList();
    }

    public Task SaveSavedViewsAsync(List<MhdSavedColumnView> views)
        => savedViewStorage.SaveAllAsync(Scope, views.Select(ToApp).ToList());

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
