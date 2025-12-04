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
            // Layout أساسي للزر
            "inline-flex items-center justify-center whitespace-nowrap",

            // فوكس Tailwind مباشر
            "focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-1 focus-visible:ring-blue-500",

            // نصف القطر و الترانزيشن من نظامك
            RadiusCss.Md,
            Transitions.Normal,

            // الحجم من Sizes
            Sizes.GetPadding(size),
            Sizes.GetTextSize(size)
        };

        // الألوان/الستايل حسب الـ Variant و الـ State (من Variants)
        classes.Add(Variants.Build(variant, state));

        if (fullWidth)
            classes.Add("w-full");

        if (disabled)
            classes.Add("opacity-50 cursor-not-allowed");

        return string.Join(" ", classes);
    }

    public string GetTagClass(MhdState state, MhdSize size)
    {
        return string.Join(" ", new[]
        {
            "inline-flex items-center rounded-full",
            Sizes.GetPadding(size),
            Sizes.GetTextSize(size),
            // لاصق Ghost من Variants – يفترض الآن أنه مبني على Tailwind أيضًا
            Variants.Build(MhdVariant.Ghost, state)
        });
    }

    public string GetCardClass(bool elevated = false)
    {
        return string.Join(" ", new[]
        {
            // سطح الكارد: Tailwind فقط
            "bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100",
            RadiusCss.LG,
            elevated ? Shadows.Subtle : Shadows.None,
            "border border-slate-200 dark:border-slate-800"
        });
    }

    public string GetSurfaceClass()
    {
        // سطح عام (مثل bg-mhd-surface القديم لكن الآن Tailwind)
        return "bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100";
    }
}
