using BlazorMHD.UI.Core.Data;
using Xunit;

namespace ProjectManagement.Tests;

/// <summary>
/// Unit tests for the library's <see cref="MhdColumnViewManager"/>. These pin the
/// column-view behavior that the project/calculation lists rely on (Standard vs
/// saved views, active/modified detection, save validation, delete), so the
/// extracted logic stays a faithful drop-in for the hand-rolled code-behind.
/// </summary>
public class MhdColumnViewManagerTests
{
    private static readonly Dictionary<string, bool> Defaults = new()
    {
        ["row"] = true,   // always-visible helper column
        ["code"] = true,
        ["name"] = true,
        ["extra"] = false,
    };

    private static readonly MhdColumnViewLabels Labels = new()
    {
        StandardViewName = "Standard",
        CustomViewLabel = "Custom view",
        ModifiedLabelFormat = "{0} · modified",
        AlreadySavedReasonFormat = "Already saved: {0}",
        NameRequiredError = "Name required.",
        StandardNameReservedError = "Standard is reserved.",
        NameAlreadyExistsError = "Name exists.",
        DuplicateContentError = "Already saved.",
    };

    private sealed class FakeStore : IMhdColumnViewStore
    {
        public Dictionary<string, bool>? VisibleColumns { get; set; }
        public List<MhdSavedColumnView> Views { get; set; } = new();
        public int SaveVisibleCalls { get; private set; }
        public int SaveViewsCalls { get; private set; }

        public Task<Dictionary<string, bool>?> LoadVisibleColumnsAsync() => Task.FromResult(VisibleColumns);

        public Task SaveVisibleColumnsAsync(Dictionary<string, bool> columns)
        {
            SaveVisibleCalls++;
            VisibleColumns = new Dictionary<string, bool>(columns);
            return Task.CompletedTask;
        }

        public Task<List<MhdSavedColumnView>> LoadSavedViewsAsync()
            => Task.FromResult(Views.Select(Clone).ToList());

        public Task SaveSavedViewsAsync(List<MhdSavedColumnView> views)
        {
            SaveViewsCalls++;
            Views = views.Select(Clone).ToList();
            return Task.CompletedTask;
        }

        public List<string>? ColumnOrder { get; set; }
        public int SaveOrderCalls { get; private set; }

        public Task<List<string>?> LoadColumnOrderAsync() => Task.FromResult(ColumnOrder);

        public Task SaveColumnOrderAsync(IReadOnlyList<string> order)
        {
            SaveOrderCalls++;
            ColumnOrder = order.ToList();
            return Task.CompletedTask;
        }

        private static MhdSavedColumnView Clone(MhdSavedColumnView v) => new()
        {
            Name = v.Name,
            CreatedAt = v.CreatedAt,
            Columns = new Dictionary<string, bool>(v.Columns),
            Order = new List<string>(v.Order),
        };
    }

    private static MhdColumnViewManager Create(FakeStore store) =>
        new(store, Defaults, Labels, alwaysVisibleKeys: new[] { "row" });

    private static MhdColumnViewManager CreateWithLock(FakeStore store) =>
        new(store, Defaults, Labels,
            alwaysVisibleKeys: new[] { "row" },
            lockedOrderKeys: new[] { "row", "code" });

    [Fact]
    public void Fresh_Manager_Starts_On_Standard()
    {
        var mgr = Create(new FakeStore());

        Assert.True(mgr.IsStandardViewActive);
        Assert.Null(mgr.SelectedViewName);
        Assert.Equal("Standard", mgr.ActiveViewLabel);
        Assert.False(mgr.CanSaveCurrentView);
        Assert.Equal("Already saved: Standard", mgr.SaveDisabledReason);
    }

    [Fact]
    public async Task InitializeAsync_Applies_Known_Keys_Forces_AlwaysVisible_Ignores_Unknown()
    {
        var store = new FakeStore
        {
            VisibleColumns = new Dictionary<string, bool>
            {
                ["row"] = false,        // always-visible → forced back on
                ["extra"] = true,       // known → applied
                ["ghost"] = true,       // unknown → ignored
            },
        };
        var mgr = Create(store);

        await mgr.InitializeAsync();

        Assert.True(mgr.IsColumnVisible("row"));
        Assert.True(mgr.IsColumnVisible("extra"));
        Assert.False(mgr.Columns.ContainsKey("ghost"));
    }

