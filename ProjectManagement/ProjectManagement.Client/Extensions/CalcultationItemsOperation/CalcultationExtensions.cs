using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.Base.Calculation;

namespace ProjectManagement.Client.Extensions.CalcultationItemsOperation
{

    public static class CalcultationExtensions
    {
        static void InitializeCalculation(CalculationMVVM calc)
        {
            calc.Factors ??= [];
            calc.QuanityList ??= [];

            foreach (var task in calc.Tasks.Where(x => !x.TaskId.HasValue)) task.CalcVaribles(calc.QuanityList, null);
            foreach (TaskListMVVM task in calc.Tasks.Where(x => x.Data.Type != TaskType.CodeName && x.Resources.Count > 0))
                foreach (var res in task.Resources)
                {
                    res.CalcVaribles(calc?.QuanityList, task?.Data?.Quantity, task?.Data?.Cap);

                    if (res.Active)
                    {
                        Factors? factors = calc?
                            .Factors?
                            .FirstOrDefault(x =>
                                x.SortId == res.ResourceSortId &&
                                x.ResId == res.ResourceTypeId &&
                                x.ResourceType == res.ResType
                            );
                        if (factors != null)
                        {
                            if (res.ResName != factors.ResName || res.Sort != factors.Sort)
                            {
                                factors.ResName = res.ResName;
                                factors.Sort = res.Sort;
                            }
                            factors.AddResValue(task.Data.IsOH, res.NetCostTotaly);
                        }
                        else calc.Factors.Add(Factors.AddNewFactor(task.Data.IsOH, res));
                    }
                }
        }

        public static void CalcEarningsForUnlockedRes(this CalculationMVVM Calculation)
        {
            double e = ((Calculation.ProfitDecision * Calculation.Sum) - Calculation.Factors.Where(x => x.IsLocked).Sum(x => x.Sum * x.Earnings))
                / Calculation.Factors.Where(x => !x.IsLocked).Sum(x => x.Sum);

            foreach (Factors item in Calculation.Factors.Where(x => x.EarningsValue > 0 && !x.IsLocked))
                item.Earnings = e;
            AssignFactorsToResources(Calculation);
        }

        public static void AssignFactorsToResources(this CalculationMVVM? calculation)
        {
            if (calculation is null)
                return;

            var factors = calculation.Factors;
            var tasks = calculation.Tasks;

            // الجزء الأول: تطبيق FactorF على العوامل ذات التكلفة
            if (factors is not null)
            {
                foreach (var factor in factors.Where(x => x.NetCostTotaly > 0))
                    factor.FactorF(factors);
            }

            // إن لم يكن لدينا Tasks أو Factors نخرج
            if (tasks is null || factors is null)
                return;

            // الجزء الثاني: ربط Factors بالـ Resources
            foreach (var resource in tasks
                .Where(t => t?.Data?.Quantity.HasValue == true)
                .SelectMany(t => t!.Resources ?? Enumerable.Empty<ResourceListMVVM>()))
            {
                var factor = factors.FirstOrDefault(x =>
                    x.SortId == resource.ResourceSortId &&
                    x.ResourceType == resource.ResType);

                if (factor is not null)
                {
                    resource.Factor = factor.Factor;
                }
            }
        }

        public static void ExecuteCalculation(this CalculationMVVM Calculation)
        {
            InitializeCalculation(Calculation);
            AssignFactorsToResources(Calculation);
            Calculation.Factors = [.. Calculation.Factors.Where(x => x.NetCostTotaly > 0 || x.NetCostTotalyOH > 0)];
            Calculation.ProfitDecision = Calculation.Factors.Sum(x => x.Sum * x.Earnings) / Calculation.Sum;
        }
    }
}