using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ProjectManagement.Client.Pages.Calculation.Table.SectionsList;
using ProjectManagement.Client.Shared.MVVM.Calculation;

namespace ProjectManagement.Client.Helper
{
    public static class TableRenderHelpers
    {
        public static void ToggleCollapse(TaskListMVVM task, List<TaskListMVVM> allTasks)
        {
            var list = allTasks.Where(x => x.TaskId == task.Id).ToList();
            task.CollSpan = !task.CollSpan;

            foreach (var item in list)
            {
                item.CollSpan = !item.CollSpan;
                ToggleCollapse(item, allTasks);
            }
        }

        //-----------------
        public static void HandleKeyUp(KeyboardEventArgs e)
        {
            TemporaryData.Key = null;
        }

        public static void HandleItemSelected(int id, double? q, CalculationItemType type)
        {
            if (TemporaryData.Key == "Control")
                SelectedData.Add(id, q, type);
            else
                SelectedData.Reset();
        }

        static string Format(int round) => round switch
        {
            1 => "{0:0.#}",
            2 => "{0:0.##}",
            3 => "{0:0.###}",
            4 => "{0:0.####}",
            5 => "{0:0.#####}",
            _ => "{0:0.##}",
        };

        public static List<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> GetColumns(double tax, int x = 2)
        {
            string round = Format(x);
            return new()
            {
                new() { Title = "الكود", TaskRender = t => RenderTextTd(t.Metadata.Code), ResRender = r => EmptyTd() },

                new() { Title = "فعال", TaskRender = t => RenderCheckboxTd(t.Active), ResRender = r => RenderCheckboxTd(r.Active) },

                new() { Title = "كود الحساب", TaskRender = t => EmptyTd(), ResRender = r => RenderWithTitle(r.AccountCode) },

                new() { Title = "الاسم", TaskRender = t => RenderWithTitle(t.Name), ResRender = r => RenderWithTitle(r.Name) },

                new()
                {
                    Title = "الحالة",
                    TaskRender = t => RenderStatusTd(t.StatusColor, t.Status),
                    ResRender = r => RenderStatusTd(
                        r.StatusColor,
                        r.Status,
                        r.HasOfferSelected() ? 0 : r.HasOffer ? 1 : 2,
                        r.OfferClick // هنا نمرّر Func<Task>
                    )
                },

                new() { Title = "نوع المورد", TaskRender = t => EmptyTd(), ResRender = r => RenderTextTd(r.ResType) },

                new() { Title = "اسم المورد", TaskRender = t => EmptyTd(), ResRender = r => RenderTextTd(r.ResName) },

                new() { Title = "الترتيب", TaskRender = t => EmptyTd(), ResRender = r => RenderTextTd(r.Sort) },

                new() { Title = "الكمية", TaskRender = t => RenderFormattedTd(round, t.Quantity), ResRender = r => RenderFormattedTd(round, r.Quantity) },

                new() { Title = "الوحدة", TaskRender = t => RenderTextTd(t.Unit), ResRender = r => RenderTextTd(r.Unit) },

                new() { Title = "التكلفة", TaskRender = t => EmptyTd(), ResRender = r => RenderFormattedTd(round, r.Cost) },

                new() { Title = "عامل تغيير 1", TaskRender = t => RenderTextTd(t.Metadata.ChangeFactor1), ResRender = r => RenderTextTd(r.ChangeFactor1) },

                new() { Title = "عامل تغيير 2", TaskRender = t => RenderTextTd(t.Metadata.ChangeFactor2), ResRender = r => RenderTextTd(r.ChangeFactor2) },

                new() { Title = "الطاقة", TaskRender = t => RenderTextTd(t.Cap),
                        ResRender = r => (r.ResType == ResourceTypesEnum.Worker || r.ResType == ResourceTypesEnum.MachinesAndEquipments)
                            ? RenderTextTd(r.CapWaste) : EmptyTd() },

                new() { Title = "الهدر", TaskRender = t => EmptyTd(),
                        ResRender = r => r.ResType == ResourceTypesEnum.Materials ? RenderTextTd(r.CapWaste) : EmptyTd() },

                new() { Title = "تكلفة أساسية", TaskRender = t => RenderFormattedTd(round, t.BaseCost), ResRender = r => RenderFormattedTd(round, r.BaseCost) },

                new() { Title = "فرصة", TaskRender = t => RenderTextTd(t.Opportunity), ResRender = r => RenderTextTd(r.Opportunity) },

                new() { Title = "صافي تكلفة/وحدة", TaskRender = t => RenderFormattedTd(round, t.NetCostQ), ResRender = r => RenderFormattedTd(round, r.NetCostQ) },

                new() { Title = "صافي تكلفة كلي", TaskRender = t => RenderFormattedTd(round, t.NetCostTotaly), ResRender = r => RenderFormattedTd(round, r.NetCostTotaly) },

                new() { Title = "سعر بوحدة مع ضريبة", TaskRender = t => RenderFormattedTd(round, t.PriceQTax(tax)), ResRender = r => EmptyTd() },

                new() { Title = "سعر بوحدة", TaskRender = t => RenderFormattedTd(round, t.PriceQ), ResRender = r => EmptyTd() },

                new() { Title = "سعر كلي بدون ضريبة", TaskRender = t => RenderFormattedTd(round, t.ApriceTotally), ResRender = r => RenderFormattedTd(round, r.ApriceTotally) },

                new() { Title = "سعر كلي مع ضريبة", TaskRender = t => RenderFormattedTd(round, t.ApriceTotallyTax(tax)), ResRender = r => EmptyTd() },

                new() { Title = "معامل", TaskRender = t => EmptyTd(), ResRender = r => RenderFormattedTd(round, r.Factor) },

                new() { Title = "أقل سعر", TaskRender = t => RenderFormattedTd(round, t.MinPrice), ResRender = r => EmptyTd() },

                new() { Title = "السعر الأعلى", TaskRender = t => RenderFormattedTd(round, t.CeilingPrice), ResRender = r => EmptyTd() },

                new() { Title = "سعر فرعي", TaskRender = t => RenderFormattedTd(round, t.PriceSub), ResRender = r => EmptyTd() },

                new() { Title = "سعر فرعي كلي", TaskRender = t => RenderFormattedTd(round, t.PriceSubTotal), ResRender = r => EmptyTd() },

                new() { Title = "فرق السعر", TaskRender = t => RenderFormattedTd(round, t.Diff), ResRender = r => EmptyTd() },

                new() { Title = "المسؤول", TaskRender = t => RenderTextTd(t.Responsible), ResRender = r => EmptyTd() },

                new() { Title = "انبعاث كربوني", TaskRender = t => EmptyTd(), ResRender = r => RenderFormattedTd(round, r.CO2) },

                new() { Title = "انبعاث كلي", TaskRender = t => RenderFormattedTd(round, t.TotalCO2), ResRender = r => RenderFormattedTd(round, r.TotalCO2) },

                new() { Title = "ملاحظة", TaskRender = t => RenderTextTd(t.Note), ResRender = r => RenderTextTd(r.Note) },
            };
        }

