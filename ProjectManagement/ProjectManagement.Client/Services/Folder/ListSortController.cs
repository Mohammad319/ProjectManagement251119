namespace ProjectManagement.Client.Services.Folder;

/// <summary>
/// Shared sort state + header rendering for the right-panel list tables
/// (the project list and the calculation list). Owns the active sort column and
/// direction, the click-to-toggle behavior, and the header CSS / sort-indicator
/// SVG that both lists previously rendered with byte-for-byte identical helpers.
///
/// The actual <c>OrderBy</c> switch stays in each component because the sort keys
/// are domain-specific; this controller only holds the state both switches read
/// (<see cref="Column"/> / <see cref="Ascending"/>) and the chrome they share.
/// </summary>
public sealed class ListSortController
{
    /// <summary>The currently sorted column key, or "" for the default order.</summary>
    public string Column { get; set; } = "";

    /// <summary>True for ascending, false for descending.</summary>
    public bool Ascending { get; set; } = true;

    /// <summary>
    /// Header click: toggles direction when the column is already active, otherwise
    /// switches to <paramref name="column"/> ascending. The caller resets paging.
    /// </summary>
    public void Toggle(string column)
    {
        if (Column == column)
            Ascending = !Ascending;
        else
        {
            Column = column;
            Ascending = true;
        }
    }

    /// <summary>CSS classes for a sortable column header (highlighted when active).</summary>
    public string HeaderClass(string column)
    {
        var active = Column == column;
        return "cursor-pointer select-none whitespace-nowrap px-3 py-2 transition " +
               (active ? "text-slate-800 dark:text-slate-200" : "hover:text-slate-700 dark:hover:text-slate-200");
    }

    /// <summary>Inline SVG sort indicator: neutral when inactive, up/down arrow when active.</summary>
    public string IndicatorHtml(string column)
    {
        if (Column != column)
            return NeutralIcon;

        return Ascending ? AscendingIcon : DescendingIcon;
    }

    private const string NeutralIcon =
        "<svg class=\"h-3 w-3 opacity-30\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\"><path stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"M7 16V4m0 0L3 8m4-4l4 4M17 8v12m0 0l4-4m-4 4l-4-4\"/></svg>";

    private const string AscendingIcon =
        "<svg class=\"h-3 w-3\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\"><path stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"M5 15l7-7 7 7\"/></svg>";

    private const string DescendingIcon =
        "<svg class=\"h-3 w-3\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\"><path stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"M19 9l-7 7-7-7\"/></svg>";
}
