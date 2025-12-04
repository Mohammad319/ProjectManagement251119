using System;
using System.Globalization;

namespace ProjectManagement.Client.Shared.Constants;

/// <summary>
/// أنماط الألوان وتصنيفات الصفوف في الجداول
/// </summary>
public static class UiStyles
{
    public const string Task = "#bec1f9";        // لون المهام
    public const string SubTask = "#b1d2f7";     // لون المهام الفرعية
    public const string Resource = "#e2f1fd";    // لون الموارد

    public const string ClassTask = "table-primary";
    public const string ClassSubTask = "table-secondary";
    public const string ClassResource = "table-warning";
    public const string ClassGroup = "table-light";

    /// <summary>
    /// تنسيق رقم ككائن نصي بعدد معين من الأرقام العشرية
    /// </summary>
    public static string Format(object value, int round)
    {
        return round switch
        {
            1 => string.Format(CultureInfo.InvariantCulture, "{0:0.#}", value),
            2 => string.Format(CultureInfo.InvariantCulture, "{0:0.##}", value),
            3 => string.Format(CultureInfo.InvariantCulture, "{0:0.###}", value),
            4 => string.Format(CultureInfo.InvariantCulture, "{0:0.####}", value),
            5 => string.Format(CultureInfo.InvariantCulture, "{0:0.#####}", value),
            _ => string.Format(CultureInfo.InvariantCulture, "{0:0.##}", value),
        };
    }

    /// <summary>
    /// تقريب رقم عشري بدقة محددة
    /// </summary>
    public static double Format(double value, int round)
    {
        return Math.Round(value, round);
    }
}
