namespace ProjectManagement.Client.Shared.MVVM.Calculation
{
    public static class CalculationComputedAccessors
    {
        public static decimal GetComputedNetCostQ(this TaskListMVVM task) => task.Computed.NetCostQ;

        public static decimal GetComputedNetCostTotaly(this TaskListMVVM task) => task.Computed.NetCostTotaly;

        public static decimal GetComputedApriceTotally(this TaskListMVVM task) => task.Computed.ApriceTotally;

        public static double? GetComputedTotalCO2(this TaskListMVVM task) => task.Computed.TotalCO2;

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
            resource.Computed.ApriceTotallyCache = null;
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
                (resource.BaseCost ?? 0m) + (resource.Quantity.HasValue ? resource.Quantity.Value * resource.Cost : 0m);
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
    }
}
