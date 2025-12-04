namespace BlazorMHD.UI.Core.DesignSystem;

public static class ButtonStyles
{
    private const string BaseButton =
        "inline-flex items-center justify-center rounded-md " +
        "font-medium transition-colors duration-150 " +
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 " +
        "focus-visible:ring-sky-500 dark:focus-visible:ring-sky-400 " +
        "focus-visible:ring-offset-white dark:focus-visible:ring-offset-slate-900 " +
        "disabled:opacity-50 disabled:cursor-not-allowed";

    public static string Get(MhdState state, bool isPrimary, MhdSize size = MhdSize.Md)
    {
        // ألوان حسب نوع الزر (Primary / Outline)
        string colorClasses = isPrimary
            ? $"{Colors.GetFilledBg(state)} {Colors.GetFilledText(state)} border border-transparent"
            : Colors.GetOutline(state);

        // حجم الزر
        string sizeClasses =
            $"{Sizes.GetPadding(size)} {Sizes.GetTextSize(size)} {Sizes.GetHeight(size)}";

        return $"{BaseButton} {colorClasses} {sizeClasses}";
    }

}