    [Fact]
    public async Task SetColumnVisible_Persists_And_Keeps_SelectedView()
    {
        var store = new FakeStore();
        var mgr = Create(store);
        await mgr.ApplyStandardViewAsync(); // SelectedViewName = "Standard"

        await mgr.SetColumnVisibleAsync("extra", true);

        Assert.True(mgr.IsColumnVisible("extra"));
        Assert.False(mgr.IsStandardViewActive);
        Assert.Equal("Standard", mgr.SelectedViewName);   // unchanged by a manual toggle
        Assert.True(mgr.IsSelectedViewModified);
        Assert.Equal("Standard · modified", mgr.ActiveViewLabel);
        Assert.True(store.SaveVisibleCalls > 0);
    }

    [Fact]
    public async Task SetColumnVisible_Unknown_Key_Is_NoOp()
    {
        var store = new FakeStore();
        var mgr = Create(store);

        await mgr.SetColumnVisibleAsync("ghost", true);

        Assert.False(mgr.Columns.ContainsKey("ghost"));
        Assert.Equal(0, store.SaveVisibleCalls);
    }

    [Fact]
    public async Task Save_Apply_RoundTrip_Marks_View_Active()
    {
        var store = new FakeStore();
        var mgr = Create(store);

        await mgr.SetColumnVisibleAsync("extra", true);
        var error = await mgr.SaveCurrentViewAsync("My view");

        Assert.Null(error);
        Assert.Equal("My view", mgr.SelectedViewName);
        Assert.Single(mgr.SavedViews);
        Assert.False(mgr.CanSaveCurrentView);                 // now matches a saved view
        var saved = mgr.SavedViews[0];
        Assert.True(mgr.IsViewActive(saved));
        Assert.Same(saved, mgr.MatchingSavedView);
        Assert.True(store.SaveViewsCalls > 0);
    }

    [Fact]
    public async Task ApplySavedView_Then_Edit_Reports_Modified()
    {
        var store = new FakeStore();
        var mgr = Create(store);
        await mgr.SetColumnVisibleAsync("extra", true);
        await mgr.SaveCurrentViewAsync("My view");

        await mgr.ApplyStandardViewAsync();
        Assert.False(mgr.IsSelectedViewModified);

        var view = mgr.SavedViews[0];
        await mgr.ApplySavedViewAsync(view);
        Assert.True(mgr.IsViewActive(view));
        Assert.Equal("My view", mgr.ActiveViewLabel);

        await mgr.SetColumnVisibleAsync("extra", false);
        Assert.True(mgr.IsSelectedViewModified);
        Assert.Equal("My view · modified", mgr.ActiveViewLabel);
    }

    [Theory]
    [InlineData("", "Name required.")]
    [InlineData("   ", "Name required.")]
    [InlineData("standard", "Standard is reserved.")]   // case-insensitive reserved name
    [InlineData("STANDARD", "Standard is reserved.")]
    public async Task SaveCurrentView_Rejects_Invalid_Names(string name, string expected)
    {
        var mgr = Create(new FakeStore());
        await mgr.SetColumnVisibleAsync("extra", true);

        Assert.Equal(expected, await mgr.SaveCurrentViewAsync(name));
        Assert.Empty(mgr.SavedViews);
    }

    [Fact]
    public async Task SaveCurrentView_Rejects_Duplicate_Name()
    {
        var mgr = Create(new FakeStore());
        await mgr.SetColumnVisibleAsync("extra", true);
        await mgr.SaveCurrentViewAsync("View A");

        // Different visible columns but a colliding (case-insensitive) name.
        await mgr.SetColumnVisibleAsync("name", false);
        Assert.Equal("Name exists.", await mgr.SaveCurrentViewAsync("view a"));
    }

    [Fact]
    public async Task SaveCurrentView_Rejects_Duplicate_Content()
    {
        var mgr = Create(new FakeStore());
        await mgr.SetColumnVisibleAsync("extra", true);
        await mgr.SaveCurrentViewAsync("View A");

        // Same visible columns, different name → duplicate content.
        Assert.Equal("Already saved.", await mgr.SaveCurrentViewAsync("View B"));
        Assert.Single(mgr.SavedViews);
    }

    [Fact]
    public async Task SavedViews_Are_Sorted_By_Name()
    {
        var mgr = Create(new FakeStore());

        await mgr.SetColumnVisibleAsync("extra", true);
        await mgr.SaveCurrentViewAsync("Zebra");
        await mgr.SetColumnVisibleAsync("name", false);
        await mgr.SaveCurrentViewAsync("Alpha");

        Assert.Equal(new[] { "Alpha", "Zebra" }, mgr.SavedViews.Select(v => v.Name).ToArray());
    }

