using System.Globalization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace ProjectManagement.Client.Helper
{
    public static class TableRenderHelpers
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private static void RenderTd(RenderTreeBuilder builder, string? content, string? cssClass = null)
        {
            int seq = 0;
            builder.OpenElement(seq++, "td");
            if (!string.IsNullOrEmpty(cssClass))
                builder.AddAttribute(seq++, "class", cssClass);

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
                builder.AddAttribute(seq++, "title", content);
                builder.AddContent(seq++, content);
            }

            builder.CloseElement();
        }

        public static void RenderTextTd(RenderTreeBuilder builder, string? value) =>
            RenderTd(builder, value);

        public static void RenderTextTd(RenderTreeBuilder builder, object? value) =>
            RenderTd(builder, value?.ToString() ?? string.Empty);

        public static void RenderFormattedTd(RenderTreeBuilder builder, string format, double value) =>
            RenderTd(builder, NumericFormatHelper.Format(value, NumericFormatHelper.ExtractMaxFractionDigits(format), Inv), cssClass: "num-cell");

        public static void RenderFormattedTd(RenderTreeBuilder builder, string format, double? value) =>
            RenderTd(builder, NumericFormatHelper.Format(value, NumericFormatHelper.ExtractMaxFractionDigits(format), Inv), cssClass: "num-cell");

        public static void RenderFormattedTd(RenderTreeBuilder builder, string format, decimal value) =>
            RenderTd(builder, NumericFormatHelper.Format(value, NumericFormatHelper.ExtractMaxFractionDigits(format), Inv), cssClass: "num-cell");

        public static void RenderFormattedTd(RenderTreeBuilder builder, string format, decimal? value) =>
            RenderTd(builder, NumericFormatHelper.Format(value, NumericFormatHelper.ExtractMaxFractionDigits(format), Inv), cssClass: "num-cell");

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
            Func<System.Threading.Tasks.Task>? onDetailsClick)
        {
            int seq = 0;
            builder.OpenElement(seq++, "td");

            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "style", "position:static");

            if (onDetailsClick != null)
            {
                builder.AddAttribute(seq++, "onclick", onDetailsClick);
                builder.AddEventStopPropagationAttribute(seq++, "onclick", true);
            }

            builder.AddMarkupContent(seq++,
                $"<span class='inline-block w-5 text-center [&>svg]:w-3 [&>svg]:h-3'>" +
                $"{(i == 0 ? Icons.Chain : i == 1 ? Icons.ChinUnLink : Icons.Plus)}</span>");

            builder.AddMarkupContent(seq++,
                $"<span style='margin-top:6px;width:12px;height:12px;background-color:{color};display:inline-block;border-radius:50%;'></span> ");

            builder.AddContent(seq++, status);

            builder.CloseElement();
            builder.CloseElement();
        }
    }
}
