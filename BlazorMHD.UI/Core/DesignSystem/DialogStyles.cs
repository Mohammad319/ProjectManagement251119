using BlazorMHD.UI.Core.Services;

namespace BlazorMHD.UI.Core.DesignSystem;

public static class DialogStyles
{
    public static string GetHeaderBackground(MhdState state) => state switch
    {
        // نهاراً: ألوان فاتحة واضحة
        // ليلاً: نفس التدرّج اللوني لكن بدرجة داكنة وهادئة

        MhdState.Primary =>
            "bg-blue-50    dark:bg-blue-950/25",

        MhdState.Success =>
            "bg-emerald-50 dark:bg-emerald-950/20",

        MhdState.Danger =>
            "bg-red-50     dark:bg-red-950/25",

        MhdState.Warning =>
            "bg-amber-50   dark:bg-amber-950/20",

        MhdState.Info =>
            "bg-sky-50     dark:bg-sky-950/25",

        MhdState.Secondary =>
            "bg-violet-50  dark:bg-violet-950/20",

        MhdState.Neutral =>
            "bg-slate-50   dark:bg-slate-900/85",

        _ =>
            "bg-slate-50   dark:bg-slate-900/85"
    };


    public static string GetHeaderAccent(MhdState state) => state switch
    {
        MhdState.Primary => "bg-blue-500",
        MhdState.Success => "bg-emerald-500",
        MhdState.Danger => "bg-red-500",
        MhdState.Warning => "bg-amber-500",
        MhdState.Info => "bg-sky-500",
        MhdState.Secondary => "bg-violet-500",
        MhdState.Neutral => "bg-slate-400",
        _ => "bg-slate-400"
    };

    public static string GetPanelClasses(DialogModel model)
    {
        if (model.Size == DialogSize.FullScreen)
        {
            // Full screen له وضع خاص
            return "bg-white dark:bg-slate-950 " +
                   "border border-slate-200 dark:border-slate-800 " +
                   "w-screen h-screen m-0 rounded-none shadow-xl overflow-hidden flex flex-col";
        }

        var maxHeight = model.MaxHeightClass ?? "max-h-[80vh]";

        return
            "bg-white dark:bg-slate-950 " + // خلفية أغمق شوي من الهيدر عشان يعطينا فصل بصري بسيط
            "border border-slate-200 dark:border-slate-800 " +
            $"{maxHeight} rounded-xl shadow-xl overflow-hidden flex flex-col";
    }
}

