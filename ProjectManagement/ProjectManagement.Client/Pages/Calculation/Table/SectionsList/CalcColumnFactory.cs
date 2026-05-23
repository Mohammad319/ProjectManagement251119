using Microsoft.AspNetCore.Components.Rendering;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Constants;

namespace ProjectManagement.Client.Pages.Calculation.Table.SectionsList;

public static class CalcColumnFactory
{
    private readonly record struct CacheKey(decimal Tax, int Digits, string ColumnSignature);

    private static readonly Dictionary<CacheKey, IReadOnlyList<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>>> Cache = [];

    public static IReadOnlyList<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> GetColumns(
        decimal tax,
        int digits,
        IReadOnlyList<NetColumnState>? visibleColumns)
    {
        var signature = BuildSignature(visibleColumns);
        var key = new CacheKey(tax, digits, signature);
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        string round = BuildFormat(digits);

        var allColumns = BuildAllColumns(tax, round);
        List<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> cols = new(visibleColumns?.Count ?? allColumns.Count);

        if (visibleColumns is { Count: > 0 })
        {
            for (int i = 0; i < visibleColumns.Count; i++)
            {
                if (allColumns.TryGetValue(visibleColumns[i].Id, out var column))
                    cols.Add(column);
            }
        }
        else
        {
            var defaultOrder = TemplateDefaults.NetCalc().Select(x => x.Id).ToList();
            for (int i = 0; i < defaultOrder.Count; i++)
            {
                if (allColumns.TryGetValue(defaultOrder[i], out var column))
                    cols.Add(column);
            }

            cols.AddRange(allColumns
                .Where(x => !defaultOrder.Contains(x.Key))
                .OrderBy(x => (int)x.Key)
                .Select(x => x.Value));
        }

        IReadOnlyList<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> result = cols;
        Cache[key] = result;
        return result;
    }

    private static string BuildFormat(int digits) => NumericFormatHelper.BuildOptionalFractionFormat(digits);

    private static string BuildSignature(IReadOnlyList<NetColumnState>? visibleColumns) =>
        visibleColumns is not { Count: > 0 }
            ? string.Empty
            : string.Join(',', visibleColumns.Select(x => $"{(int)x.Id}:{x.Width}"));

