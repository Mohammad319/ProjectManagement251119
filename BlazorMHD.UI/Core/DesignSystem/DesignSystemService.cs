namespace BlazorMHD.UI.Core.DesignSystem;

public interface IDesignSystemService
{
    string GetButtonClass(MhdState state, MhdVariant variant, MhdSize size, bool fullWidth = false, bool disabled = false);
    string GetTagClass(MhdState state, MhdSize size);
    string GetCardClass(bool elevated = false);
    string GetSurfaceClass();
}

public class DesignSystemService : IDesignSystemService
{
    public string GetButtonClass(MhdState state, MhdVariant variant, MhdSize size, bool fullWidth = false, bool disabled = false)
    {
        var classes = new List<string>
        {
            "inline-flex items-center justify-center gap-2 rounded-md border font-semibold shadow-sm transition-colors duration-150 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500",
            GetButtonSize(size),
            GetButtonVariant(state, variant)
        };

        if (fullWidth)
            classes.Add("w-full");

        if (disabled)
            classes.Add("cursor-not-allowed opacity-60");
        else
            classes.Add("cursor-pointer");

        return string.Join(" ", classes);
    }

    public string GetTagClass(MhdState state, MhdSize size)
    {
        return string.Join(" ", new[]
        {
            "mhd-tag-base",
            Sizes.GetPadding(size),
            Sizes.GetTextSize(size),
            Colors.GetGhost(state)
        });
    }

    public string GetCardClass(bool elevated = false)
    {
        return string.Join(" ", new[]
        {
            "mhd-card-base",
            RadiusCss.LG,
            elevated ? Shadows.Subtle : Shadows.None
        });
    }

    public string GetSurfaceClass()
    {
        return "mhd-surface-base";
    }

    private static string GetButtonSize(MhdSize size) => size switch
    {
        MhdSize.Xs => "px-2 py-1 text-[0.70rem]",
        MhdSize.Sm => "px-2.5 py-1.5 text-[0.75rem]",
        MhdSize.Md => "px-3.5 py-2 text-sm",
        MhdSize.LG => "px-4 py-2.5 text-sm",
        MhdSize.XL => "px-5 py-3 text-base",
        _ => "px-3.5 py-2 text-sm"
    };

    private static string GetButtonVariant(MhdState state, MhdVariant variant) => variant switch
    {
        MhdVariant.Filled => GetFilledButtonVariant(state),
        MhdVariant.Outline => GetOutlineButtonVariant(state),
        MhdVariant.Ghost => GetGhostButtonVariant(state),
        MhdVariant.Text => GetTextButtonVariant(state),
        _ => GetFilledButtonVariant(state)
    };

    private static string GetFilledButtonVariant(MhdState state) => state switch
    {
        MhdState.Primary => "border-blue-600 bg-blue-600 text-white hover:border-blue-700 hover:bg-blue-700 dark:border-blue-500 dark:bg-blue-600 dark:hover:border-blue-400 dark:hover:bg-blue-500",
        MhdState.Secondary => "border-slate-700 bg-slate-700 text-white hover:border-slate-800 hover:bg-slate-800 dark:border-slate-500 dark:bg-slate-600 dark:hover:border-slate-400 dark:hover:bg-slate-500",
        MhdState.Success => "border-emerald-600 bg-emerald-600 text-white hover:border-emerald-700 hover:bg-emerald-700 dark:border-emerald-500 dark:bg-emerald-600 dark:hover:border-emerald-400 dark:hover:bg-emerald-500",
        MhdState.Danger => "border-rose-600 bg-rose-600 text-white hover:border-rose-700 hover:bg-rose-700 dark:border-rose-500 dark:bg-rose-600 dark:hover:border-rose-400 dark:hover:bg-rose-500",
        MhdState.Warning => "border-amber-400 bg-amber-400 text-amber-950 hover:border-amber-500 hover:bg-amber-500 dark:border-amber-400 dark:bg-amber-400 dark:text-amber-950 dark:hover:border-amber-300 dark:hover:bg-amber-300",
        MhdState.Info => "border-sky-500 bg-sky-500 text-white hover:border-sky-600 hover:bg-sky-600 dark:border-sky-400 dark:bg-sky-500 dark:hover:border-sky-300 dark:hover:bg-sky-400",
        _ => "border-slate-200 bg-slate-100 text-slate-900 hover:border-slate-300 hover:bg-slate-200 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100 dark:hover:border-slate-600 dark:hover:bg-slate-700"
    };

