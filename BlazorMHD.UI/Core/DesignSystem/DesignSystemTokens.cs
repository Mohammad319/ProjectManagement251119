namespace BlazorMHD.UI.Core.DesignSystem;

public static class DesignSystemTokens
{
    public static class Spacing
    {
        public const string Xs = "0.125rem";
        public const string Sm = "0.25rem";
        public const string Md = "0.5rem";
        public const string LG = "0.75rem";
        public const string XL = "1rem";
        public const string XXL = "1.5rem";
    }

    public static class Radius
    {
        public const string None = "0";
        public const string Sm = "0.125rem";
        public const string Md = "0.25rem";
        public const string LG = "0.5rem";
        public const string XL = "0.75rem";
    }

    public static class Shadows
    {
        public const string None = "shadow-none";
        public const string Subtle = "shadow-sm";
        public const string Soft = "shadow";
        public const string Strong = "shadow-lg";
    }

    public static class Durations
    {
        public const int Fast = 100;
        public const int Normal = 150;
        public const int Slow = 250;
    }

    public static class ZIndex
    {
        public const int Toast = 1050;
        public const int Dialog = 1100;
        public const int Drawer = 1000;
        public const int Overlay = 1200;
    }
}
