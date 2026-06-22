namespace ProjectManagement.Client.Shared.Components;

/// <summary>
/// Selects which presentation <see cref="HiddenFilterColumnsNotice"/> renders.
/// </summary>
public enum HiddenFilterNoticeMode
{
    /// <summary>Compact box rendered inside the open filter dropdown.</summary>
    Inline,

    /// <summary>Popup shown next to the filter button even when the menu is closed.</summary>
    Floating,
}