    private static string GetOutlineButtonVariant(MhdState state) => state switch
    {
        MhdState.Primary => "border-blue-200 bg-white text-blue-700 hover:border-blue-300 hover:bg-blue-50 dark:border-blue-900/50 dark:bg-transparent dark:text-blue-300 dark:hover:bg-blue-950/30",
        MhdState.Secondary => "border-slate-300 bg-white text-slate-700 hover:border-slate-400 hover:bg-slate-50 dark:border-slate-700 dark:bg-transparent dark:text-slate-200 dark:hover:bg-slate-800",
        MhdState.Success => "border-emerald-200 bg-white text-emerald-700 hover:border-emerald-300 hover:bg-emerald-50 dark:border-emerald-900/50 dark:bg-transparent dark:text-emerald-300 dark:hover:bg-emerald-950/30",
        MhdState.Danger => "border-rose-200 bg-white text-rose-700 hover:border-rose-300 hover:bg-rose-50 dark:border-rose-900/50 dark:bg-transparent dark:text-rose-300 dark:hover:bg-rose-950/30",
        MhdState.Warning => "border-amber-200 bg-white text-amber-800 hover:border-amber-300 hover:bg-amber-50 dark:border-amber-900/50 dark:bg-transparent dark:text-amber-200 dark:hover:bg-amber-950/30",
        MhdState.Info => "border-sky-200 bg-white text-sky-700 hover:border-sky-300 hover:bg-sky-50 dark:border-sky-900/50 dark:bg-transparent dark:text-sky-300 dark:hover:bg-sky-950/30",
        _ => "border-slate-200 bg-white text-slate-700 hover:border-slate-300 hover:bg-slate-50 dark:border-slate-700 dark:bg-transparent dark:text-slate-200 dark:hover:bg-slate-800"
    };

    private static string GetGhostButtonVariant(MhdState state) => state switch
    {
        MhdState.Primary => "border-transparent bg-blue-50 text-blue-700 hover:bg-blue-100 dark:bg-blue-950/30 dark:text-blue-300 dark:hover:bg-blue-950/50",
        MhdState.Secondary => "border-transparent bg-slate-100 text-slate-700 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-200 dark:hover:bg-slate-700",
        MhdState.Success => "border-transparent bg-emerald-50 text-emerald-700 hover:bg-emerald-100 dark:bg-emerald-950/30 dark:text-emerald-300 dark:hover:bg-emerald-950/50",
        MhdState.Danger => "border-transparent bg-rose-50 text-rose-700 hover:bg-rose-100 dark:bg-rose-950/30 dark:text-rose-300 dark:hover:bg-rose-950/50",
        MhdState.Warning => "border-transparent bg-amber-50 text-amber-800 hover:bg-amber-100 dark:bg-amber-950/30 dark:text-amber-200 dark:hover:bg-amber-950/50",
        MhdState.Info => "border-transparent bg-sky-50 text-sky-700 hover:bg-sky-100 dark:bg-sky-950/30 dark:text-sky-300 dark:hover:bg-sky-950/50",
        _ => "border-transparent bg-slate-100 text-slate-700 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-200 dark:hover:bg-slate-700"
    };

    private static string GetTextButtonVariant(MhdState state) => state switch
    {
        MhdState.Primary => "border-transparent bg-transparent text-blue-700 hover:bg-blue-50 dark:text-blue-300 dark:hover:bg-blue-950/30",
        MhdState.Secondary => "border-transparent bg-transparent text-slate-700 hover:bg-slate-50 dark:text-slate-200 dark:hover:bg-slate-800",
        MhdState.Success => "border-transparent bg-transparent text-emerald-700 hover:bg-emerald-50 dark:text-emerald-300 dark:hover:bg-emerald-950/30",
        MhdState.Danger => "border-transparent bg-transparent text-rose-700 hover:bg-rose-50 dark:text-rose-300 dark:hover:bg-rose-950/30",
        MhdState.Warning => "border-transparent bg-transparent text-amber-800 hover:bg-amber-50 dark:text-amber-200 dark:hover:bg-amber-950/30",
        MhdState.Info => "border-transparent bg-transparent text-sky-700 hover:bg-sky-50 dark:text-sky-300 dark:hover:bg-sky-950/30",
        _ => "border-transparent bg-transparent text-slate-700 hover:bg-slate-50 dark:text-slate-200 dark:hover:bg-slate-800"
    };
}
