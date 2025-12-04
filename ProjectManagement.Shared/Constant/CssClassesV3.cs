using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManagement.Shared.Constant
{
    #region New API — CssClassesV2 (preferred going forward)

    public static class CssClassesV3_2
    {
        public static class Text
        {
            public const string H5 = "font-medium text-gray-800 dark:text-gray-100";
            public const string MutedXs = "text-xs " + MutedText;
            public const string Sm = "text-sm";
            public const string Size11 = "text-[11px]";
            public const string Size12 = "text-[12px]";
            public const string Size13 = "text-[13px]";
            public const string SemiBold = "font-semibold";

            public const string LabelText = "text-gray-700 dark:text-gray-200";
            public const string MutedText = "text-gray-500 dark:text-gray-400";

            public const string LabelXs = "text-xs " + LabelText;
            public const string Uppercase = "uppercase";

        }
        public static class Card
        {
            // رئيسية
            public const string Panel =
                "rounded border mb-1 p-1 bg-white dark:bg-gray-900/60 border-gray-200 dark:border-gray-700";

            // رئيسية لكن أكبر حشوة
            public const string PanelLg =
                "rounded border p-2 bg-white dark:bg-gray-900/60 border-gray-200 dark:border-gray-700";

            // فرعية داخل Panel
            public const string SubPanel =
                "mt-2 rounded border p-1 bg-white/60 dark:bg-gray-900/40 border-gray-200 dark:border-gray-700";

            // بدون حدود (مثال: داخل مودال)
            public const string Borderless =
                "rounded p-2 bg-white dark:bg-gray-900/60";

            // ناعمة ومخففة
            public const string SoftMuted =
                "rounded-lg shadow-sm border border-gray-100 dark:border-gray-800 bg-white/70 dark:bg-gray-900/50";

            // بطاقات Muted (لإبراز أقسام ثانوية)
            public const string Muted =
                "rounded border p-2 bg-gray-50 dark:bg-gray-900/40 border-gray-200 dark:border-gray-800";
        }

        public static class Skeleton
        {
            public const string Base = "animate-pulse rounded-md bg-gray-200 dark:bg-gray-700";
            public const string Avatar = Base + " h-10 w-10 rounded-full";
        }
    }

    #endregion
}
