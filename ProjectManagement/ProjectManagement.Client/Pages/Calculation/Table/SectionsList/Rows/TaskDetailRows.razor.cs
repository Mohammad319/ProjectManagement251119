using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace ProjectManagement.Client.Pages.Calculation.Table.SectionsList.Rows;

public partial class TaskDetailRows
{
    [Parameter] public TaskListMVVM Task { get; set; } = default!;
    [Parameter] public IReadOnlyList<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> Colmuns { get; set; } = Array.Empty<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>>();
    [Parameter] public int Left { get; set; }
    [Parameter] public int MaxFractionDigits { get; set; } = NumericFormatHelper.DefaultMaxFractionDigits;
    [Parameter] public NetColor? Colors { get; set; }

    private string BaseQuantityRowStyle => BuildDetailRowStyle(Colors?.TaskDetailBaseQuantity ?? TemplateConstBase.TaskDetailBaseQuantity);
    private string ConversionParamRowStyle => BuildDetailRowStyle(Colors?.Task ?? TemplateConstBase.Task);
    private bool HasChangeFactor2Column => Colmuns.Any(x => x.Id == NetColumnId.ChangeFactor2);

    private IEnumerable<DetailLine> DetailLines
    {
        get
        {
            var metadata = Task?.Metadata;
            var parameters = metadata?.ConversionParameters;

            if (metadata is null || parameters is not { Count: > 0 })
                yield break;

            var units = TaskConversionUnitDisplayHelper.Resolve(metadata, Task?.Unit);
            var baseUnit = string.IsNullOrWhiteSpace(units.BaseUnit)
                ? (Task?.Unit ?? string.Empty)
                : units.BaseUnit;

            yield return new DetailLine
            {
                Kind = DetailKind.BaseQuantity,
                Name = Task?.Name ?? string.Empty,
                QuantityText = metadata.BaseQuantity.HasValue
                    ? FormatDecimal(metadata.BaseQuantity.Value)
                    : string.Empty,
                Unit = baseUnit,
                Co2Text = FormatDouble(Task?.GetComputedCO2PerBaseQuantity())
            };

            for (int i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                yield return new DetailLine
                {
                    Kind = DetailKind.ConversionParameter,
                    Name = parameter.Name,
                    ChangeFactor2Text = FormatDecimal(parameter.Value),
                    Unit = ResolveConversionParameterUnit(parameter.Name, parameter.Unit)
                };
            }
        }
    }

    private static string ResolveConversionParameterUnit(string? name, string? unit)
    {
        if (string.Equals(name, "Thickness", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Width", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Length", StringComparison.OrdinalIgnoreCase))
        {
            return "m";
        }

        return unit ?? string.Empty;
    }

    private void RenderDetailCells(RenderTreeBuilder builder, DetailLine line)
    {
        var seq = 0;
        for (int i = 0; i < Colmuns.Count; i++)
        {
            var column = Colmuns[i];
            builder.OpenElement(seq++, "td");
            builder.AddAttribute(seq++, "class", GetDetailCellClass(column.Id));
            builder.AddAttribute(seq++, "style", GetDetailCellStyle(column.Id));

            if (line.Kind == DetailKind.BaseQuantity)
            {
                if (column.Id == NetColumnId.Name)
                {
                    RenderNameCell(builder, ref seq, line);
                }
                else if (column.Id == NetColumnId.Quantity)
                {
                    RenderValueCell(builder, ref seq, line.QuantityText);
                }
                else if (column.Id == NetColumnId.Unit)
                {
                    RenderValueCell(builder, ref seq, line.Unit);
                }
                else if (column.Id == NetColumnId.Co2)
                {
                    RenderValueCell(builder, ref seq, line.Co2Text);
                }
                else
                {
                    builder.AddMarkupContent(seq++, "&nbsp;");
                }
            }
            else
            {
                if (column.Id == NetColumnId.Name)
                {
                    RenderNameCell(builder, ref seq, line);
                }
                else if (column.Id == NetColumnId.ChangeFactor2)
                {
                    RenderValueCell(builder, ref seq, line.ChangeFactor2Text);
                }
                else if (!HasChangeFactor2Column && column.Id == NetColumnId.Quantity)
                {
                    RenderValueCell(builder, ref seq, line.ChangeFactor2Text);
                }
                else if (column.Id == NetColumnId.Unit)
                {
                    RenderValueCell(builder, ref seq, line.Unit);
                }
                else
                {
                    builder.AddMarkupContent(seq++, "&nbsp;");
                }
            }

            builder.CloseElement();
        }
    }

