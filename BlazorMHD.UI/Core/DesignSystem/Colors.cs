namespace BlazorMHD.UI.Core.DesignSystem;

public static class Colors
{
    // خلفية الأزرار الممتلئة (Filled)
    public static string GetFilledBg(MhdState state) => state switch
    {
        MhdState.Primary => "bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-400",
        MhdState.Secondary => "bg-violet-600 hover:bg-violet-700 dark:bg-violet-500 dark:hover:bg-violet-400",
        MhdState.Success => "bg-emerald-600 hover:bg-emerald-700 dark:bg-emerald-500 dark:hover:bg-emerald-400",
        MhdState.Danger => "bg-red-600 hover:bg-red-700 dark:bg-red-500 dark:hover:bg-red-400",
        MhdState.Warning => "bg-amber-500 hover:bg-amber-600 dark:bg-amber-400 dark:hover:bg-amber-300",
        MhdState.Info => "bg-sky-500 hover:bg-sky-600 dark:bg-sky-500 dark:hover:bg-sky-400",
        MhdState.Neutral => "bg-slate-600 hover:bg-slate-700 dark:bg-slate-500 dark:hover:bg-slate-400",
        _ => "bg-slate-600 hover:bg-slate-700 dark:bg-slate-500 dark:hover:bg-slate-400"
    };

    // لون النص في الأزرار الممتلئة
    public static string GetFilledText(MhdState state) => state switch
    {
        // الخلفية فاتحة → نستخدم نص داكن
        MhdState.Warning => "text-slate-900",
        _ => "text-white"
    };

    // Outline Buttons
    public static string GetOutline(MhdState state) => state switch
    {
        MhdState.Primary =>
            "border border-blue-600 text-blue-700 hover:bg-blue-50 " +
            "dark:border-blue-400 dark:text-blue-200 " +
            "dark:hover:bg-blue-900/60 dark:hover:border-blue-300",

        MhdState.Secondary =>
            "border border-violet-600 text-violet-700 hover:bg-violet-50 " +
            "dark:border-violet-400 dark:text-violet-300 dark:hover:bg-violet-950/30",

        MhdState.Success =>
            "border border-emerald-600 text-emerald-700 hover:bg-emerald-50 " +
            "dark:border-emerald-400 dark:text-emerald-300 dark:hover:bg-emerald-950/30",

        MhdState.Danger =>
            "border border-red-600 text-red-700 hover:bg-red-50 " +
            "dark:border-red-400 dark:text-red-300 dark:hover:bg-red-950/30",

        MhdState.Warning =>
            "border border-amber-600 text-amber-700 hover:bg-amber-50 " +
            "dark:border-amber-400 dark:text-amber-300 dark:hover:bg-amber-950/30",

        MhdState.Info =>
            "border border-sky-600 text-sky-700 hover:bg-sky-50 " +
            "dark:border-sky-400 dark:text-sky-300 dark:hover:bg-sky-950/30",

        MhdState.Neutral =>
            "border border-slate-400 text-slate-700 hover:bg-slate-50 " +
            "dark:border-slate-500 dark:text-slate-300 dark:hover:bg-slate-800/40",

        _ =>
            "border border-slate-400 text-slate-700 hover:bg-slate-50 " +
            "dark:border-slate-500 dark:text-slate-300 dark:hover:bg-slate-800/40"
    };

    // أزرار Ghost (نص ملون وخلفية شفافة مع hover خفيف)
    public static string GetGhost(MhdState state) => state switch
    {
        MhdState.Primary =>
            "text-blue-600 hover:bg-blue-50 " +
            "dark:text-blue-300 dark:hover:bg-blue-950/30",

        MhdState.Secondary =>
            "text-violet-600 hover:bg-violet-50 " +
            "dark:text-violet-300 dark:hover:bg-violet-950/30",

        MhdState.Success =>
            "text-emerald-600 hover:bg-emerald-50 " +
            "dark:text-emerald-300 dark:hover:bg-emerald-950/30",

        MhdState.Danger =>
            "text-red-600 hover:bg-red-50 " +
            "dark:text-red-300 dark:hover:bg-red-950/30",

        MhdState.Warning =>
            "text-amber-700 hover:bg-amber-50 " +
            "dark:text-amber-300 dark:hover:bg-amber-950/30",

        MhdState.Info =>
            "text-sky-600 hover:bg-sky-50 " +
            "dark:text-sky-300 dark:hover:bg-sky-950/30",

        MhdState.Neutral =>
            "text-slate-700 hover:bg-slate-50 " +
            "dark:text-slate-300 dark:hover:bg-slate-800/40",

        _ =>
            "text-slate-700 hover:bg-slate-50 " +
            "dark:text-slate-300 dark:hover:bg-slate-800/40"
    };

    // نص فقط (Text button)
    public static string GetText(MhdState state) => state switch
    {
        MhdState.Primary =>
            "text-blue-600 hover:text-blue-700 " +
            "dark:text-blue-300 dark:hover:text-blue-200",

        MhdState.Secondary =>
            "text-violet-600 hover:text-violet-700 " +
            "dark:text-violet-300 dark:hover:text-violet-200",

        MhdState.Success =>
            "text-emerald-600 hover:text-emerald-700 " +
            "dark:text-emerald-300 dark:hover:text-emerald-200",

        MhdState.Danger =>
            "text-red-600 hover:text-red-700 " +
            "dark:text-red-300 dark:hover:text-red-200",

        MhdState.Warning =>
            "text-amber-700 hover:text-amber-800 " +
            "dark:text-amber-300 dark:hover:text-amber-200",

        MhdState.Info =>
            "text-sky-600 hover:text-sky-700 " +
            "dark:text-sky-300 dark:hover:text-sky-200",

        MhdState.Neutral =>
            "text-slate-700 hover:text-slate-800 " +
            "dark:text-slate-300 dark:hover:text-slate-100",

        _ =>
            "text-slate-700 hover:text-slate-800 " +
            "dark:text-slate-300 dark:hover:text-slate-100"
    };
}
