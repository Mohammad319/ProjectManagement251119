namespace BlazorMHD.UI.Core.DesignSystem;

public static class Transitions
{
    // تستخدم للهوفر والضغط والتغيرات البسيطة
    public const string Fast = "transition-colors transition-opacity transition-shadow transition-transform duration-100 ease-in-out";

    // الوضع الافتراضي للمكونات
    public const string Normal = "transition-colors transition-opacity transition-shadow transition-transform duration-150 ease-in-out";

    // للموشن الأبطأ مثل فتح Dialog أو Drawer
    public const string Slow = "transition-colors transition-opacity transition-shadow transition-transform duration-200 ease-in-out";
}