        // دوال مساعدة

        static RenderFragment RenderTd(string content, string cssClass = null) => __b =>
        {
            var seq = 0;
            __b.OpenElement(seq++, "td");
            if (!string.IsNullOrEmpty(cssClass))
                __b.AddAttribute(seq++, "class", cssClass);

            if (!string.IsNullOrEmpty(content))
                __b.AddContent(seq++, content);

            __b.CloseElement();
        };

        static RenderFragment RenderWithTitle(string content) => __b =>
        {
            var seq = 0;
            __b.OpenElement(seq++, "td");
            if (!string.IsNullOrEmpty(content))
            {
                __b.AddAttribute(seq++, "title", content);
                __b.AddContent(seq++, content);
            }
            __b.CloseElement();
        };

        public static RenderFragment RenderTextTd(object value) =>
            RenderTd(value?.ToString() ?? string.Empty);
        public static RenderFragment RenderFormattedTd(string format, object value) 
            => RenderTextTd(string.Format(format, value));

        public static RenderFragment RenderCheckboxTd(bool isChecked) => __b =>
        {
            var seq = 0;
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
            var seq = 0;
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

        public static RenderFragment RenderStatusTd(string color, string status, int i, Func<Task> onDetailsClick) => __b =>
        {
            int seq = 0;
            __b.OpenElement(seq++, "td");

            __b.OpenElement(seq++, "span");
            __b.AddAttribute(seq++, "style", "position:static");

            if (onDetailsClick != null)
            {
                // نمرر الـ Action مباشرة كـ event handler
                __b.AddAttribute(seq++, "onclick", onDetailsClick);
            }

            __b.AddMarkupContent(seq++,
                $"<span class='inline-block w-5 text-center [&>svg]:w-3 [&>svg]:h-3'>" +
                $"{(i == 0 ? Icons.Chain : i == 1 ? Icons.ChinUnLink : Icons.Plus)}</span>");

            __b.AddMarkupContent(seq++,
                $"<span style='margin-top:6px;width:12px;height:12px;background-color:{color};display:inline-block;border-radius:50%;'></span> ");

            __b.AddContent(seq++, status);

            __b.CloseElement(); // span الداخلي
            __b.CloseElement(); // td
        };
    }
}
