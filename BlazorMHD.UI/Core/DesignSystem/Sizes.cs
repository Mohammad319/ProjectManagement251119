namespace BlazorMHD.UI.Core.DesignSystem;

public static class Sizes
{
    public static string GetPadding(MhdSize size) => size switch
    {
        MhdSize.Xs => "mhd-size-pad-xs",
        MhdSize.Sm => "mhd-size-pad-sm",
        MhdSize.Md => "mhd-size-pad-md",
        MhdSize.LG => "mhd-size-pad-lg",
        MhdSize.XL => "mhd-size-pad-xl",
        _ => "mhd-size-pad-md"
    };

    public static string GetTextSize(MhdSize size) => size switch
    {
        MhdSize.Xs => "mhd-size-text-xs",
        MhdSize.Sm => "mhd-size-text-sm",
        MhdSize.Md => "mhd-size-text-md",
        MhdSize.LG => "mhd-size-text-lg",
        MhdSize.XL => "mhd-size-text-xl",
        _ => "mhd-size-text-md"
    };
}