    private static Dictionary<NetColumnId, CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> BuildAllColumns(decimal tax, string round)
    {
        Dictionary<NetColumnId, CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> columns =
            new()
            {
            [NetColumnId.Code] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderTextTd(b, t.Metadata.Code),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.Active] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderCheckboxTd(b, t.Active),
                ResRender = (b, r) => TableRenderHelpers.RenderCheckboxTd(b, r.Active)
            },
            [NetColumnId.Account] = new()
            {
                TaskRender = static (b, _) => TableRenderHelpers.EmptyTd(b),
                ResRender = (b, r) => TableRenderHelpers.RenderWithTitle(b, r.AccountCode)
            },
            [NetColumnId.Name] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderWithTitle(b, t.Name),
                ResRender = (b, r) => TableRenderHelpers.RenderWithTitle(b, r.Name)
            },
            [NetColumnId.Status] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderStatusTd(b, t.StatusColor, t.Status),
                ResRender = (b, r) => TableRenderHelpers.RenderStatusTd(
                    b,
                    r.StatusColor ?? string.Empty,
                    r.Status ?? string.Empty,
                    r.HasOfferSelected() ? 0 : r.HasOffer ? 1 : 2,
                    r.Ui.OfferClick)
            },
            [NetColumnId.ResourceTypeSystem] = new()
            {
                TaskRender = static (b, _) => TableRenderHelpers.EmptyTd(b),
                ResRender = (b, r) => TableRenderHelpers.RenderTextTd(b, r.ResType)
            },
            [NetColumnId.ResourceType] = new()
            {
                TaskRender = static (b, _) => TableRenderHelpers.EmptyTd(b),
                ResRender = (b, r) => TableRenderHelpers.RenderTextTd(b, r.ResName)
            },
            [NetColumnId.ResourceSort] = new()
            {
                TaskRender = static (b, _) => TableRenderHelpers.EmptyTd(b),
                ResRender = (b, r) => TableRenderHelpers.RenderTextTd(b, r.Sort)
            },
            [NetColumnId.Quantity] = new()
            {
                TaskRender = (b, t) => RenderTaskQuantityTd(b, round, t),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.Quantity)
            },
            [NetColumnId.Unit] = new()
            {
                TaskRender = RenderTaskUnitTd,
                ResRender = (b, r) => TableRenderHelpers.RenderTextTd(b, r.Unit)
            },
            [NetColumnId.Cost] = new()
            {
                TaskRender = static (b, _) => TableRenderHelpers.EmptyTd(b),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.GetComputedCost())
            },
            [NetColumnId.ChangeFactor1] = new()
            {
                TaskRender = static (b, _) => TableRenderHelpers.EmptyTd(b),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.ChangeFactor1)
            },
            [NetColumnId.ChangeFactor2] = new()
            {
                TaskRender = (b, t) => { if (t.Type is TaskType.CodeName or TaskType.FourBarCode) TableRenderHelpers.EmptyTd(b); else TableRenderHelpers.RenderFormattedTd(b, round, t.Metadata.ChangeFactor2); },
                //ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.ChangeFactor2)
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b),
            },
            [NetColumnId.Cap] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderTextTd(b, t.Cap),
                ResRender = (b, r) =>
                {
                    if (r.ResType == ResourceTypesEnum.Worker || r.ResType == ResourceTypesEnum.MachinesAndEquipments)
                        TableRenderHelpers.RenderTextTd(b, r.DisplayCapWaste);
                    else
                        TableRenderHelpers.EmptyTd(b);
                }
            },
            [NetColumnId.Waste] = new()
            {
                TaskRender = static (b, _) => TableRenderHelpers.EmptyTd(b),
                ResRender = (b, r) =>
                {
                    if (r.ResType == ResourceTypesEnum.Materials)
                        TableRenderHelpers.RenderTextTd(b, r.DisplayCapWaste);
                    else
                        TableRenderHelpers.EmptyTd(b);
                }
            },
            [NetColumnId.BaseCost] = new()
            {
                TaskRender = (b, t) => RenderFourBarPriceTd(b, round, t, t.GetComputedBaseCost()),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.GetComputedBaseCost())
            },
            [NetColumnId.Opportunity] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderTextTd(b, t.Opportunity),
                ResRender = (b, r) => TableRenderHelpers.RenderTextTd(b, r.Opportunity)
            },
            [NetColumnId.NetCostQ] = new()
            {
                TaskRender = (b, t) => RenderFourBarPriceTd(b, round, t, t.GetComputedNetCostQ()),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.GetComputedNetCostQ())
            },
            [NetColumnId.TotalNetCost] = new()
            {
                TaskRender = (b, t) => RenderFourBarPriceTd(b, round, t, t.GetComputedNetCostTotaly()),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.GetComputedNetCostTotaly())
            },
            [NetColumnId.PriceQTax] = new()
            {
                TaskRender = (b, t) => RenderFourBarPriceTd(b, round, t, t.PriceQTax(tax)),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.PriceQ] = new()
            {
                TaskRender = (b, t) => RenderFourBarPriceTd(b, round, t, t.PriceQ),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.PriceProduction] = new()
            {
                TaskRender = (b, t) => RenderFourBarPriceTd(b, round, t, t.PriceProduction),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.PriceTotaly] = new()
            {
                TaskRender = (b, t) => RenderFourBarPriceTd(b, round, t, t.GetComputedApriceTotally()),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.GetComputedApriceTotally())
            },
            [NetColumnId.PriceTotallyTax] = new()
            {
                TaskRender = (b, t) => RenderFourBarPriceTd(b, round, t, t.ApriceTotallyTax(tax)),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.Factor] = new()
            {
                TaskRender = static (b, _) => TableRenderHelpers.EmptyTd(b),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.GetComputedFactor())
            },
            [NetColumnId.MinPrice] = new()
            {
                TaskRender = (b, t) => RenderFourBarPriceTd(b, round, t, t.MinPrice),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.CeilingPrice] = new()
            {
                TaskRender = (b, t) => RenderFourBarPriceTd(b, round, t, t.CeilingPrice),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.PriceSub] = new()
            {
                TaskRender = (b, t) => RenderFourBarPriceTd(b, round, t, t.PriceSub),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.Data.PriceSub)
            },
            [NetColumnId.PriceTotalSub] = new()
            {
                TaskRender = (b, t) => RenderFourBarPriceTd(b, round, t, t.PriceSubTotal),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.PriceSubTotal)
            },
            [NetColumnId.PriceTotalSubTax] = new()
            {
                TaskRender = (b, t) => RenderFourBarPriceTd(b, round, t, t.PriceTotalSubTax(tax)),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.Diff] = new()
            {
                TaskRender = (b, t) => RenderFourBarPriceTd(b, round, t, t.Diff),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.Responsible] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderTextTd(b, t.Responsible),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.Co2] = new()
            {
                TaskRender = (b, t) => RenderFourBarEmptyTd(b, round, t, t.GetComputedCO2PerQuantity()),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.CO2)
            },
            [NetColumnId.TotalCo2] = new()
            {
                TaskRender = (b, t) => RenderFourBarEmptyTd(b, round, t, t.GetComputedTotalCO2()),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.GetComputedTotalCO2())
            },
            [NetColumnId.ActuallyQuantity] = new()
            {
                TaskRender = (b, t) => RenderFourBarEmptyTd(b, round, t, t.Metadata.ActuallyQuantity),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.WorkedQ] = new()
            {
                TaskRender = (b, t) => RenderFourBarEmptyTd(b, round, t, t.Metadata.WorkedQ),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.WorkedQPercent] = new()
            {
                TaskRender = (b, t) => RenderFourBarEmptyTd(b, round, t, t.WorkedQPercent),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.PriceActuallyQuantity] = new()
            {
                TaskRender = (b, t) => RenderFourBarPriceTd(b, round, t, t.PriceActuallyQuantity),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.PriceWorkedQ] = new()
            {
                TaskRender = (b, t) => RenderFourBarPriceTd(b, round, t, t.PriceWorkedQ),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.PriceSubTax] = new()
            {
                TaskRender = (b, t) => RenderFourBarPriceTd(b, round, t, t.PriceSubTax(tax)),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.PriceActuallyQuantityTax] = new()
            {
                TaskRender = (b, t) => RenderFourBarPriceTd(b, round, t, t.PriceActuallyQuantityTax(tax)),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.PriceWorkedQTax] = new()
            {
                TaskRender = (b, t) => RenderFourBarPriceTd(b, round, t, t.PriceWorkedQTax(tax)),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.Note] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderTextTd(b, t.Note),
                ResRender = (b, r) => TableRenderHelpers.RenderTextTd(b, r.Data.Note)
            },
        };

        foreach (var pair in columns)
            pair.Value.Id = pair.Key;

        return columns;
    }

    private static void RenderTaskQuantityTd(RenderTreeBuilder builder, string round, TaskListMVVM task)
    {
        if (TaskTypeRules.DisplaysDashQuantity(task.Type))
        {
            var title = TaskTypeRules.IsThreeBarCode(task.Type)
                ? "\"-\" här räknas mängden som 1."
                : null;
            TableRenderHelpers.RenderTextTd(builder, "-", title);
        }
        else
            TableRenderHelpers.RenderFormattedTd(builder, round, task.Quantity);
    }

    private static void RenderTaskUnitTd(RenderTreeBuilder builder, TaskListMVVM task)
    {
        if (TaskTypeRules.DisplaysDashUnit(task.Type))
            TableRenderHelpers.RenderTextTd(builder, "-");
        else
            TableRenderHelpers.RenderTextTd(builder, TaskConversionUnitDisplayHelper.Resolve(task.Metadata, task.Unit).ConvertedUnit);
    }

    private static void RenderFourBarPriceTd(RenderTreeBuilder builder, string round, TaskListMVVM task, decimal value)
    {
        if (TaskTypeRules.DisplaysDashPrice(task.Type))
            TableRenderHelpers.RenderTextTd(builder, "-");
        else
            TableRenderHelpers.RenderFormattedTd(builder, round, value);
    }

    private static void RenderFourBarPriceTd(RenderTreeBuilder builder, string round, TaskListMVVM task, decimal? value)
    {
        if (TaskTypeRules.DisplaysDashPrice(task.Type))
            TableRenderHelpers.RenderTextTd(builder, "-");
        else
            TableRenderHelpers.RenderFormattedTd(builder, round, value);
    }

    private static void RenderFourBarEmptyTd(RenderTreeBuilder builder, string round, TaskListMVVM task, decimal value)
    {
        if (TaskTypeRules.IsFourBarCode(task.Type))
            TableRenderHelpers.EmptyTd(builder);
        else
            TableRenderHelpers.RenderFormattedTd(builder, round, value);
    }

    private static void RenderFourBarEmptyTd(RenderTreeBuilder builder, string round, TaskListMVVM task, double? value)
    {
        if (TaskTypeRules.IsFourBarCode(task.Type))
            TableRenderHelpers.EmptyTd(builder);
        else
            TableRenderHelpers.RenderFormattedTd(builder, round, value);
    }
}