    [Fact]
    public async Task DeleteView_Removes_And_Clears_Selection()
    {
        var store = new FakeStore();
        var mgr = Create(store);
        await mgr.SetColumnVisibleAsync("extra", true);
        await mgr.SaveCurrentViewAsync("My view");
        var view = mgr.SavedViews[0];

        await mgr.DeleteViewAsync(view);

        Assert.Empty(mgr.SavedViews);
        Assert.Null(mgr.SelectedViewName);
        Assert.Empty(store.Views);
    }

    [Fact]
    public void Signature_Is_Order_Independent_And_Ignores_Hidden()
    {
        var a = new Dictionary<string, bool> { ["code"] = true, ["name"] = true, ["x"] = false };
        var b = new Dictionary<string, bool> { ["name"] = true, ["code"] = true };

        Assert.Equal(MhdSavedColumnView.BuildSignature(a), MhdSavedColumnView.BuildSignature(b));
    }

    // ── column order ─────────────────────────────────────────────────────────

    [Fact]
    public void Fresh_Manager_Starts_In_Canonical_Order()
    {
        var mgr = CreateWithLock(new FakeStore());

        Assert.Equal(new[] { "row", "code", "name", "extra" }, mgr.ColumnOrder.ToArray());
        Assert.True(mgr.IsColumnLocked("row"));
        Assert.True(mgr.IsColumnLocked("code"));
        Assert.False(mgr.IsColumnLocked("name"));
        Assert.True(mgr.IsStandardViewActive);
    }

    [Fact]
    public async Task MoveColumn_Reorders_Persists_And_Makes_Layout_Custom()
    {
        var store = new FakeStore();
        var mgr = CreateWithLock(store);
        await mgr.ApplyStandardViewAsync();

        var moved = await mgr.MoveColumnAsync("extra", -1); // before "name"

        Assert.True(moved);
        Assert.Equal(new[] { "row", "code", "extra", "name" }, mgr.ColumnOrder.ToArray());
        Assert.False(mgr.IsStandardViewActive);             // order change → not Standard
        Assert.Equal("Standard · modified", mgr.ActiveViewLabel);
        Assert.True(store.SaveOrderCalls > 0);
    }

    [Fact]
    public async Task MoveColumn_Cannot_Displace_Locked_Columns()
    {
        var mgr = CreateWithLock(new FakeStore());

        // "name" is the first movable; it can't move up into the locked region.
        Assert.False(await mgr.MoveColumnAsync("name", -1));
        // Locked columns themselves never move.
        Assert.False(await mgr.MoveColumnAsync("code", 1));
        Assert.Equal(new[] { "row", "code", "name", "extra" }, mgr.ColumnOrder.ToArray());
    }

    [Fact]
    public async Task InitializeAsync_Restores_Persisted_Order_With_Locked_First()
    {
        var store = new FakeStore { ColumnOrder = new List<string> { "extra", "name", "code", "row" } };
        var mgr = CreateWithLock(store);

        await mgr.InitializeAsync();

        // Locked keys forced to the front (canonical), movable keys keep the saved order.
        Assert.Equal(new[] { "row", "code", "extra", "name" }, mgr.ColumnOrder.ToArray());
    }

    [Fact]
    public async Task SaveAndApply_Roundtrips_Order_And_Standard_Resets_It()
    {
        var store = new FakeStore();
        var mgr = CreateWithLock(store);
        await mgr.MoveColumnAsync("extra", -1);             // custom order
        var error = await mgr.SaveCurrentViewAsync("Reordered");

        Assert.Null(error);
        var view = mgr.SavedViews[0];
        Assert.Equal(new[] { "row", "code", "extra", "name" }, view.Order.ToArray());
        Assert.True(mgr.IsViewActive(view));

        await mgr.ApplyStandardViewAsync();                 // resets to canonical order
        Assert.Equal(new[] { "row", "code", "name", "extra" }, mgr.ColumnOrder.ToArray());
        Assert.False(mgr.IsViewActive(view));

        await mgr.ApplySavedViewAsync(view);                // re-applies saved order
        Assert.Equal(new[] { "row", "code", "extra", "name" }, mgr.ColumnOrder.ToArray());
        Assert.True(mgr.IsViewActive(view));
    }

    [Fact]
    public async Task Views_Differing_Only_By_Order_Are_Not_Duplicates()
    {
        var mgr = CreateWithLock(new FakeStore());
        await mgr.SetColumnVisibleAsync("extra", true);
        await mgr.SaveCurrentViewAsync("Natural");

        await mgr.MoveColumnAsync("extra", -1);             // same visibility, new order
        Assert.Null(await mgr.SaveCurrentViewAsync("Reordered"));
        Assert.Equal(2, mgr.SavedViews.Count);
    }
}
