using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ProjectManagement.Client.Pages.Calculation.Table.SectionsList;
using ProjectManagement.Client.Shared.MVVM.Calculation;

namespace ProjectManagement.Client.Helper
{
    public static class TableRenderHelpers
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // ✅ Cache: لا نعيد بناء الأعمدة كل مرة
        private static readonly Dictionary<(double tax, int x), List<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>>> _colsCache
            = new();

        // ----------------- SELECTION -----------------
        public static void HandleKeyUp(KeyboardEventArgs e) => TemporaryData.Key = null;

        public static void HandleItemSelected(int id, decimal? q, CalculationItemType type)
        {
            if (TemporaryData.Key == "Control")
                SelectedData.Add(id, q, type);
            else
                SelectedData.Reset();
        }

        // ----------------- FORMAT -----------------
        private static string Format(int digits) => "0." + new string('#', digits);

        public static List<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> GetColumns(double tax, int x = 2)
        {
            var key = (tax, x);
            if (_colsCache.TryGetValue(key, out var cached))
                return cached;

            string round = Format(x);

            var cols = new List<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>>(48)
            {
                new() { TaskRender = t => RenderTextTd(t.Metadata.Code), ResRender = _ => EmptyTd() },
                new() { TaskRender = t => RenderCheckboxTd(t.Active), ResRender = r => RenderCheckboxTd(r.Active) },
                new() { TaskRender = _ => EmptyTd(), ResRender = r => RenderWithTitle(r.AccountCode) },
                new() { TaskRender = t => RenderWithTitle(t.Name), ResRender = r => RenderWithTitle(r.Name) },

                new()
                {
                    TaskRender = t => RenderStatusTd(t.StatusColor, t.Status),
                    ResRender = r => RenderStatusTd(
                        r.StatusColor,
                        r.Status,
                        r.HasOfferSelected() ? 0 : r.HasOffer ? 1 : 2,
                        r.OfferClick
                    )
                },

                new() { TaskRender = _ => EmptyTd(), ResRender = r => RenderTextTd(r.ResType) },
                new() { TaskRender = _ => EmptyTd(), ResRender = r => RenderTextTd(r.ResName) },
                new() { TaskRender = _ => EmptyTd(), ResRender = r => RenderTextTd(r.Sort) },

                new() { TaskRender = t => RenderFormattedTd(round, t.Quantity), ResRender = r => RenderFormattedTd(round, r.Quantity) },
                new() { TaskRender = t => RenderTextTd(t.Unit), ResRender = r => RenderTextTd(r.Unit) },
                new() { TaskRender = _ => EmptyTd(), ResRender = r => RenderFormattedTd(round, r.Cost) },

                new() { TaskRender = t => RenderFormattedTd(round, t.Metadata.ChangeFactor1), ResRender = r => RenderFormattedTd(round, r.ChangeFactor1) },
                new() { TaskRender = t => RenderFormattedTd(round, t.Metadata.ChangeFactor2), ResRender = r => RenderFormattedTd(round, r.ChangeFactor2) },

                new()
                {
                    TaskRender = t => RenderTextTd(t.Cap),
                    ResRender = r => (r.ResType == ResourceTypesEnum.Worker || r.ResType == ResourceTypesEnum.MachinesAndEquipments)
                        ? RenderTextTd(r.CapWaste)
                        : EmptyTd()
                },

                new()
                {
                    TaskRender = _ => EmptyTd(),
                    ResRender = r => r.ResType == ResourceTypesEnum.Materials ? RenderTextTd(r.CapWaste) : EmptyTd()
                },

                new() { TaskRender = t => RenderFormattedTd(round, t.BaseCost), ResRender = r => RenderFormattedTd(round, r.BaseCost) },
                new() { TaskRender = t => RenderTextTd(t.Opportunity), ResRender = r => RenderTextTd(r.Opportunity) },

                new() { TaskRender = t => RenderFormattedTd(round, t.NetCostQ), ResRender = r => RenderFormattedTd(round, r.NetCostQ) },
                new() { TaskRender = t => RenderFormattedTd(round, t.NetCostTotaly), ResRender = r => RenderFormattedTd(round, r.NetCostTotaly) },

                new() { TaskRender = t => RenderFormattedTd(round, t.PriceQTax(tax)), ResRender = _ => EmptyTd() },
                new() { TaskRender = t => RenderFormattedTd(round, t.PriceQ), ResRender = _ => EmptyTd() },

                new() { TaskRender = t => RenderFormattedTd(round, t.ApriceTotally), ResRender = r => RenderFormattedTd(round, r.ApriceTotally) },
                new() { TaskRender = t => RenderFormattedTd(round, t.ApriceTotallyTax(tax)), ResRender = _ => EmptyTd() },

                new() { TaskRender = _ => EmptyTd(), ResRender = r => RenderFormattedTd(round, r.Factor) },

                new() { TaskRender = t => RenderFormattedTd(round, t.MinPrice), ResRender = _ => EmptyTd() },
                new() { TaskRender = t => RenderFormattedTd(round, t.CeilingPrice), ResRender = _ => EmptyTd() },

                new() { TaskRender = t => RenderFormattedTd(round, t.PriceSub), ResRender = r => RenderFormattedTd(round, r.PriceSub) },
                new() { TaskRender = t => RenderFormattedTd(round, t.PriceSubTotal), ResRender = r => RenderFormattedTd(round, r.PriceSubTotal) },

                new() { TaskRender = t => RenderFormattedTd(round, t.Diff), ResRender = _ => EmptyTd() },
                new() { TaskRender = t => RenderTextTd(t.Responsible), ResRender = _ => EmptyTd() },

                new() { TaskRender = _ => EmptyTd(), ResRender = r => RenderFormattedTd(round, r.CO2) },
                new() { TaskRender = t => RenderFormattedTd(round, t.TotalCO2), ResRender = r => RenderFormattedTd(round, r.TotalCO2) },

                new() { TaskRender = t => RenderFormattedTd(round, t.Metadata.ActuallyQuantity), ResRender = _ => EmptyTd() },
                new() { TaskRender = t => RenderFormattedTd(round, t.Metadata.WorkedQ), ResRender = _ => EmptyTd() },
                new() { TaskRender = t => RenderFormattedTd(round, t.WorkedQPercent), ResRender = _ => EmptyTd() },

                new() { TaskRender = t => RenderFormattedTd(round, t.PriceActuallyQuantity), ResRender = _ => EmptyTd() },
                new() { TaskRender = t => RenderFormattedTd(round, t.PriceWorkedQ), ResRender = _ => EmptyTd() },

                new() { TaskRender = t => RenderFormattedTd(round, t.PriceSubTax(tax)), ResRender = _ => EmptyTd() },
                new() { TaskRender = t => RenderFormattedTd(round, t.PriceTotalSubTax(tax)), ResRender = _ => EmptyTd() },
                new() { TaskRender = t => RenderFormattedTd(round, t.PriceActuallyQuantityTax(tax)), ResRender = _ => EmptyTd() },
                new() { TaskRender = t => RenderFormattedTd(round, t.PriceWorkedQTax(tax)), ResRender = _ => EmptyTd() },

                new() { TaskRender = t => RenderTextTd(t.Note), ResRender = r => RenderTextTd(r.Note) },
            };

            _colsCache[key] = cols;
            return cols;
        }

