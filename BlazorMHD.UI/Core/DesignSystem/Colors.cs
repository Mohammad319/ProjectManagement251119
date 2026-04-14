namespace BlazorMHD.UI.Core.DesignSystem;

public static class Colors
{
    public static string GetFilledBg(MhdState state) => $"mhd-btn-filled-{StateName(state)}";
    public static string GetFilledText(MhdState state) => string.Empty;
    public static string GetOutline(MhdState state) => $"mhd-btn-outline-{StateName(state)}";
    public static string GetGhost(MhdState state) => $"mhd-tone-ghost-{StateName(state)}";
    public static string GetText(MhdState state) => $"mhd-btn-text-{StateName(state)}";

    private static string StateName(MhdState state) => state switch
    {
        MhdState.Primary => "primary",
        MhdState.Secondary => "secondary",
        MhdState.Success => "success",
        MhdState.Danger => "danger",
        MhdState.Warning => "warning",
        MhdState.Info => "info",
        _ => "neutral"
    };
}
