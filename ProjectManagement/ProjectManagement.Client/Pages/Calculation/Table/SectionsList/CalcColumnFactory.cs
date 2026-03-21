using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.MVVM.Calculation;
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
            cols.AddRange(allColumns.OrderBy(x => (int)x.Key).Select(x => x.Value));
        }

        IReadOnlyList<CalcColmunDefinition<TaskListMVVM, ResourceListMVVM>> result = cols;
        Cache[key] = result;
        return result;
    }

    private static string BuildFormat(int digits) => "0." + new string('#', digits);

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
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.Quantity),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.Quantity)
            },
            [NetColumnId.Unit] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderTextTd(b, t.Unit),
                ResRender = (b, r) => TableRenderHelpers.RenderTextTd(b, r.Unit)
            },
            [NetColumnId.Cost] = new()
            {
                TaskRender = static (b, _) => TableRenderHelpers.EmptyTd(b),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.Cost)
            },
            [NetColumnId.ChangeFactor1] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.Metadata.ChangeFactor1),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.ChangeFactor1)
            },
            [NetColumnId.ChangeFactor2] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.Metadata.ChangeFactor2),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.ChangeFactor2)
            },
            [NetColumnId.Waste] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderTextTd(b, t.Cap),
                ResRender = (b, r) =>
                {
                    if (r.ResType == ResourceTypesEnum.Worker || r.ResType == ResourceTypesEnum.MachinesAndEquipments)
                        TableRenderHelpers.RenderTextTd(b, r.CapWaste);
                    else
                        TableRenderHelpers.EmptyTd(b);
                }
            },
            [NetColumnId.Cap] = new()
            {
                TaskRender = static (b, _) => TableRenderHelpers.EmptyTd(b),
                ResRender = (b, r) =>
                {
                    if (r.ResType == ResourceTypesEnum.Materials)
                        TableRenderHelpers.RenderTextTd(b, r.CapWaste);
                    else
                        TableRenderHelpers.EmptyTd(b);
                }
            },
            [NetColumnId.BaseCost] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.GetComputedBaseCost()),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.BaseCost)
            },
            [NetColumnId.Opportunity] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderTextTd(b, t.Opportunity),
                ResRender = (b, r) => TableRenderHelpers.RenderTextTd(b, r.Opportunity)
            },
            [NetColumnId.NetCostQ] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.GetComputedNetCostQ()),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.GetComputedNetCostQ())
            },
            [NetColumnId.TotalNetCost] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.GetComputedNetCostTotaly()),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.GetComputedNetCostTotaly())
            },
            [NetColumnId.PriceQTax] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.PriceQTax(tax)),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.PriceQ] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.PriceQ),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.PriceTotaly] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.GetComputedApriceTotally()),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.GetComputedApriceTotally())
            },
            [NetColumnId.PriceTotallyTax] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.ApriceTotallyTax(tax)),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.Factor] = new()
            {
                TaskRender = static (b, _) => TableRenderHelpers.EmptyTd(b),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.GetComputedFactor())
            },
            [NetColumnId.MinPrice] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.MinPrice),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.CeilingPrice] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.CeilingPrice),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.PriceSub] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.PriceSub),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.PriceSub)
            },
            [NetColumnId.PriceTotalSub] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.PriceSubTotal),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.PriceSubTotal)
            },
            [NetColumnId.Diff] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.Diff),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.Responsible] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderTextTd(b, t.Responsible),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.Co2] = new()
            {
                TaskRender = static (b, _) => TableRenderHelpers.EmptyTd(b),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.CO2)
            },
            [NetColumnId.TotalCo2] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.GetComputedTotalCO2()),
                ResRender = (b, r) => TableRenderHelpers.RenderFormattedTd(b, round, r.GetComputedTotalCO2())
            },
            [NetColumnId.ActuallyQuantity] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.Metadata.ActuallyQuantity),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.WorkedQ] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.Metadata.WorkedQ),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.WorkedQPercent] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.WorkedQPercent),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.PriceActuallyQuantity] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.PriceActuallyQuantity),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.PriceWorkedQ] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.PriceWorkedQ),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.PriceSubTax] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.PriceSubTax(tax)),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.PriceActuallyQuantityTax] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.PriceActuallyQuantityTax(tax)),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.PriceWorkedQTax] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderFormattedTd(b, round, t.PriceWorkedQTax(tax)),
                ResRender = static (b, _) => TableRenderHelpers.EmptyTd(b)
            },
            [NetColumnId.Note] = new()
            {
                TaskRender = (b, t) => TableRenderHelpers.RenderTextTd(b, t.Note),
                ResRender = (b, r) => TableRenderHelpers.RenderTextTd(b, r.Note)
            },
        };

        foreach (var pair in columns)
            pair.Value.Id = pair.Key;

        return columns;
    }
}
