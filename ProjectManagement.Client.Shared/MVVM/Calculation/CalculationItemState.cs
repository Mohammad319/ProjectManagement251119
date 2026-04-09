namespace ProjectManagement.Client.Shared.MVVM.Calculation
{
    public class CalculationItemUiState
    {
        public bool FilterVisible { get; set; } = true;
        public bool HasUpdated { get; set; }
        public bool IsDragOver { get; set; }
    }

    public sealed class TaskUiState : CalculationItemUiState
    {
        public bool CollSpan { get; set; } = true;
        public int Version { get; set; }
        public Func<Task>? ContextClick { get; set; }
    }

    public sealed class TaskComputedState
    {
        public decimal NetCostQ { get; set; }
        public decimal NetCostTotaly { get; set; }
        public decimal ApriceTotally { get; set; }
        public double? TotalCO2 { get; set; }
        public decimal? BaseCost { get; set; }

        public void Reset()
        {
            NetCostQ = 0;
            NetCostTotaly = 0;
            ApriceTotally = 0;
            TotalCO2 = null;
            BaseCost = null;
        }
    }

    public sealed class ResourceUiState : CalculationItemUiState
    {
        public int Version { get; set; }
        public Func<Task>? ContextClick { get; set; }
        public Func<Task>? OfferClick { get; set; }
    }

    public sealed class ResourceComputedState
    {
        public decimal Factor { get; set; } = 1;
        public decimal? CostCache { get; set; }
        public decimal? BaseCostCache { get; set; }
        public decimal? NetCostQCache { get; set; }
        public decimal? NetCostTotalyCache { get; set; }
        public decimal? ApriceTotallyCache { get; set; }
        public double? TotalCO2Cache { get; set; }

        public void Reset()
        {
            CostCache = null;
            BaseCostCache = null;
            NetCostQCache = null;
            NetCostTotalyCache = null;
            ApriceTotallyCache = null;
            TotalCO2Cache = null;
        }
    }
}