        // ----------------- RENDER HELPERS -----------------
        private static RenderFragment RenderTd(string? content, string? cssClass = null) => __b =>
        {
            int seq = 0;
            __b.OpenElement(seq++, "td");
            if (!string.IsNullOrEmpty(cssClass))
                __b.AddAttribute(seq++, "class", cssClass);

            if (!string.IsNullOrEmpty(content))
                __b.AddContent(seq++, content);

            __b.CloseElement();
        };

        private static RenderFragment RenderWithTitle(string? content) => __b =>
        {
            int seq = 0;
            __b.OpenElement(seq++, "td");
            if (!string.IsNullOrEmpty(content))
            {
                __b.AddAttribute(seq++, "title", content);
                __b.AddContent(seq++, content);
            }
            __b.CloseElement();
        };

        public static RenderFragment RenderTextTd(string? value) => RenderTd(value);

        public static RenderFragment RenderTextTd(object? value) =>
            RenderTd(value?.ToString() ?? string.Empty);

        public static RenderFragment RenderFormattedTd(string format, double value) =>
            RenderTd(value.ToString(format, Inv), cssClass: "num-cell");

        public static RenderFragment RenderFormattedTd(string format, double? value) =>
            RenderTd(value.HasValue ? value.Value.ToString(format, Inv) : string.Empty, cssClass: "num-cell");

        public static RenderFragment RenderFormattedTd(string format, decimal value) =>
            RenderTd(value.ToString(format, Inv), cssClass: "num-cell");

        public static RenderFragment RenderFormattedTd(string format, decimal? value) =>
            RenderTd(value.HasValue ? value.Value.ToString(format, Inv) : string.Empty, cssClass: "num-cell");

        public static RenderFragment RenderCheckboxTd(bool isChecked) => __b =>
        {
            int seq = 0;
            __b.OpenElement(seq++, "td");
            __b.OpenElement(seq++, "input");
            __b.AddAttribute(seq++, "type", "checkbox");
            __b.AddAttribute(seq++, "disabled", true);
            if (isChecked)
                __b.AddAttribute(seq++, "checked", true);
            __b.CloseElement();
            __b.CloseElement();
        };

        public static RenderFragment RenderStatusTd(string color, string status) => __b =>
        {
            int seq = 0;
            __b.OpenElement(seq++, "td");

            __b.OpenElement(seq++, "span");
            __b.AddAttribute(seq++, "style",
                $"margin-left:20px;margin-top:6px;width:12px;height:12px;background-color:{color};display:inline-block;border-radius:50%;");
            __b.CloseElement();

            __b.AddContent(seq++, " ");
            __b.AddContent(seq++, status);

            __b.CloseElement();
        };

        public static RenderFragment EmptyTd() => RenderTd(string.Empty);

        public static RenderFragment RenderStatusTd(string color, string status, int i, Func<Task>? onDetailsClick) => __b =>
        {
            int seq = 0;
            __b.OpenElement(seq++, "td");

            __b.OpenElement(seq++, "span");
            __b.AddAttribute(seq++, "style", "position:static");

            if (onDetailsClick != null)
                __b.AddAttribute(seq++, "onclick", onDetailsClick);

            __b.AddMarkupContent(seq++,
                $"<span class='inline-block w-5 text-center [&>svg]:w-3 [&>svg]:h-3'>" +
                $"{(i == 0 ? Icons.Chain : i == 1 ? Icons.ChinUnLink : Icons.Plus)}</span>");

            __b.AddMarkupContent(seq++,
                $"<span style='margin-top:6px;width:12px;height:12px;background-color:{color};display:inline-block;border-radius:50%;'></span> ");

            __b.AddContent(seq++, status);

            __b.CloseElement();
            __b.CloseElement();
        };
    }
}
