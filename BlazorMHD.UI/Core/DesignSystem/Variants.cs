namespace BlazorMHD.UI.Core.DesignSystem;

public static class Variants
{
    public static string Build(MhdVariant variant, MhdState state)
        => variant switch
        {
            MhdVariant.Filled => $"{Colors.GetFilledBg(state)} {Colors.GetFilledText(state)}",
            MhdVariant.Outline => Colors.GetOutline(state),
            MhdVariant.Ghost => Colors.GetGhost(state),
            MhdVariant.Text => Colors.GetText(state),
            _ => $"{Colors.GetFilledBg(state)} {Colors.GetFilledText(state)}",
        };
}
