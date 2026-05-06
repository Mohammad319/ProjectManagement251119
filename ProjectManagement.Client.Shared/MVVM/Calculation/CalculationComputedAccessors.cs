namespace ProjectManagement.Client.Shared.MVVM.Calculation
{
    public static class CalculationComputedAccessors
    {
        public static decimal GetComputedNetCostQ(this TaskListMVVM task) => task.Computed.NetCostQ;

        public static decimal GetComputedNetCostTotaly(this TaskListMVVM task) => task.Computed.NetCostTotaly;

        public static decimal GetComputedApriceTotally(this TaskListMVVM task) => task.Computed.ApriceTotally;

        public static double? GetComputedTotalCO2(this TaskListMVVM task) => task.Computed.TotalCO2;

        public static double? GetComputedCO2PerQuantity(this TaskListMVVM task)
        {
            var quantity = task.Quantity;
            var totalCO2 = task.GetComputedTotalCO2();

            return totalCO2.HasValue && quantity.HasValue && quantity.Value > 0m
                ? totalCO2.Value / (double)quantity.Value
                : null;
        }

        public static double? GetComputedCO2PerBaseQuantity(this TaskListMVVM task)
        {
            var baseQuantity = task.Metadata?.BaseQuantity;
            var totalCO2 = task.GetComputedTotalCO2();

            return totalCO2.HasValue && baseQuantity.HasValue && baseQuantity.Value > 0m
                ? totalCO2.Value / (double)baseQuantity.Value
                : null;
        }

        public static decimal? GetComputedBaseCost(this TaskListMVVM task) => task.Computed.BaseCost;

        public static void SetComputedAggregates(
            this TaskListMVVM task,
            decimal netCostQ,
            decimal netCostTotaly,
            decimal apriceTotally,
            double? totalCO2,
            decimal? baseCost)
        {
            task.Computed.NetCostQ = netCostQ;
            task.Computed.NetCostTotaly = netCostTotaly;
            task.Computed.ApriceTotally = apriceTotally;
            task.Computed.TotalCO2 = totalCO2;
            task.Computed.BaseCost = baseCost;
        }

        public static decimal GetComputedFactor(this ResourceListMVVM resource) => resource.Computed.Factor;

        public static void SetComputedFactor(this ResourceListMVVM resource, decimal value)
        {
            if (resource.Computed.Factor == value)
                return;

            resource.Computed.Factor = value;
            resource.Computed.CostCache = null;
            resource.Computed.BaseCostCache = null;
            resource.Computed.ApriceTotallyCache = null;
            resource.Computed.NetCostQCache = null;
            resource.Computed.NetCostTotalyCache = null;
        }

        public static decimal GetComputedCost(this ResourceListMVVM resource)
        {
            return resource.Computed.CostCache ??= ComputeEffectiveCost(resource);
        }

        public static decimal? GetComputedBaseCost(this ResourceListMVVM resource)
        {
            return resource.Computed.BaseCostCache ??= ComputeEffectiveBaseCost(resource);
        }

        public static decimal GetComputedNetCostQ(this ResourceListMVVM resource)
        {
            return resource.Computed.NetCostQCache ??=
                resource.Quantity.HasValue && resource.Quantity.Value > 0
                    ? resource.GetComputedNetCostTotaly() / resource.Quantity.Value
                    : 0;
        }

        public static decimal GetComputedNetCostTotaly(this ResourceListMVVM resource)
        {
            return resource.Computed.NetCostTotalyCache ??=
                ComputeVariableCostTotal(resource) + (resource.GetComputedBaseCost() ?? 0m);
        }

        public static decimal GetComputedApriceTotally(this ResourceListMVVM resource)
        {
            return resource.Computed.ApriceTotallyCache ??=
                resource.GetComputedFactor() * resource.GetComputedNetCostTotaly();
        }

        public static double? GetComputedTotalCO2(this ResourceListMVVM resource)
        {
            return resource.Computed.TotalCO2Cache ??=
                (resource.CO2.HasValue && resource.Quantity.HasValue)
                    ? (double)resource.Quantity.Value * resource.CO2.Value
                    : null;
        }

        private static decimal ComputeEffectiveCost(ResourceListMVVM resource)
        {
            var quantity = resource.Quantity ?? 0m;
            if (quantity <= 0m)
                return resource.Cost;

            return ComputeVariableCostTotal(resource) / quantity;
        }

        private static decimal? ComputeEffectiveBaseCost(ResourceListMVVM resource)
        {
            var addOns = resource.Data.AddOns;
            var hasAddOns = addOns is { Count: > 0 };

            if (!resource.BaseCost.HasValue && !hasAddOns)
                return null;

            decimal totalBaseCost = resource.BaseCost ?? 0m;

            if (hasAddOns)
            {
                for (int i = 0; i < addOns!.Count; i++)
                    totalBaseCost += addOns[i].BaseCost;
            }

            return totalBaseCost;
        }

        private static decimal ComputeVariableCostTotal(ResourceListMVVM resource)
        {
            decimal total = resource.Quantity.HasValue
                ? resource.Quantity.Value * resource.Cost
                : 0m;

            var addOns = resource.Data.AddOns;
            if (addOns is null || addOns.Count == 0)
                return total;

            for (int i = 0; i < addOns.Count; i++)
                total += addOns[i].Quantity(resource.Quantity.HasValue? resource.Quantity.Value: 0m) * addOns[i].Cost;

            return total;
        }
    }
}
