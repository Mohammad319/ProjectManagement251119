namespace BlazorMHD.UI.Core.DesignSystem;

public static class Sizes
{
    // Padding — مقاربة Tailwind UI (Buttons)
    public static string GetPadding(MhdSize size) => size switch
    {
        MhdSize.Xs => "px-2 py-1",
        MhdSize.Sm => "px-3 py-1.5",
        MhdSize.Md => "px-4 py-2",
        MhdSize.LG => "px-5 py-2.5",
        MhdSize.XL => "px-6 py-3",
        _ => "px-4 py-2"
    };

    // Text sizes — مبنية مباشرة على Tailwind
    public static string GetTextSize(MhdSize size) => size switch
    {
        MhdSize.Xs => "text-xs",
        MhdSize.Sm => "text-sm",
        MhdSize.Md => "text-sm",
        MhdSize.LG => "text-base",
        MhdSize.XL => "text-lg",
        _ => "text-sm"
    };
}