    private static void RenderNameCell(RenderTreeBuilder builder, ref int seq, DetailLine line)
    {
        var dotClass = line.Kind == DetailKind.BaseQuantity
            ? "mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-sky-500"
            : "mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-violet-500";

        var textClass = line.Kind == DetailKind.BaseQuantity
            ? "truncate text-[11px] font-medium text-slate-700"
            : "truncate text-[11px] text-slate-700";

        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "flex min-w-0 items-start gap-2");

        builder.OpenElement(seq++, "span");
        builder.AddAttribute(seq++, "class", dotClass);
        builder.CloseElement();

        builder.OpenElement(seq++, "span");
        builder.AddAttribute(seq++, "class", textClass);
        builder.AddContent(seq++, line.Name);
        builder.CloseElement();

        builder.CloseElement();
    }

    private static void RenderValueCell(RenderTreeBuilder builder, ref int seq, string? text)
    {
        builder.OpenElement(seq++, "span");
        builder.AddAttribute(seq++, "class", "text-[11px] font-medium text-slate-700");
        builder.AddContent(seq++, text ?? string.Empty);
        builder.CloseElement();
    }

    private static string GetDetailCellClass(NetColumnId columnId)
    {
        const string baseClass = "px-2 py-1 text-xs align-middle";
        return $"{baseClass} text-left";
    }

    private static string GetDetailCellStyle(NetColumnId columnId) =>
        "background-color: inherit; text-align: left; direction: ltr; unicode-bidi: isolate;";

    private static string BuildDetailRowStyle(string color)
    {
        if (TryParseHexColor(color, out var r, out var g, out var b))
        {
            var bgR = Math.Clamp(r + 22, 0, 255);
            var bgG = Math.Clamp(g + 22, 0, 255);
            var bgB = Math.Clamp(b + 22, 0, 255);
            return $"background-color: rgb({bgR},{bgG},{bgB});box-shadow: inset 2px 0 0 rgba({r},{g},{b},0.55);";
        }

        return "background-color: rgb(248,250,252);box-shadow: inset 2px 0 0 rgba(100,116,139,0.45);";
    }

    private static bool TryParseHexColor(string? input, out int r, out int g, out int b)
    {
        r = g = b = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var value = input.Trim();
        if (value.StartsWith('#'))
            value = value[1..];

        if (value.Length == 3)
            value = string.Concat(value[0], value[0], value[1], value[1], value[2], value[2]);
        else if (value.Length == 8)
            value = value[2..];

        if (value.Length != 6)
            return false;

        if (!int.TryParse(value[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r))
            return false;
        if (!int.TryParse(value.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g))
            return false;
        if (!int.TryParse(value.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b))
            return false;

        return true;
    }

    private string FormatDecimal(decimal value)
        => NumericFormatHelper.Format(value, MaxFractionDigits, CultureInfo.CurrentCulture);

    private string? FormatDouble(double? value)
        => value.HasValue
            ? NumericFormatHelper.Format(value.Value, MaxFractionDigits, CultureInfo.CurrentCulture)
            : null;

    private enum DetailKind
    {
        BaseQuantity,
        ConversionParameter
    }

    private sealed class DetailLine
    {
        public DetailKind Kind { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? QuantityText { get; set; }
        public string? Unit { get; set; }
        public string? ChangeFactor2Text { get; set; }
        public string? Co2Text { get; set; }
    }
}
