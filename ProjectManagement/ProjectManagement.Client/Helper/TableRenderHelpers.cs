using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace ProjectManagement.Client.Helper
{
    public static class TableRenderHelpers
    {
        private static void RenderTd(RenderTreeBuilder builder, string? content, string? cssClass = null, string? title = null)
        {
            int seq = 0;
            builder.OpenElement(seq++, "td");
            if (!string.IsNullOrEmpty(cssClass))
                builder.AddAttribute(seq++, "class", cssClass);

            if (!string.IsNullOrWhiteSpace(title))
                builder.AddAttribute(seq++, "title", title);

            if (!string.IsNullOrEmpty(content))
                builder.AddContent(seq++, content);

            builder.CloseElement();
        }

        public static void RenderWithTitle(RenderTreeBuilder builder, string? content)
        {
            int seq = 0;
            builder.OpenElement(seq++, "td");
            if (!string.IsNullOrEmpty(content))
            {
                builder.AddAttribute(seq++, "data-pm-overflow-title", content);
                builder.AddContent(seq++, content);
            }

            builder.CloseElement();
        }

        public static void RenderWithTitleIndented(RenderTreeBuilder builder, string? content, int indentPx)
        {
            int seq = 0;
            builder.OpenElement(seq++, "td");
            if (indentPx > 0)
                builder.AddAttribute(seq++, "style", $"padding-left:{indentPx}px");
            if (!string.IsNullOrEmpty(content))
            {
                builder.AddAttribute(seq++, "data-pm-overflow-title", content);
                builder.AddContent(seq++, content);
            }

            builder.CloseElement();
        }

        public static void RenderTextTd(RenderTreeBuilder builder, string? value) =>
            RenderTd(builder, value);

        public static void RenderTextTd(RenderTreeBuilder builder, string? value, string? title) =>
            RenderTd(builder, value, title: title);

        public static void RenderTextTd(RenderTreeBuilder builder, object? value) =>
            RenderTd(builder, value?.ToString() ?? string.Empty);

        public static void RenderFormattedTd(RenderTreeBuilder builder, string format, double value) =>
            RenderTd(builder, NumericFormatHelper.Format(value, NumericFormatHelper.ExtractMaxFractionDigits(format)), cssClass: "num-cell");

        public static void RenderFormattedTd(RenderTreeBuilder builder, string format, double? value) =>
            RenderTd(builder, NumericFormatHelper.Format(value, NumericFormatHelper.ExtractMaxFractionDigits(format)), cssClass: "num-cell");

        public static void RenderFormattedTd(RenderTreeBuilder builder, string format, decimal value) =>
            RenderTd(builder, NumericFormatHelper.Format(value, NumericFormatHelper.ExtractMaxFractionDigits(format)), cssClass: "num-cell");

        public static void RenderFormattedTd(RenderTreeBuilder builder, string format, decimal? value) =>
            RenderTd(builder, NumericFormatHelper.Format(value, NumericFormatHelper.ExtractMaxFractionDigits(format)), cssClass: "num-cell");

        public static void RenderCheckboxTd(RenderTreeBuilder builder, bool isChecked)
        {
            int seq = 0;
            builder.OpenElement(seq++, "td");
            builder.OpenElement(seq++, "input");
            builder.AddAttribute(seq++, "type", "checkbox");
            builder.AddAttribute(seq++, "disabled", true);
            if (isChecked)
                builder.AddAttribute(seq++, "checked", true);

            builder.CloseElement();
            builder.CloseElement();
        }

        public static void RenderActiveToggleTd(RenderTreeBuilder builder, bool isActive, Func<Task>? onToggle)
        {
            int seq = 0;
            var label = isActive ? "Active" : "Inactive";

            builder.OpenElement(seq++, "td");
            builder.AddAttribute(seq++, "class", "calc-active-cell");

            builder.OpenElement(seq++, "button");
            builder.AddAttribute(seq++, "type", "button");
            builder.AddAttribute(seq++, "title", label);
            builder.AddAttribute(seq++, "aria-label", label);
            builder.AddAttribute(seq++, "aria-pressed", isActive ? "true" : "false");
            builder.AddAttribute(seq++, "class", isActive ? "calc-active-toggle is-active" : "calc-active-toggle is-inactive");

            if (onToggle is not null)
            {
                builder.AddAttribute(seq++, "onclick", onToggle);
                builder.AddEventStopPropagationAttribute(seq++, "onclick", true);
            }

            builder.CloseElement();
            builder.CloseElement();
        }

        public static void RenderStatusTd(RenderTreeBuilder builder, string color, string status)
        {
            int seq = 0;
            builder.OpenElement(seq++, "td");

            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "style",
                $"margin-left:20px;margin-top:6px;width:12px;height:12px;background-color:{color};display:inline-block;border-radius:50%;");
            builder.CloseElement();

            builder.AddContent(seq++, " ");
            builder.AddContent(seq++, status);

            builder.CloseElement();
        }

        public static void EmptyTd(RenderTreeBuilder builder) => RenderTd(builder, string.Empty);

       public static void RenderStatusTd(
         RenderTreeBuilder builder,
         string color,
         string status,
         int i,
         Func<Task>? onDetailsClick)
        {
            int seq = 0;

            builder.OpenElement(seq++, "td");

            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "style", "position:static");

            // الأيقونة فقط هي القابلة للنقر
            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "class",
                $"inline-block w-5 text-center [&>svg]:w-3 [&>svg]:h-3{(onDetailsClick != null ? " cursor-pointer" : "")}");

            if (onDetailsClick != null)
            {
                builder.AddAttribute(seq++, "onclick", onDetailsClick);
                builder.AddEventStopPropagationAttribute(seq++, "onclick", true);
            }

            builder.AddMarkupContent(seq++,
                i == 0 ? Icons.Chain : i == 1 ? Icons.ChinUnLink : Icons.Plus);

            builder.CloseElement(); // span icon

            builder.AddMarkupContent(seq++,
                $"<span style='margin-top:6px;width:12px;height:12px;background-color:{color};display:inline-block;border-radius:50%;'></span> ");

            builder.AddContent(seq++, status);

            builder.CloseElement(); // outer span
            builder.CloseElement(); // td
        }
    }
}
