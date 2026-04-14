using BlazorMHD.UI.Core.Services;

namespace BlazorMHD.UI.Core.DesignSystem;

public static class DialogStyles
{
    public static string GetHeaderBackground(MhdState state) => state switch
    {
        MhdState.Primary => "mhd-dialog-header-tone-primary",
        MhdState.Success => "mhd-dialog-header-tone-success",
        MhdState.Danger => "mhd-dialog-header-tone-danger",
        MhdState.Warning => "mhd-dialog-header-tone-warning",
        MhdState.Info => "mhd-dialog-header-tone-info",
        MhdState.Secondary => "mhd-dialog-header-tone-secondary",
        MhdState.Neutral => "mhd-dialog-header-tone-neutral",
        _ => "mhd-dialog-header-tone-neutral"
    };

    public static string GetHeaderAccent(MhdState state) => state switch
    {
        MhdState.Primary => "mhd-dialog-accent-primary",
        MhdState.Success => "mhd-dialog-accent-success",
        MhdState.Danger => "mhd-dialog-accent-danger",
        MhdState.Warning => "mhd-dialog-accent-warning",
        MhdState.Info => "mhd-dialog-accent-info",
        MhdState.Secondary => "mhd-dialog-accent-secondary",
        MhdState.Neutral => "mhd-dialog-accent-neutral",
        _ => "mhd-dialog-accent-neutral"
    };

    public static string GetPanelClasses(DialogModel model)
        => model.Size == DialogSize.FullScreen
            ? "mhd-dialog-panel-full"
            : "mhd-dialog-panel-default";
}
