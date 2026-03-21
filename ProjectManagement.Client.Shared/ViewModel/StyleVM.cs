using System.Collections.Generic;
using System.Linq;

namespace ProjectManagement.Client.Shared.ViewModel
{
    public class StyleVM
    {
        public int? Width { get; set; } = 100;
        public int? Height { get; set; } = 35;
        public string Color { get; set; } = "#00000";
        public string BackgroundColor { get; set; } = "#FFFFFF";
        public int? MarginLeft { get; set; } = 5;
        public int? MarginTop { get; set; }
        public int? MarginBottom { get; set; }
        public int? PaddingLeft { get; set; }
        public int? PaddingTop { get; set; }

        public string BorderStyle { get; set; } = string.Empty;
        public string BorderColor { get; set; } = string.Empty;
        public string TextAlign { get; set; } = string.Empty;
        public int? FontSize { get; set; } = 13;
        public string FontWeight { get; set; } = string.Empty;

        public void Set(string style)
        {
            if (string.IsNullOrWhiteSpace(style))
                return;

            Dictionary<string, string> items = style
                .Split(';')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0 && s.Contains(':'))
                .Select(s => new
                {
                    Key = s[..s.IndexOf(':')].Trim(),
                    Value = s[(s.IndexOf(':') + 1)..].Trim()
                })
                .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Last().Value, StringComparer.OrdinalIgnoreCase);

            static int? ParsePx(Dictionary<string, string> source, string key)
            {
                if (!source.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
                    return null;

                raw = raw.Trim();
                if (raw.EndsWith("px", StringComparison.OrdinalIgnoreCase))
                    raw = raw[..^2].Trim();

                return int.TryParse(raw, out var value) ? value : null;
            }

            static string GetValue(Dictionary<string, string> source, string key)
                => source.TryGetValue(key, out var value) ? value : string.Empty;

            Width = ParsePx(items, "width");
            Height = ParsePx(items, "height");
            MarginLeft = ParsePx(items, "margin-left");
            MarginBottom = ParsePx(items, "margin-bottom");
            MarginTop = ParsePx(items, "margin-top");
            PaddingLeft = ParsePx(items, "padding-left");
            PaddingTop = ParsePx(items, "padding-top");
            FontSize = ParsePx(items, "font-size");

            Color = GetValue(items, "color");
            BackgroundColor = GetValue(items, "background-color");
            BorderColor = GetValue(items, "border-color");
            BorderStyle = GetValue(items, "border-style");
            TextAlign = GetValue(items, "text-align");
            FontWeight = GetValue(items, "font-weight");
        }
        public string GetString()
        {
            string Style = "";
            if (!string.IsNullOrEmpty(Color)) Style += $"color:{Color};";
            if (!string.IsNullOrEmpty(BackgroundColor)) Style += $"background-color:{BackgroundColor};";
            if (Width.HasValue) Style += $"width:{Width}px;";
            if (Height.HasValue) Style += $"height:{Height}px;";
            if (MarginBottom.HasValue) Style += $"margin-bottom:{MarginBottom}px;";
            if (MarginTop.HasValue) Style += $"margin-top:{MarginTop}px;";
            if (MarginLeft.HasValue) Style += $"margin-left:{MarginLeft}px;";
            if (PaddingLeft.HasValue) Style += $"padding-left:{PaddingLeft}px;";
            if (PaddingTop.HasValue) Style += $"padding-top:{PaddingTop}px;";
            if (FontSize.HasValue) Style += $"font-size:{FontSize}px;";

            if (!string.IsNullOrEmpty(BorderColor)) Style += $"border-color:{BorderColor};";
            if (!string.IsNullOrEmpty(BorderStyle)) Style += $"border-style:{BorderStyle};";
            if (!string.IsNullOrEmpty(TextAlign)) Style += $"text-align:{TextAlign};";
            if (!string.IsNullOrEmpty(FontWeight)) Style += $"font-weight:{FontWeight};";

            return Style;
        }

        public void BoolStyle()
        {
            Width = 20;
            Height = 20;
            MarginBottom = 10;
        }
    }
}
