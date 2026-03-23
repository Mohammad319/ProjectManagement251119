using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace ProjectManagement.Client.Pages.Calculation.Table.SectionsList.Rows;

public partial class ResourceDetailRows
{
    [Parameter] public ResourceListMVVM Resource { get; set; } = default!;
    [Parameter] public IReadOnlyList<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> Colmuns { get; set; } = Array.Empty<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>>();
    [Parameter] public int Left { get; set; }
    [Parameter] public string Color { get; set; } = string.Empty;

    private string DetailRowStyle => BuildDetailRowStyle(Color);

    private IEnumerable<DetailLine> DetailLines
    {
        get
        {
            if (Resource?.Data?.Parameters != null)
            {
                foreach (var p in Resource.Data.Parameters)
                {
                    yield return new DetailLine
                    {
                        Kind = DetailKind.Parameter,
                        Name = p.Name,
                        Unit = p.Unit,
                        QuantityText = FormatDecimal(p.Value),
                        CostText = null
                    };
                }
            }

            if (Resource?.Data?.Times != null)
            {
                foreach (var t in Resource.Data.Times)
                {
                    yield return new DetailLine
                    {
                        Kind = DetailKind.Time,
                        Name = t.Name,
                        Unit = null,
                        QuantityText = FormatDecimal(t.Quantity),
                        CostText = FormatDecimal(t.Cost)
                    };
                }
            }
        }
    }

    private void RenderDetailCells(RenderTreeBuilder builder, DetailLine line)
    {
        var seq = 0;
        var count = Colmuns?.Count ?? 0;

        for (int i = 0; i < count; i++)
        {
            var column = Colmuns[i];

            builder.OpenElement(seq++, "td");
            builder.AddAttribute(seq++, "class", GetDetailCellClass(column.Id));

            if (column.Id == NetColumnId.Name)
            {
                RenderNameCell(builder, ref seq, line);
            }
            else if (column.Id == NetColumnId.Quantity)
            {
                RenderQuantityCell(builder, ref seq, line);
            }
            else if (line.Kind == DetailKind.Parameter && column.Id == NetColumnId.Unit)
            {
                RenderUnitCell(builder, ref seq, line);
            }
            else if (line.Kind == DetailKind.Time && column.Id == NetColumnId.Cost)
            {
                RenderCostCell(builder, ref seq, line);
            }
            else
            {
                builder.AddMarkupContent(seq++, "&nbsp;");
            }

            builder.CloseElement();
        }
    }

    private static void RenderNameCell(RenderTreeBuilder builder, ref int seq, DetailLine line)
    {
        var dotClass = line.Kind == DetailKind.Time
            ? "mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-slate-500 dark:bg-slate-400"
            : "mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-slate-400 dark:bg-slate-500";

        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "flex min-w-0 items-start gap-2");

        builder.OpenElement(seq++, "span");
        builder.AddAttribute(seq++, "class", dotClass);
        builder.CloseElement();

        builder.OpenElement(seq++, "span");
        builder.AddAttribute(seq++, "class", "truncate text-[11px] text-slate-700 dark:text-slate-200");
        builder.AddContent(seq++, line.Name);
        builder.CloseElement();

        builder.CloseElement();
    }

    private static void RenderQuantityCell(RenderTreeBuilder builder, ref int seq, DetailLine line)
    {
        builder.OpenElement(seq++, "span");
        builder.AddAttribute(seq++, "class", "text-[11px] font-medium text-slate-700 dark:text-slate-200");
        builder.AddContent(seq++, line.QuantityText);
        builder.CloseElement();
    }

    private static void RenderUnitCell(RenderTreeBuilder builder, ref int seq, DetailLine line)
    {
        builder.OpenElement(seq++, "span");
        builder.AddAttribute(seq++, "class", "text-[11px] text-slate-600 dark:text-slate-300");
        builder.AddContent(seq++, string.IsNullOrWhiteSpace(line.Unit) ? "—" : line.Unit);
        builder.CloseElement();
    }

    private static void RenderCostCell(RenderTreeBuilder builder, ref int seq, DetailLine line)
    {
        builder.OpenElement(seq++, "span");
        builder.AddAttribute(seq++, "class", "text-[11px] font-medium text-slate-700 dark:text-slate-200");
        builder.AddContent(seq++, line.CostText ?? string.Empty);
        builder.CloseElement();
    }

    private static string GetDetailCellClass(NetColumnId columnId)
    {
        var baseClass = "border-t border-slate-200/60 px-2 py-1.5 align-middle dark:border-slate-700/60";

        if (columnId == NetColumnId.Name)
            return baseClass + " text-left";

        if (columnId == NetColumnId.Unit ||
            columnId == NetColumnId.Quantity ||
            columnId == NetColumnId.Cost)
            return baseClass + " whitespace-nowrap";

        return baseClass;
    }

    private static string BuildDetailRowStyle(string? color)
    {
        if (TryParseHexColor(color, out var r, out var g, out var b))
        {
            return
                $"background:linear-gradient(90deg, rgba({r},{g},{b},0.045) 0%, rgba({r},{g},{b},0.02) 24%, transparent 100%);" +
                $"box-shadow: inset 2px 0 0 rgba({r},{g},{b},0.45);";
        }

        return "background:linear-gradient(90deg, rgba(148,163,184,0.06) 0%, rgba(148,163,184,0.02) 24%, transparent 100%); box-shadow: inset 2px 0 0 rgba(100,116,139,0.40);";
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
        {
            value = string.Concat(value[0], value[0], value[1], value[1], value[2], value[2]);
        }
        else if (value.Length == 8)
        {
            value = value[2..];
        }

        if (value.Length != 6)
            return false;

        if (!int.TryParse(value[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r))
            return false;

        if (!int.TryParse(value.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g))
            return false;

        if (!int.TryParse(value.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b))
            return false;

        return true;
    }

    private static string FormatDecimal(decimal value)
        => value.ToString("0.##", CultureInfo.CurrentCulture);

    private enum DetailKind
    {
        Parameter,
        Time
    }

    private sealed class DetailLine
    {
        public DetailKind Kind { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public string QuantityText { get; set; } = string.Empty;
        public string? CostText { get; set; }
    }
}-