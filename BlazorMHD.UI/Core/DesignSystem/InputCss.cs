namespace BlazorMHD.UI.Core.DesignSystem;

public static class InputCss
{
    public static string Stack => "mhd-input-stack";
    public static string Label => "mhd-input-label";
    public static string Helper => "mhd-input-helper";

    public static string Field(MhdSize size, bool disabled = false, bool readOnly = false)
    {
        var classes = new List<string>
        {
            "mhd-input-field-base",
            FieldSize(size)
        };

        if (disabled)
            classes.Add("mhd-input-field-disabled");

        if (readOnly)
            classes.Add("mhd-input-field-readonly");

        return string.Join(" ", classes);
    }

    public static string Shell(MhdSize size, bool disabled = false, bool readOnly = false)
    {
        var classes = new List<string>
        {
            "mhd-input-shell-base",
            ShellSize(size)
        };

        if (disabled)
            classes.Add("mhd-input-shell-disabled");

        if (readOnly)
            classes.Add("mhd-input-shell-readonly");

        return string.Join(" ", classes);
    }

    public static string SearchInput(MhdSize size) => string.Join(" ", new[]
    {
        "mhd-input-inline-field",
        TextSize(size)
    });

    public static string AutoCompletePanel => "mhd-ac-panel";
    public static string Busy => "mhd-ac-busy";

    public static string AutoCompleteItem(bool active)
        => active ? "mhd-ac-item mhd-ac-item-active" : "mhd-ac-item mhd-ac-item-hover";

    public static string ToggleTrack(MhdSize size, bool value, bool disabled)
    {
        var classes = new List<string>
        {
            "mhd-toggle-track",
            size switch
            {
                MhdSize.Xs => "mhd-toggle-track-xs",
                MhdSize.Md => "mhd-toggle-track-md",
                MhdSize.LG => "mhd-toggle-track-lg",
                _ => "mhd-toggle-track-sm"
            },
            value ? "mhd-toggle-on" : "mhd-toggle-off"
        };

        classes.Add(disabled ? "mhd-state-disabled" : "cursor-pointer");
        return string.Join(" ", classes);
    }

    public static string ToggleThumb(MhdSize size, bool value)
        => size switch
        {
            MhdSize.Xs => value ? "mhd-toggle-thumb mhd-toggle-thumb-xs mhd-toggle-thumb-xs-on" : "mhd-toggle-thumb mhd-toggle-thumb-xs mhd-toggle-thumb-xs-off",
            MhdSize.Md => value ? "mhd-toggle-thumb mhd-toggle-thumb-md mhd-toggle-thumb-md-on" : "mhd-toggle-thumb mhd-toggle-thumb-md mhd-toggle-thumb-md-off",
            MhdSize.LG => value ? "mhd-toggle-thumb mhd-toggle-thumb-lg mhd-toggle-thumb-lg-on" : "mhd-toggle-thumb mhd-toggle-thumb-lg mhd-toggle-thumb-lg-off",
            _ => value ? "mhd-toggle-thumb mhd-toggle-thumb-sm mhd-toggle-thumb-sm-on" : "mhd-toggle-thumb mhd-toggle-thumb-sm mhd-toggle-thumb-sm-off"
        };

    private static string FieldSize(MhdSize size) => size switch
    {
        MhdSize.Xs => "mhd-input-field-xs",
        MhdSize.Sm => "mhd-input-field-sm",
        MhdSize.Md => "mhd-input-field-md",
        MhdSize.LG => "mhd-input-field-lg",
        MhdSize.XL => "mhd-input-field-xl",
        _ => "mhd-input-field-sm"
    };

    private static string ShellSize(MhdSize size) => size switch
    {
        MhdSize.Xs => "mhd-input-shell-xs",
        MhdSize.Sm => "mhd-input-shell-sm",
        MhdSize.Md => "mhd-input-shell-md",
        MhdSize.LG => "mhd-input-shell-lg",
        MhdSize.XL => "mhd-input-shell-xl",
        _ => "mhd-input-shell-sm"
    };

    private static string TextSize(MhdSize size) => size switch
    {
        MhdSize.Xs => "mhd-input-text-xs",
        MhdSize.Sm => "mhd-input-text-sm",
        MhdSize.Md => "mhd-input-text-md",
        MhdSize.LG => "mhd-input-text-lg",
        MhdSize.XL => "mhd-input-text-xl",
        _ => "mhd-input-text-sm"
    };
}
