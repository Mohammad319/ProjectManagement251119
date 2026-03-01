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
            if (!string.IsNullOrEmpty(style))
            {
                Dictionary<string, string> items = style.Split(';').Select(s => s.Trim()).Where(s => s.Length > 0)
               .ToDictionary(s => s.Substring(0, s.IndexOf(':')).Trim(), d => d.Substring(d.IndexOf(':') + 1).Trim()
               );
                string w = items.FirstOrDefault(x => x.Key == "width").Value;
                if (!string.IsNullOrEmpty(w))
                    Width = int.Parse(w.Substring(0, w.IndexOf("px")));
                else Width = null;

                string h = items.FirstOrDefault(x => x.Key == "height").Value;
                if (!string.IsNullOrEmpty(h))
                    Height = int.Parse(h.Substring(0, h.IndexOf("px")));

                string ml = items.FirstOrDefault(x => x.Key == "margin-left").Value;
                if (!string.IsNullOrEmpty(ml))
                    MarginLeft = int.Parse(ml.Substring(0, ml.IndexOf("px")));
                else MarginLeft = null;

                string mb = items.FirstOrDefault(x => x.Key == "margin-bottom").Value;
                if (!string.IsNullOrEmpty(mb))
                    MarginBottom = int.Parse(mb.Substring(0, mb.IndexOf("px")));

                string mt = items.FirstOrDefault(x => x.Key == "margin-top").Value;
                if (!string.IsNullOrEmpty(mt))
                    MarginTop = int.Parse(mt.Substring(0, mt.IndexOf("px")));

                string pl = items.FirstOrDefault(x => x.Key == "padding-left").Value;
                if (!string.IsNullOrEmpty(pl))
                    PaddingLeft = int.Parse(pl.Substring(0, pl.IndexOf("px")));

                string pr = items.FirstOrDefault(x => x.Key == "padding-right").Value;

                string pb = items.FirstOrDefault(x => x.Key == "padding-bottom").Value;

                string pt = items.FirstOrDefault(x => x.Key == "padding-top").Value;
                if (!string.IsNullOrEmpty(pt))
                    PaddingTop = int.Parse(pt.Substring(0, pt.IndexOf("px")));

                string fs = items.FirstOrDefault(x => x.Key == "font-size").Value;
                if (!string.IsNullOrEmpty(fs))
                    FontSize = int.Parse(fs.Substring(0, fs.IndexOf("px")));
                else FontSize = null;

                if (!string.IsNullOrEmpty(items.FirstOrDefault(x => x.Key == "color").Value))
                    Color = items.FirstOrDefault(x => x.Key == "color").Value;
                else Color = string.Empty;
                if (!string.IsNullOrEmpty(items.FirstOrDefault(x => x.Key == "background-color").Value))
                    BackgroundColor = items.FirstOrDefault(x => x.Key == "background-color").Value;
                else BackgroundColor = string.Empty;

                if (!string.IsNullOrEmpty(items.FirstOrDefault(x => x.Key == "border-color").Value))
                    BorderColor = items.FirstOrDefault(x => x.Key == "border-color").Value;
                if (!string.IsNullOrEmpty(items.FirstOrDefault(x => x.Key == "border-style").Value))
                    BorderStyle = items.FirstOrDefault(x => x.Key == "border-style").Value;
                if (!string.IsNullOrEmpty(items.FirstOrDefault(x => x.Key == "text-align").Value))
                    TextAlign = items.FirstOrDefault(x => x.Key == "text-align").Value;
                if (!string.IsNullOrEmpty(items.FirstOrDefault(x => x.Key == "font-weight").Value))
                    FontWeight = items.FirstOrDefault(x => x.Key == "font-weight").Value;

            }
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
